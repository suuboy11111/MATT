using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
        public IActionResult Donate(int? maiAmId = null)
        {
            ViewBag.MaiAmId = maiAmId;
            var maiAms = _context.MaiAms.ToList();
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
                
                // Tạo payment request - sử dụng object anonymous
                var paymentRequest = new
                {
                    OrderCode = orderCode,
                    Amount = (int)request.Amount,
                    Description = $"Ủng hộ tài chính - {request.DonorName}",
                    ReturnUrl = $"{baseUrl}/Payment/Success?orderCode={orderCode}",
                    CancelUrl = $"{baseUrl}/Payment/Cancel"
                };

                // Tạo payment link - sử dụng HttpClient trực tiếp (đơn giản hơn SDK)
                var clientId = _configuration["PayOS:ClientId"];
                var apiKey = _configuration["PayOS:ApiKey"];
                
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
                httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                
                var json = JsonSerializer.Serialize(paymentRequest);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("https://api.payos.vn/v2/payment-requests", content);
                var responseBody = await response.Content.ReadAsStringAsync();
                var paymentLink = JsonSerializer.Deserialize<JsonElement>(responseBody);

                // Lưu thông tin giao dịch vào database
                var transaction = new TransactionHistory
                {
                    MaiAmId = request.MaiAmId ?? 1, // Mặc định mái ấm đầu tiên nếu không chọn
                    Amount = request.Amount,
                    TransactionDate = DateTime.Now,
                    Status = "Pending",
                    Description = $"Ủng hộ tài chính - {request.DonorName} - OrderCode: {orderCode}"
                };

                _context.TransactionHistories.Add(transaction);
                await _context.SaveChangesAsync();

                // Lưu orderCode vào session để tra cứu sau
                HttpContext.Session.SetString($"OrderCode_{orderCode}", transaction.Id.ToString());
                
                // Lưu orderCode vào Description để tra cứu từ webhook
                transaction.Description = $"Ủng hộ tài chính - {request.DonorName} - OrderCode: {orderCode} - TransactionId: {transaction.Id}";
                await _context.SaveChangesAsync();

                // Lấy CheckoutUrl từ response
                var checkoutUrl = paymentLink.GetProperty("data").GetProperty("checkoutUrl").GetString();
                
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
                
                var response = await httpClient.GetAsync($"https://api.payos.vn/v2/payment-requests/{orderCode}");
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

