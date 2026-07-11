using System.Net;
using System.Net.Mail;

namespace GymManagementSystem.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var settings = _config.GetSection("EmailSettings");
            var senderEmail = settings["SenderEmail"];
            var senderName = settings["SenderName"];
            var smtpServer = settings["SmtpServer"];
            var smtpPort = int.Parse(settings["SmtpPort"]!);
            var appPassword = settings["AppPassword"];

            var client = new SmtpClient(smtpServer, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(senderEmail, appPassword)
            };

            var message = new MailMessage
            {
                From = new MailAddress(senderEmail!, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);
            await client.SendMailAsync(message);
        }

        // Send Password Reset Email
        public async Task SendPasswordResetEmailAsync(
            string toEmail, string userName, string resetLink)
        {
            var subject = "FitZone — Reset Your Password";
            var body = $@"
<!DOCTYPE html>
<html>
<body style='font-family:Inter,sans-serif;background:#f4f6f9;
             padding:40px 20px;'>
    <div style='max-width:500px;margin:0 auto;background:white;
                border-radius:16px;overflow:hidden;
                box-shadow:0 4px 20px rgba(0,0,0,0.08);'>

        <!-- Header -->
        <div style='background:linear-gradient(135deg,#1a1a2e,#16213e);
                    padding:30px;text-align:center;'>
            <div style='width:50px;height:50px;border-radius:12px;
                        background:linear-gradient(135deg,#ffc107,#ff8c00);
                        display:inline-flex;align-items:center;
                        justify-content:center;margin-bottom:12px;'>
                ⚡
            </div>
            <h1 style='color:white;margin:0;font-size:1.5rem;'>FitZone</h1>
            <p style='color:rgba(255,255,255,0.6);margin:4px 0 0;
                      font-size:0.85rem;'>Gym Management System</p>
        </div>

        <!-- Body -->
        <div style='padding:32px;'>
            <h2 style='color:#1a1a2e;margin-top:0;'>
                Reset Your Password
            </h2>
            <p style='color:#6c757d;line-height:1.6;'>
                Hello <strong>{userName}</strong>,<br/><br/>
                We received a request to reset your FitZone password.
                Click the button below to set a new password.
            </p>

            <div style='text-align:center;margin:30px 0;'>
                <a href='{resetLink}'
                   style='background:linear-gradient(135deg,#ffc107,#ff8c00);
                          color:#1a1a2e;font-weight:700;padding:14px 32px;
                          border-radius:12px;text-decoration:none;
                          display:inline-block;font-size:0.95rem;'>
                    🔐 Reset Password
                </a>
            </div>

            <div style='background:#fff8e1;border:1px solid #ffc107;
                        border-radius:10px;padding:14px;margin-top:20px;'>
                <p style='margin:0;color:#856404;font-size:0.85rem;'>
                    ⚠️ This link expires in <strong>1 hour</strong>.
                    If you didn't request this, ignore this email.
                </p>
            </div>
        </div>

        <!-- Footer -->
        <div style='background:#f8f9fa;padding:20px;text-align:center;
                    border-top:1px solid #f0f0f0;'>
            <p style='color:#adb5bd;font-size:0.8rem;margin:0;'>
                © 2025 FitZone Gym Management System
            </p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}