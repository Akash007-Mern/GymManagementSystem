using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace GymManagementSystem.Controllers
{
    [Authorize]
    public class ESewaController : Controller
    {
        private readonly GymDbContext _context;

        private const string MerchantCode = "EPAYTEST";
        private const string SecretKey = "8gBm/:&EnhH.1[L";
        private const string ESewaUrl =
            "https://rc-epay.esewa.com.np/api/epay/main/v2/form";

        public ESewaController(GymDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Pay(int memberId, int planId)
        {
            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.MemberId == memberId);

            var plan = await _context.MembershipPlans
                .FindAsync(planId);

            if (member == null || plan == null)
                return NotFound();

            // Transaction UUID
            var transactionUuid = $"FZ-{memberId}-{planId}-{DateTime.Now.Ticks}";

            // eSewa needs amount as integer string if no decimals
            // e.g. "1500" not "1500.00"
            var amount = plan.Price % 1 == 0
                ? ((int)plan.Price).ToString()
                : plan.Price.ToString("0.00",
                    System.Globalization.CultureInfo.InvariantCulture);

            // CRITICAL: message must match signed_field_names order EXACTLY
            // signed_field_names = "total_amount,transaction_uuid,product_code"
            var message = $"{amount},{transactionUuid},{MerchantCode}";
            var signature = GenerateHmacSignature(message);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            ViewBag.Member = member;
            ViewBag.Plan = plan;
            ViewBag.Amount = amount;
            ViewBag.TransactionUuid = transactionUuid;
            ViewBag.Signature = signature;
            ViewBag.SuccessUrl = $"{baseUrl}/ESewa/Success";
            ViewBag.FailureUrl = $"{baseUrl}/ESewa/Failure";
            ViewBag.MerchantCode = MerchantCode;
            ViewBag.ESewaUrl = ESewaUrl;

            // Debug info
            ViewBag.DebugMessage = message;

            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> Success(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                TempData["Error"] = "Payment verification failed.";
                return RedirectToAction("MyProfile", "Account");
            }

            try
            {
                var decodedBytes = Convert.FromBase64String(data);
                var decodedJson = Encoding.UTF8.GetString(decodedBytes);

                var response = System.Text.Json.JsonSerializer
                    .Deserialize<Dictionary<string, string>>(decodedJson);

                if (response == null)
                {
                    TempData["Error"] = "Invalid payment response.";
                    return RedirectToAction("MyProfile", "Account");
                }

                var status = response.GetValueOrDefault("status", "");
                var transactionCode = response.GetValueOrDefault(
                    "transaction_code", "");
                var transactionUuid = response.GetValueOrDefault(
                    "transaction_uuid", "");
                var totalAmount = response.GetValueOrDefault(
                    "total_amount", "0");

                if (status != "COMPLETE")
                {
                    TempData["Error"] = $"Payment failed. Status: {status}";
                    return RedirectToAction("MyProfile", "Account");
                }

                // Parse memberId and planId from UUID
                // Format: FZ-{memberId}-{planId}-{ticks}
                var parts = transactionUuid.Split('-');
                if (parts.Length >= 3 &&
                    int.TryParse(parts[1], out int memberId) &&
                    int.TryParse(parts[2], out int planId))
                {
                    var plan = await _context.MembershipPlans.FindAsync(planId);

                    var cleanAmount = totalAmount.Replace(",", "");
                    decimal.TryParse(cleanAmount,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out decimal parsedAmount);

                    var payment = new Payment
                    {
                        MemberId = memberId,
                        PlanId = planId,
                        AmountPaid = parsedAmount,
                        PaymentDate = DateOnly.FromDateTime(DateTime.Today),
                        ValidUntil = DateOnly.FromDateTime(
                            DateTime.Today.AddDays(plan?.DurationDays ?? 30)),
                        Notes = $"eSewa Payment — Code: {transactionCode}"
                    };

                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();

                    TempData["Success"] =
                        $"✅ Payment successful! Transaction: {transactionCode}";
                }

                return RedirectToAction("MyProfile", "Account");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("MyProfile", "Account");
            }
        }

        [AllowAnonymous]
        public IActionResult Failure()
        {
            TempData["Error"] = "❌ Payment was cancelled or failed.";
            return RedirectToAction("MyProfile", "Account");
        }

        private string GenerateHmacSignature(string message)
        {
            var keyBytes = Encoding.UTF8.GetBytes(SecretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}