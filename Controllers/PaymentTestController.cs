using Microsoft.AspNetCore.Mvc;
using PayOS;
using PayOS.Models;

namespace MaiAmTinhThuong.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentTestController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly PayOSClient _payOSClient;
        private readonly ILogger<PaymentTestController> _logger;

        public PaymentTestController(
            IConfiguration configuration, 
            PayOSClient payOSClient,
            ILogger<PaymentTestController> logger)
        {
            _configuration = configuration;
            _payOSClient = payOSClient;
            _logger = logger;
        }

        [HttpGet("check-config")]
        public IActionResult CheckConfig()
        {
            var clientId = _configuration["PayOS:ClientId"];
            var apiKey = _configuration["PayOS:ApiKey"];
            var checksumKey = _configuration["PayOS:ChecksumKey"];

            var result = new
            {
                hasClientId = !string.IsNullOrEmpty(clientId),
                hasApiKey = !string.IsNullOrEmpty(apiKey),
                hasChecksumKey = !string.IsNullOrEmpty(checksumKey),
                clientIdLength = clientId?.Length ?? 0,
                apiKeyLength = apiKey?.Length ?? 0,
                checksumKeyLength = checksumKey?.Length ?? 0,
                isPlaceholder = clientId == "YOUR_CLIENT_ID" || 
                               apiKey == "YOUR_API_KEY" || 
                               checksumKey == "YOUR_CHECKSUM_KEY",
                isValid = !string.IsNullOrEmpty(clientId) && 
                         !string.IsNullOrEmpty(apiKey) && 
                         !string.IsNullOrEmpty(checksumKey) &&
                         clientId != "YOUR_CLIENT_ID" &&
                         apiKey != "YOUR_API_KEY" &&
                         checksumKey != "YOUR_CHECKSUM_KEY"
            };

            return Ok(result);
        }

        [HttpPost("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var clientId = _configuration["PayOS:ClientId"];
                var apiKey = _configuration["PayOS:ApiKey"];
                var checksumKey = _configuration["PayOS:ChecksumKey"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(checksumKey))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Thiếu thông tin cấu hình PayOS",
                        details = new
                        {
                            hasClientId = !string.IsNullOrEmpty(clientId),
                            hasApiKey = !string.IsNullOrEmpty(apiKey),
                            hasChecksumKey = !string.IsNullOrEmpty(checksumKey)
                        }
                    });
                }

                if (clientId == "YOUR_CLIENT_ID" || apiKey == "YOUR_API_KEY" || checksumKey == "YOUR_CHECKSUM_KEY")
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Vui lòng thay thế các placeholder bằng key thực tế từ PayOS" 
                    });
                }

                // Tạo test order code
                var testOrderCode = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                
                // Tạo payment request - sử dụng object anonymous
                var paymentRequest = new
                {
                    OrderCode = testOrderCode,
                    Amount = 1000, // Test với 1000 VND
                    Description = "Test kết nối PayOS",
                    ReturnUrl = "https://your-url.com/payment/success",
                    CancelUrl = "https://your-url.com/payment/cancel"
                };
                
                // Tạo payment link - sử dụng PostAsync với RequestOptions
                var requestOptions = new RequestOptions<object>();
                requestOptions.Data = paymentRequest;
                var paymentLink = await _payOSClient.PostAsync<dynamic, object>("/v2/payment-requests", requestOptions);

                // Lấy CheckoutUrl từ response
                var checkoutUrl = paymentLink?.GetType().GetProperty("CheckoutUrl")?.GetValue(paymentLink)?.ToString() 
                    ?? paymentLink?.GetType().GetProperty("checkoutUrl")?.GetValue(paymentLink)?.ToString();
                
                return Ok(new
                {
                    success = true,
                    message = "Kết nối PayOS thành công!",
                    checkoutUrl = checkoutUrl,
                    orderCode = testOrderCode,
                    note = "Đây chỉ là test, bạn có thể hủy thanh toán sau khi vào trang thanh toán"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi test kết nối PayOS");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi kết nối PayOS",
                    error = ex.Message,
                    details = "Kiểm tra lại Client ID, API Key và Checksum Key"
                });
            }
        }
    }
}

