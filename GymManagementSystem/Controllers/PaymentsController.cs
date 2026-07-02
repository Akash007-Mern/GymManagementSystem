using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PaymentsController : Controller
    {
        private readonly GymDbContext _context;

        public PaymentsController(GymDbContext context)
        {
            _context = context;
        }

        // READ - List all payments
        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payments
                .Include(p => p.Member)
                .Include(p => p.Plan)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
            return View(payments);
        }

        // READ - Payment details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Member)
                .Include(p => p.Plan)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null) return NotFound();
            return View(payment);
        }

        // CREATE - Show form
        public IActionResult Create()
        {
            ViewData["MemberId"] = new SelectList(
                _context.Members, "MemberId", "FullName");
            ViewData["PlanId"] = new SelectList(
                _context.MembershipPlans, "PlanId", "PlanName");
            return View();
        }

        // CREATE - Save payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Payment payment)
        {
            if (ModelState.IsValid)
            {
                // Auto calculate ValidUntil based on plan duration
                var plan = await _context.MembershipPlans
                    .FindAsync(payment.PlanId);
                if (plan != null)
                {
                    payment.ValidUntil = payment.PaymentDate
                        .AddDays(plan.DurationDays);
                }

                _context.Add(payment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["MemberId"] = new SelectList(
                _context.Members, "MemberId", "FullName", payment.MemberId);
            ViewData["PlanId"] = new SelectList(
                _context.MembershipPlans, "PlanId", "PlanName", payment.PlanId);
            return View(payment);
        }

        // UPDATE - Show edit form
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null) return NotFound();

            ViewData["MemberId"] = new SelectList(
                _context.Members, "MemberId", "FullName", payment.MemberId);
            ViewData["PlanId"] = new SelectList(
                _context.MembershipPlans, "PlanId", "PlanName", payment.PlanId);
            return View(payment);
        }

        // UPDATE - Save changes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Payment payment)
        {
            if (id != payment.PaymentId) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(payment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["MemberId"] = new SelectList(
                _context.Members, "MemberId", "FullName", payment.MemberId);
            ViewData["PlanId"] = new SelectList(
                _context.MembershipPlans, "PlanId", "PlanName", payment.PlanId);
            return View(payment);
        }

        // DELETE - Show confirmation
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Member)
                .Include(p => p.Plan)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null) return NotFound();
            return View(payment);
        }

        // DELETE - Confirm
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null) _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}