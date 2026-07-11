using GymManagementSystem.Data;
using GymManagementSystem.Models;
using GymManagementSystem.Services;
using GymManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly GymDbContext _context;
        private readonly EmailService _emailService;
        private readonly IConfiguration _config;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            GymDbContext context,
            EmailService emailService,
            IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailService = emailService;
            _config = config;
        }

        // ─── LOGIN ───────────────────────────────────────────
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity!.IsAuthenticated)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                var roles = await _userManager.GetRolesAsync(user!);

                if (roles.Contains("Admin"))
                    return RedirectToAction("Index", "Home");
                else
                    return RedirectToAction("MyProfile", "Account");
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        // ─── REGISTER ────────────────────────────────────────
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity!.IsAuthenticated)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Role = "Member",
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Member");
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("MyProfile", "Account");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // ─── MY PROFILE ───────────────────────────────────────
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            // Get all available plans
            ViewBag.Plans = await _context.MembershipPlans.ToListAsync();

            // Find member record — try email first, then name
            var memberRecord = await _context.Members
                .FirstOrDefaultAsync(m => m.Email == user.Email);

            if (memberRecord == null && !string.IsNullOrEmpty(user.FullName))
            {
                memberRecord = await _context.Members
                    .FirstOrDefaultAsync(m => m.FullName.ToLower()
                        == user.FullName.ToLower());
            }

            if (memberRecord != null)
            {
                ViewBag.MemberDbId = memberRecord.MemberId;

                // Get latest payment for this member
                var latestPayment = await _context.Payments
                    .Include(p => p.Plan)
                    .Where(p => p.MemberId == memberRecord.MemberId)
                    .OrderByDescending(p => p.PaymentDate)
                    .FirstOrDefaultAsync();

                ViewBag.CurrentPayment = latestPayment;
            }
            else
            {
                ViewBag.MemberDbId = null;
                ViewBag.CurrentPayment = null;
            }

            return View(user);
        }

        // ─── LOGOUT ──────────────────────────────────────────
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return RedirectToAction("LogoutAnimation", "Account");
        }

        // ─── LOGOUT ANIMATION ─────────────────────────────────
        [AllowAnonymous]
        public IActionResult LogoutAnimation()
        {
            return View();
        }

        // ─── ACCESS DENIED ────────────────────────────────────
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ─── FORGOT PASSWORD ──────────────────────────────────
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Please enter your email.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                TempData["ForgotSuccess"] = true;
                return View();
            }

            // Generate reset token
            var token = await _userManager
                .GeneratePasswordResetTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);

            // Build reset URL using fixed base URL from appsettings
            var baseUrl = _config["EmailSettings:BaseUrl"]
                ?? $"{Request.Scheme}://{Request.Host}";

            var resetLink = $"{baseUrl}/Account/ResetPassword" +
                $"?token={encodedToken}" +
                $"&email={Uri.EscapeDataString(email)}";

            // Send email
            try
            {
                await _emailService.SendPasswordResetEmailAsync(
                    email, user.FullName, resetLink);
                TempData["ForgotSuccess"] = true;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("",
                    $"Email error: {ex.Message}");
            }

            return View();
        }

        // ─── RESET PASSWORD ───────────────────────────────────
        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            ViewBag.Token = token;
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(
            string token, string email,
            string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                ViewBag.Token = token;
                ViewBag.Email = email;
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return RedirectToAction("Login");

            var decodedToken = Uri.UnescapeDataString(token);
            var result = await _userManager.ResetPasswordAsync(
                user, decodedToken, newPassword);

            if (result.Succeeded)
            {
                TempData["ResetSuccess"] = true;
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            ViewBag.Token = token;
            ViewBag.Email = email;
            return View();
        }
    }
}