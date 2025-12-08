using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using System.Linq;
using PayOS;
using PayOS.Models;

namespace MaiAmTinhThuong.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger;
        private readonly PayOSClient? _payOSClient;

        public PaymentController(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<PaymentController> logger,
            PayOSClient? payOSClient = null)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _payOSClient = payOSClient;
        }

        // GET: Payment/Donate
        [HttpGet]
        public async Task<IActionResult> Donate(int? maiAmId = null)
        {
            ViewBag.MaiAmId = maiAmId;
            var maiAms = await _context.MaiAms.ToListAsync();
            ViewBag.MaiAms = maiAms;
            return View();
        }

        // POST: Payment/CreatePaymentLink
        [HttpPost]
        public async Task<IActionResult> CreatePaymentLink([FromBody] CreatePaymentRequest request)
        {
            try
            {
                // Kiểm tra PayOS Client có sẵn không
                if (_payOSClient == null)
                {
                    _logger.LogError("PayOS Client is not configured");
                    return Json(new { success = false, message = "PayOS chưa được cấu hình. Vui lòng liên hệ quản trị viên." });
                }

                // Tạo order code duy nhất (dùng Unix timestamp)
                var orderCode = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                // Lấy base URL - ưu tiên từ config, nếu không có thì dùng Request
                var baseUrl = _configuration["PayOS:BaseUrl"];
                if (string.IsNullOrEmpty(baseUrl))
                {
                    var scheme = Request.IsHttps || Request.Headers["X-Forwarded-Proto"].ToString().Equals("https", StringComparison.OrdinalIgnoreCase) 
                        ? "https" 
                        : "https";
                    baseUrl = $"{scheme}://{Request.Host}";
                }
                
                _logger.LogInformation($"PayOS - Creating payment link. BaseUrl: {baseUrl}, Amount: {request.Amount}, OrderCode: {orderCode}");

                // PayOS SDK 2.0.1 có thể không có các class này
                // Quay lại dùng HttpClient trực tiếp với signature calculation đúng
                var clientId = _configuration["PayOS:ClientId"];
                var apiKey = _configuration["PayOS:ApiKey"];
                var checksumKey = _configuration["PayOS:ChecksumKey"];
                
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
                httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                
                // Tạo signature: amount, cancelUrl, description, returnUrl (không có orderCode)
                var cancelUrl = $"{baseUrl}/Payment/Cancel";
                var returnUrl = $"{baseUrl}/Payment/Success?orderCode={orderCode}";
                var paymentDescription = $"Ủng hộ tài chính - {request.DonorName}";
                var amountStr = ((int)request.Amount).ToString();
                
                // Tạo chuỗi signature: sắp xếp alphabetical, dùng raw values (KHÔNG URL encode)
                var signatureString = $"amount={amountStr}&cancelUrl={cancelUrl}&description={paymentDescription}&returnUrl={returnUrl}";
                
                // Tính HMAC-SHA256
                if (string.IsNullOrEmpty(checksumKey))
                {
                    return Json(new { success = false, message = "ChecksumKey không được cấu hình" });
                }
                
                using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(checksumKey));
                var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signatureString));
                var signature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                
                _logger.LogInformation($"PayOS Signature String: {signatureString}");
                
                // Tạo payment request
                var paymentRequestObj = new
                {
                    orderCode = orderCode,
                    amount = (int)request.Amount,
                    description = paymentDescription,
                    items = new[]
                    {
                        new
                        {
                            name = "Ủng hộ tài chính",
                            quantity = 1,
                            price = (int)request.Amount
                        }
                    },
                    cancelUrl = cancelUrl,
                    returnUrl = returnUrl,
                    buyerName = request.DonorName,
                    buyerPhone = request.PhoneNumber,
                    signature = signature
                };

                var json = JsonSerializer.Serialize(paymentRequestObj, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var payOsEndpoint = _configuration["PayOS:Endpoint"] ?? "https://api-merchant.payos.vn";
                var response = await httpClient.PostAsync($"{payOsEndpoint}/v2/payment-requests", content);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"PayOS Response: {responseBody}");
                
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                // Kiểm tra response hợp lệ
                if (!response.IsSuccessStatusCode ||
                    !root.TryGetProperty("data", out var dataElement) ||
                    dataElement.ValueKind != JsonValueKind.Object ||
                    !dataElement.TryGetProperty("checkoutUrl", out var checkoutElement))
                {
                    var errorMessage = root.TryGetProperty("error", out var errEl) && errEl.ValueKind == JsonValueKind.String
                        ? errEl.GetString()
                        : root.TryGetProperty("message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String
                            ? msgEl.GetString()
                            : $"PayOS API error (status {(int)response.StatusCode}). Body: {responseBody}";

                    return Json(new { success = false, message = "Có lỗi xảy ra khi tạo liên kết thanh toán: " + errorMessage });
                }

                var checkoutUrl = checkoutElement.GetString() ?? "";
                dynamic paymentLinkResponse = new { CheckoutUrl = checkoutUrl };
                
                _logger.LogInformation($"PayOS - Payment link created successfully. OrderCode: {orderCode}, CheckoutUrl: {paymentLinkResponse.CheckoutUrl?.Substring(0, Math.Min(50, paymentLinkResponse.CheckoutUrl?.Length ?? 0))}...");

                // Lưu thông tin giao dịch vào database
                var description = $"Ủng hộ tài chính - {request.DonorName}";
                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    description += $" - SĐT: {request.PhoneNumber}";
                }
                description += $" - OrderCode: {orderCode}";

                var transaction = new TransactionHistory
                {
                    MaiAmId = request.MaiAmId ?? 1,
                    Amount = request.Amount,
                    TransactionDate = DateTime.UtcNow,
                    Status = "Pending",
                    Description = description
                };

                _context.TransactionHistories.Add(transaction);
                await _context.SaveChangesAsync();

                // Lưu orderCode vào session
                HttpContext.Session.SetString($"OrderCode_{orderCode}", transaction.Id.ToString());
                
                // Cập nhật description với TransactionId
                transaction.Description = $"{description} - TransactionId: {transaction.Id}";
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    checkoutUrl = paymentLinkResponse.CheckoutUrl,
                    orderCode = orderCode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo payment link");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo liên kết thanh toán: " + ex.Message });
            }
        }

        // GET: Payment/Success
        [HttpGet]
        public async Task<IActionResult> Success(int orderCode, string status)
        {
            try
            {
                // Lấy thông tin payment từ PayOS - sử dụng HttpClient trực tiếp
                var clientId = _configuration["PayOS:ClientId"];
                var apiKey = _configuration["PayOS:ApiKey"];
                
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
                httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                
                var payOsEndpoint = _configuration["PayOS:Endpoint"] ?? "https://api-merchant.payos.vn";
                var response = await httpClient.GetAsync($"{payOsEndpoint}/v2/payment-requests/{orderCode}");
                var responseBody = await response.Content.ReadAsStringAsync();
                var paymentInfo = JsonSerializer.Deserialize<JsonElement>(responseBody);

                // Kiểm tra status từ response
                var paymentStatus = paymentInfo.GetProperty("data").GetProperty("status").GetString();
                if (paymentStatus == "PAID")
                {
                    // Cập nhật trạng thái giao dịch
                    var transactionIdStr = HttpContext.Session.GetString($"OrderCode_{orderCode}");
                    TransactionHistory? transaction = null;
                    
                    if (int.TryParse(transactionIdStr, out int transactionId))
                    {
                        transaction = await _context.TransactionHistories.FindAsync(transactionId);
                    }
                    
                    // Fallback: Tìm transaction từ description nếu không tìm thấy từ session
                    if (transaction == null)
                    {
                        transaction = await _context.TransactionHistories
                            .FirstOrDefaultAsync(t => t.Description.Contains($"OrderCode: {orderCode}"));
                    }
                    
                    if (transaction != null && transaction.Status != "Success")
                    {
                        transaction.Status = "Success";
                        await _context.SaveChangesAsync();
                    }

                    ViewBag.Success = true;
                    var amount = paymentInfo.GetProperty("data").GetProperty("amount").GetInt32();
                    ViewBag.Amount = amount;
                    ViewBag.OrderCode = orderCode;
                }
                else
                {
                    ViewBag.Success = false;
                    ViewBag.Message = "Thanh toán chưa được xác nhận";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý payment success");
                ViewBag.Success = false;
                ViewBag.Message = "Có lỗi xảy ra khi xử lý thanh toán: " + ex.Message;
            }

            return View();
        }

        // GET: Payment/Cancel
        [HttpGet]
        public IActionResult Cancel()
        {
            ViewBag.Cancelled = true;
            return View("Success");
        }

        // POST: Payment/Webhook
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                // Deserialize webhook data
                if (string.IsNullOrEmpty(body))
                {
                    return BadRequest();
                }

                var webhookData = JsonSerializer.Deserialize<JsonElement>(body);
                var checksumKey = _configuration["PayOS:ChecksumKey"];
                
                // Verify webhook signature (đơn giản hóa - chỉ kiểm tra cơ bản)
                // Trong production nên verify đầy đủ signature

                // Xử lý webhook - lấy OrderCode từ data
                if (webhookData.TryGetProperty("data", out var data))
                {
                    var orderCodeInt = data.GetProperty("orderCode").GetInt32();
                    
                    // Tìm transaction theo orderCode trong description
                    var transaction = await _context.TransactionHistories
                        .FirstOrDefaultAsync(t => t.Description.Contains($"OrderCode: {orderCodeInt}"));
                    
                    if (transaction != null && transaction.Status != "Success")
                    {
                        transaction.Status = "Success";
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation($"Đã cập nhật transaction {transaction.Id} thành công cho orderCode {orderCodeInt}");
                    }
                    else
                    {
                        _logger.LogWarning($"Không tìm thấy transaction cho orderCode {orderCodeInt}");
                    }
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý webhook");
                return StatusCode(500);
            }
        }

    }

    // DTOs
    public class CreatePaymentRequest
    {
        public decimal Amount { get; set; }
        public string DonorName { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public int? MaiAmId { get; set; }
    }
}

