using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
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
            var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUser = _configuration["Email:SmtpUser"];
            var smtpPassword = _configuration["Email:SmtpPassword"];
            var fromEmail = _configuration["Email:FromEmail"] ?? smtpUser;
            var fromName = _configuration["Email:FromName"] ?? "Mái Ấm Tình Thương";

            if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("Email configuration not found. Email sending disabled.");
                return false;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body
            };
            message.Body = bodyBuilder.ToMessageBody();

            // Thử cả port 587 (StartTLS) và 465 (SSL) nếu port 587 fail
            var portsToTry = new[] { (smtpPort, SecureSocketOptions.StartTls) };
            
            // Nếu đang dùng port 587, thử thêm port 465 với SSL
            if (smtpPort == 587)
            {
                portsToTry = new[] 
                { 
                    (587, SecureSocketOptions.StartTls),
                    (465, SecureSocketOptions.SslOnConnect)
                };
            }

            Exception? lastException = null;

            foreach (var (port, secureOption) in portsToTry)
            {
                try
                {
                    using var client = new SmtpClient();
                    // Tăng timeout lên 30 giây
                    client.Timeout = 30000;
                    
                    _logger.LogInformation($"Attempting to connect to SMTP server: {smtpHost}:{port} with {secureOption}");
                    
                    // Thử kết nối với timeout
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    await client.ConnectAsync(smtpHost, port, secureOption, cts.Token);
                    
                    _logger.LogInformation("Connected successfully. Authenticating...");
                    await client.AuthenticateAsync(smtpUser, smtpPassword, cts.Token);
                    
                    _logger.LogInformation($"Sending email to {toEmail}...");
                    await client.SendAsync(message, cts.Token);
                    
                    _logger.LogInformation("Disconnecting from SMTP server...");
                    await client.DisconnectAsync(true, cts.Token);

                    _logger.LogInformation($"Email sent successfully to {toEmail} via {smtpHost}:{port}");
                    return true;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning($"Connection timeout to {smtpHost}:{port}");
                    lastException = new TimeoutException($"Connection to {smtpHost}:{port} timed out");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to send via {smtpHost}:{port} - {ex.Message}");
                    lastException = ex;
                    // Tiếp tục thử port tiếp theo nếu có
                }
            }

            // Nếu tất cả đều fail
            _logger.LogError(lastException, $"Failed to send email to {toEmail} after trying all methods");
            return false;
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
    }
}

