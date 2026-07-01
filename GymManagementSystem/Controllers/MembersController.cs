using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

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
            // Get all members, including their Plan and Trainer info
            var members = await _context.Members
                .Include(m => m.Plan)
                .Include(m => m.Trainer)
                .ToListAsync();
            return View(members);
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