using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

namespace MaiAmTinhThuong.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<PaymentController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
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
                // Tạo order code duy nhất
                var orderCode = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                // Lấy base URL - ưu tiên từ config (ngrok URL), nếu không có thì dùng Request
                var baseUrl = _configuration["PayOS:BaseUrl"];
                if (string.IsNullOrEmpty(baseUrl))
                {
                    baseUrl = $"{Request.Scheme}://{Request.Host}";
                }
                
                // Tạo payment link - sử dụng HttpClient trực tiếp (đơn giản hơn SDK)
                var clientId = _configuration["PayOS:ClientId"];
                var apiKey = _configuration["PayOS:ApiKey"];
                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(apiKey))
                {
                    return Json(new { success = false, message = "Thiếu cấu hình PayOS: ClientId hoặc ApiKey" });
                }
                
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
                httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                
                // Body theo chuẩn PayOS (camelCase, có items)
                var paymentRequest = new
                {
                    orderCode = orderCode,
                    amount = (int)request.Amount,
                    description = $"Ủng hộ tài chính - {request.DonorName}",
                    items = new[]
                    {
                        new { name = "Ủng hộ tài chính", quantity = 1, price = (int)request.Amount }
                    },
                    returnUrl = $"{baseUrl}/Payment/Success?orderCode={orderCode}",
                    cancelUrl = $"{baseUrl}/Payment/Cancel",
                    buyerName = request.DonorName,
                    buyerPhone = request.PhoneNumber
                };

                var json = JsonSerializer.Serialize(paymentRequest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                // PayOS production endpoint (một số môi trường chặn DNS api.payos.vn -> dùng api-merchant.payos.vn)
                var payOsEndpoint = _configuration["PayOS:Endpoint"] ?? "https://api-merchant.payos.vn";
                var response = await httpClient.PostAsync($"{payOsEndpoint}/v2/payment-requests", content);
                var responseBody = await response.Content.ReadAsStringAsync();
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

                    // Rollback transaction insert nếu cần? (để đơn giản giữ lại record Pending)
                    return Json(new { success = false, message = "Có lỗi xảy ra khi tạo liên kết thanh toán: " + errorMessage });
                }

                // Lưu thông tin giao dịch vào database
                var description = $"Ủng hộ tài chính - {request.DonorName}";
                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    description += $" - SĐT: {request.PhoneNumber}";
                }
                description += $" - OrderCode: {orderCode}";

                var transaction = new TransactionHistory
                {
                    MaiAmId = request.MaiAmId ?? 1, // Mặc định mái ấm đầu tiên nếu không chọn
                    Amount = request.Amount,
                    TransactionDate = DateTime.UtcNow,
                    Status = "Pending",
                    Description = description
                };

                _context.TransactionHistories.Add(transaction);
                await _context.SaveChangesAsync();

                // Lưu orderCode vào session để tra cứu sau
                HttpContext.Session.SetString($"OrderCode_{orderCode}", transaction.Id.ToString());
                
                // Lưu orderCode và transactionId vào Description để tra cứu từ webhook
                var updatedDescription = $"Ủng hộ tài chính - {request.DonorName}";
                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    updatedDescription += $" - SĐT: {request.PhoneNumber}";
                }
                updatedDescription += $" - OrderCode: {orderCode} - TransactionId: {transaction.Id}";
                transaction.Description = updatedDescription;
                await _context.SaveChangesAsync();

                // Lấy CheckoutUrl từ response (đã kiểm tra tồn tại ở trên)
                var checkoutUrl = checkoutElement.GetString();
                
                return Json(new
                {
                    success = true,
                    checkoutUrl = checkoutUrl,
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

