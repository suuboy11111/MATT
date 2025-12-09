using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Configuration;

namespace MaiAmTinhThuong.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // Kiểm tra xem có dùng SendGrid không
                var sendGridApiKey = _configuration["Email:SendGridApiKey"];
                
                if (!string.IsNullOrEmpty(sendGridApiKey))
                {
                    // Dùng SendGrid
                    return await SendEmailViaSendGridAsync(toEmail, subject, body, sendGridApiKey);
                }
                
                // Fallback: Thử dùng SMTP (nếu có cấu hình)
                var smtpUser = _configuration["Email:SmtpUser"];
                var smtpPassword = _configuration["Email:SmtpPassword"];
                
                if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogWarning("SendGrid API key not found, falling back to SMTP (may not work on Railway)");
                    return await SendEmailViaSmtpAsync(toEmail, subject, body);
                }

                _logger.LogWarning("No email configuration found. Email sending disabled.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail}: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SendEmailViaSendGridAsync(string toEmail, string subject, string body, string apiKey)
        {
            try
            {
                var fromEmail = _configuration["Email:FromEmail"] ?? _configuration["Email:SmtpUser"] ?? "noreply@maiamtinhthuong.vn";
                var fromName = _configuration["Email:FromName"] ?? "Mái Ấm Tình Thương";

                var client = new SendGridClient(apiKey);
                var msg = new SendGridMessage()
                {
                    From = new EmailAddress(fromEmail, fromName),
                    Subject = subject,
                    HtmlContent = body
                };
                msg.AddTo(new EmailAddress(toEmail));

                _logger.LogInformation($"Sending email via SendGrid to {toEmail}...");
                var response = await client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Email sent successfully to {toEmail} via SendGrid");
                    return true;
                }
                else
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError($"SendGrid API error: Status {response.StatusCode}, Body: {responseBody}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email via SendGrid: {ex.Message}");
                return false;
            }
        }

        private Task<bool> SendEmailViaSmtpAsync(string toEmail, string subject, string body)
        {
            // Giữ lại code SMTP cũ để fallback (nhưng có thể không hoạt động trên Railway)
            _logger.LogWarning("SMTP method is deprecated. Please use SendGrid instead.");
            return Task.FromResult(false);
        }

        public async Task<bool> SendVerificationEmailAsync(string toEmail, string confirmationLink)
        {
            var subject = "Xác nhận email đăng ký - Mái Ấm Tình Thương";
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #FF6B9D 0%, #C44569 100%); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .button {{ display: inline-block; padding: 12px 30px; background: #FF6B9D; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>❤️ Mái Ấm Tình Thương</h1>
                        </div>
                        <div class='content'>
                            <h2>Xác nhận địa chỉ email của bạn</h2>
                            <p>Xin chào,</p>
                            <p>Cảm ơn bạn đã đăng ký tài khoản tại <strong>Mái Ấm Tình Thương</strong>!</p>
                            <p>Vui lòng click vào nút bên dưới để xác nhận địa chỉ email của bạn:</p>
                            <p style='text-align: center;'>
                                <a href='{confirmationLink}' class='button'>Xác nhận email</a>
                            </p>
                            <p>Hoặc copy và dán link sau vào trình duyệt:</p>
                            <p style='word-break: break-all; color: #666;'>{confirmationLink}</p>
                            <p><strong>Lưu ý:</strong> Link này sẽ hết hạn sau 24 giờ.</p>
                            <p>Nếu bạn không đăng ký tài khoản này, vui lòng bỏ qua email này.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2025 Mái Ấm Tình Thương. Tất cả quyền được bảo lưu.</p>
                        </div>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendVerificationCodeAsync(string toEmail, string code)
        {
            var subject = "Mã xác nhận đăng ký - Mái Ấm Tình Thương";
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #FF6B9D 0%, #C44569 100%); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .code {{ background: white; border: 2px dashed #FF6B9D; padding: 20px; text-align: center; font-size: 32px; font-weight: bold; color: #FF6B9D; letter-spacing: 5px; margin: 20px 0; border-radius: 5px; }}
                        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>❤️ Mái Ấm Tình Thương</h1>
                        </div>
                        <div class='content'>
                            <h2>Mã xác nhận đăng ký</h2>
                            <p>Xin chào,</p>
                            <p>Cảm ơn bạn đã đăng ký tài khoản tại <strong>Mái Ấm Tình Thương</strong>!</p>
                            <p>Mã xác nhận của bạn là:</p>
                            <div class='code'>{code}</div>
                            <p><strong>Lưu ý:</strong> Mã này sẽ hết hạn sau 10 phút.</p>
                            <p>Nếu bạn không đăng ký tài khoản này, vui lòng bỏ qua email này.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2025 Mái Ấm Tình Thương. Tất cả quyền được bảo lưu.</p>
                        </div>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var subject = "Đặt lại mật khẩu - Mái Ấm Tình Thương";
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #FF6B9D 0%, #C44569 100%); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .button {{ display: inline-block; padding: 12px 30px; background: #FF6B9D; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
                        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 5px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>❤️ Mái Ấm Tình Thương</h1>
                        </div>
                        <div class='content'>
                            <h2>Đặt lại mật khẩu</h2>
                            <p>Xin chào,</p>
                            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản <strong>{toEmail}</strong> tại <strong>Mái Ấm Tình Thương</strong>.</p>
                            <p>Vui lòng click vào nút bên dưới để đặt lại mật khẩu của bạn:</p>
                            <p style='text-align: center;'>
                                <a href='{resetLink}' class='button'>Đặt lại mật khẩu</a>
                            </p>
                            <p>Hoặc copy và dán link sau vào trình duyệt:</p>
                            <p style='word-break: break-all; color: #666;'>{resetLink}</p>
                            <div class='warning'>
                                <p><strong>⚠️ Lưu ý quan trọng:</strong></p>
                                <ul>
                                    <li>Link này sẽ hết hạn sau 1 giờ.</li>
                                    <li>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</li>
                                    <li>Mật khẩu của bạn sẽ không thay đổi cho đến khi bạn click vào link và tạo mật khẩu mới.</li>
                                </ul>
                            </div>
                            <p>Nếu bạn gặp vấn đề, vui lòng liên hệ với chúng tôi.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2025 Mái Ấm Tình Thương. Tất cả quyền được bảo lưu.</p>
                        </div>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }
    }
}

