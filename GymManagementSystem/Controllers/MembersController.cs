using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace GymManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MembersController : Controller
    {
        // _context is our connection to the database
        private readonly GymDbContext _context;

        public MembersController(GymDbContext context)
        {
            _context = context;
        }

        // READ - Show list of all members
        public async Task<IActionResult> Index()
        {
            var members = await _context.Members
                .Include(m => m.Plan)
                .Include(m => m.Trainer)
                .ToListAsync();
            return View(members);
        }                              // ← END OF INDEX

        // ← EMPTY LINE
        // ← EMPTY LINE

        // EXPORT - Download Members as PDF
        public async Task<IActionResult> ExportPdf()
        {
            var members = await _context.Members
                .Include(m => m.Plan)
                .Include(m => m.Trainer)
                .ToListAsync();

            using var ms = new MemoryStream();
            var document = new Document(PageSize.A4.Rotate(), 20, 20, 20, 20);
            var writer = PdfWriter.GetInstance(document, ms);

            document.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18,
                new BaseColor(26, 26, 46));
            var subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 10,
                new BaseColor(108, 117, 125));
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9,
                BaseColor.WHITE);
            var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 9,
                new BaseColor(51, 51, 51));
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9,
                new BaseColor(51, 51, 51));

            var title = new Paragraph("FitZone Gym Management System\n", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            document.Add(title);

            var subtitle = new Paragraph(
                $"Members Report — Generated on {DateTime.Now:dd MMM yyyy, hh:mm tt}\n\n",
                subtitleFont);
            subtitle.Alignment = Element.ALIGN_CENTER;
            document.Add(subtitle);

            // Summary box
            var summaryTable = new PdfPTable(4);
            summaryTable.WidthPercentage = 100;
            summaryTable.SpacingAfter = 15;

            var summaryItems = new[]
            {
        ("Total Members", members.Count.ToString()),
        ("Active Members", members.Count(m => m.IsActive == true).ToString()),
        ("Inactive Members", members.Count(m => m.IsActive == false).ToString()),
        ("Report Date", DateTime.Now.ToString("dd MMM yyyy"))
    };

            foreach (var (label, value) in summaryItems)
            {
                var cell = new PdfPCell();
                cell.BackgroundColor = new BaseColor(245, 247, 250);
                cell.Border = Rectangle.BOX;
                cell.BorderColor = new BaseColor(220, 220, 220);
                cell.Padding = 10;
                cell.AddElement(new Paragraph(label + "\n",
                    FontFactory.GetFont(FontFactory.HELVETICA, 8,
                        new BaseColor(108, 117, 125))));
                cell.AddElement(new Paragraph(value,
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14,
                        new BaseColor(26, 26, 46))));
                summaryTable.AddCell(cell);
            }
            document.Add(summaryTable);

            // Members table
            var table = new PdfPTable(8);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 3f, 2f, 1.5f, 2f, 2f, 2f, 1.5f, 1.5f });

            var headers = new[]
            {
        "Full Name", "Phone", "Gender",
        "Join Date", "Plan", "Trainer", "Status", "Email"
    };

            foreach (var h in headers)
            {
                var cell = new PdfPCell(new Phrase(h, headerFont));
                cell.BackgroundColor = new BaseColor(26, 26, 46);
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                cell.Padding = 8;
                cell.Border = Rectangle.NO_BORDER;
                table.AddCell(cell);
            }

            bool alternate = false;
            foreach (var m in members)
            {
                var bgColor = alternate
                    ? new BaseColor(248, 249, 250)
                    : BaseColor.WHITE;

                var rowData = new[]
                {
            m.FullName ?? "—",
            m.Phone ?? "—",
            m.Gender ?? "—",
            m.JoinDate.ToString("dd MMM yyyy"),
            m.Plan?.PlanName ?? "No Plan",
            m.Trainer?.FullName ?? "—",
            m.IsActive == true ? "Active" : "Inactive",
            m.Email ?? "—"
        };

                foreach (var data in rowData)
                {
                    var isStatus = data == "Active" || data == "Inactive";
                    var font = isStatus ? boldFont : cellFont;
                    var cell = new PdfPCell(new Phrase(data, font));
                    cell.BackgroundColor = bgColor;

                    if (isStatus)
                    {
                        cell.BackgroundColor = data == "Active"
                            ? new BaseColor(212, 237, 218)
                            : new BaseColor(248, 215, 218);
                    }

                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.Padding = 7;
                    cell.Border = Rectangle.BOTTOM_BORDER;
                    cell.BorderColor = new BaseColor(230, 230, 230);
                    table.AddCell(cell);
                }
                alternate = !alternate;
            }

            document.Add(table);

            var footer = new Paragraph(
                $"\nTotal Records: {members.Count} members exported.",
                FontFactory.GetFont(FontFactory.HELVETICA, 8,
                    new BaseColor(108, 117, 125)));
            footer.Alignment = Element.ALIGN_RIGHT;
            document.Add(footer);

            document.Close();

            return File(ms.ToArray(), "application/pdf",
                $"FitZone_Members_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // READ - Show details of one member
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var member = await _context.Members
                .Include(m => m.Plan)
                .Include(m => m.Trainer)
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member == null) return NotFound();
            return View(member);
        }

        // CREATE - Show the Add form
        public IActionResult Create()
        {
            // Send Plans and Trainers list to the form dropdowns
            ViewData["PlanId"] = new SelectList(_context.MembershipPlans, "PlanId", "PlanName");
            ViewData["TrainerId"] = new SelectList(_context.Trainers, "TrainerId", "FullName");
            return View();
        }

        // CREATE - Save new member to database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Member member)
        {
            if (ModelState.IsValid)
            {
                _context.Add(member);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PlanId"] = new SelectList(_context.MembershipPlans, "PlanId", "PlanName", member.PlanId);
            ViewData["TrainerId"] = new SelectList(_context.Trainers, "TrainerId", "FullName", member.TrainerId);
            return View(member);
        }

        // UPDATE - Show the Edit form
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var member = await _context.Members.FindAsync(id);
            if (member == null) return NotFound();

            ViewData["PlanId"] = new SelectList(_context.MembershipPlans, "PlanId", "PlanName", member.PlanId);
            ViewData["TrainerId"] = new SelectList(_context.Trainers, "TrainerId", "FullName", member.TrainerId);
            return View(member);
        }

        // UPDATE - Save edited member to database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Member member)
        {
            if (id != member.MemberId) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(member);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PlanId"] = new SelectList(_context.MembershipPlans, "PlanId", "PlanName", member.PlanId);
            ViewData["TrainerId"] = new SelectList(_context.Trainers, "TrainerId", "FullName", member.TrainerId);
            return View(member);
        }

        // DELETE - Show delete confirmation
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var member = await _context.Members
                .Include(m => m.Plan)
                .Include(m => m.Trainer)
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member == null) return NotFound();
            return View(member);
        }

        // DELETE - Actually delete from database
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var member = await _context.Members.FindAsync(id);
            if (member != null) _context.Members.Remove(member);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}