using GymManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly GymDbContext _context;

        public HomeController(GymDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Members expiring in next 7 days
            var today = DateOnly.FromDateTime(DateTime.Today);
            var next7Days = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

            ViewBag.ExpiringMembers = await _context.Payments
                .Include(p => p.Member)
                .Include(p => p.Plan)
                .Where(p => p.ValidUntil >= today && p.ValidUntil <= next7Days)
                .OrderBy(p => p.ValidUntil)
                .ToListAsync();
            // If not logged in — show landing page
            if (!User.Identity!.IsAuthenticated)
                return View("Landing");

            // If Admin — show dashboard with real stats
            if (User.IsInRole("Admin"))
            {
                ViewBag.TotalMembers = await _context.Members.CountAsync();
                ViewBag.ActiveMembers = await _context.Members
                    .CountAsync(m => m.IsActive == true);
                ViewBag.TotalTrainers = await _context.Trainers.CountAsync();
                ViewBag.ActiveTrainers = await _context.Trainers
                    .CountAsync(t => t.IsActive == true);
                ViewBag.TotalPlans = await _context.MembershipPlans.CountAsync();
                ViewBag.TotalPayments = await _context.Payments.CountAsync();
                ViewBag.MonthlyRevenue = await _context.Payments
                    .Where(p => p.PaymentDate.Month == DateTime.Now.Month
                             && p.PaymentDate.Year == DateTime.Now.Year)
                    .SumAsync(p => p.AmountPaid);
                ViewBag.TotalRevenue = await _context.Payments
                    .SumAsync(p => p.AmountPaid);
                ViewBag.TodayAttendance = await _context.Attendances
                    .CountAsync(a => a.CheckInTime.Date == DateTime.Today);
                ViewBag.StillInside = await _context.Attendances
                    .CountAsync(a => a.CheckOutTime == null
                               && a.CheckInTime.Date == DateTime.Today);

                // Recent 5 members
                ViewBag.RecentMembers = await _context.Members
                    .Include(m => m.Plan)
                    .OrderByDescending(m => m.JoinDate)
                    .Take(5)
                    .ToListAsync();

                // Recent 5 payments
                ViewBag.RecentPayments = await _context.Payments
                    .Include(p => p.Member)
                    .Include(p => p.Plan)
                    .OrderByDescending(p => p.PaymentDate)
                    .Take(5)
                    .ToListAsync();

                return View("Dashboard");
            }

            // If Member — go to profile
            return RedirectToAction("MyProfile", "Account");
        }
    }
}