using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AttendanceController : Controller
    {
        private readonly GymDbContext _context;

        public AttendanceController(GymDbContext context)
        {
            _context = context;
        }

        // READ - List all attendance
        public async Task<IActionResult> Index()
        {
            var attendance = await _context.Attendances
                .Include(a => a.Member)
                .OrderByDescending(a => a.CheckInTime)
                .ToListAsync();
            return View(attendance);
        }

        // CREATE - Show check-in form
        public IActionResult Create()
        {
            ViewData["MemberId"] = new SelectList(
                _context.Members.Where(m => m.IsActive == true),
                "MemberId", "FullName");
            return View();
        }

        // CREATE - Save check-in
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Attendance attendance)
        {
            if (ModelState.IsValid)
            {
                // Set check-in time to now if not provided
                if (attendance.CheckInTime == default)
                    attendance.CheckInTime = DateTime.Now;

                _context.Add(attendance);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["MemberId"] = new SelectList(
                _context.Members.Where(m => m.IsActive == true),
                "MemberId", "FullName", attendance.MemberId);
            return View(attendance);
        }

        // CHECKOUT - Mark checkout time
        [HttpPost]
        public async Task<IActionResult> CheckOut(int id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance != null)
            {
                attendance.CheckOutTime = DateTime.Now;
                _context.Update(attendance);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var attendance = await _context.Attendances
                .Include(a => a.Member)
                .FirstOrDefaultAsync(a => a.AttendanceId == id);

            if (attendance == null) return NotFound();
            return View(attendance);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance != null) _context.Attendances.Remove(attendance);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}