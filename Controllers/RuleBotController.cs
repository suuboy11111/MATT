using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using MaiAmTinhThuong.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MatchType = MaiAmTinhThuong.Models.MatchType;

[Route("api/[controller]")]
[ApiController]
public class RuleBotController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RuleBotController> _logger;
    private readonly GeminiService? _geminiService;

    public RuleBotController(ApplicationDbContext db, ILogger<RuleBotController> logger, IServiceProvider serviceProvider)
    {
        _db = db;
        _logger = logger;
        // Lấy GeminiService nếu có (optional)
        try
        {
            _geminiService = serviceProvider.GetService<GeminiService>();
            if (_geminiService != null)
            {
                _logger.LogInformation("GeminiService successfully injected");
            }
            else
            {
                _logger.LogWarning("GeminiService is null - check service registration and configuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting GeminiService from service provider");
            _geminiService = null;
        }
    }

    public class UserMessage
    {
        public string Message { get; set; } = "";
    }

    // Hàm bỏ dấu tiếng Việt
    public static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var normalized = text.Normalize(NormalizationForm.FormD);
        var chars = normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);

        return new string(chars.ToArray()).Normalize(NormalizationForm.FormC);
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] UserMessage req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { reply = "Vui lòng nhập câu hỏi." });

        string input = req.Message.Trim().ToLowerInvariant();
        string inputNoSign = RemoveDiacritics(input);

        // Lấy rules active
        var rules = await _db.BotRules
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.Id)
            .ToListAsync();

        foreach (var r in rules)
        {
            try
            {
                string trigger = r.Trigger.ToLowerInvariant().Trim();
                string triggerNoSign = RemoveDiacritics(trigger);

                switch (r.MatchType)
                {
                    case MatchType.Exact:
                        if (inputNoSign == triggerNoSign)
                            return Ok(new { reply = r.Response });
                        break;

                    case MatchType.Contains:
                        if (inputNoSign.Contains(triggerNoSign))
                            return Ok(new { reply = r.Response });
                        break;

                    case MatchType.Regex:
                        try
                        {
                            var pattern = triggerNoSign;

                            var rg = new Regex(
                                pattern,
                                RegexOptions.IgnoreCase | RegexOptions.Compiled,
                                TimeSpan.FromMilliseconds(200)
                            );

                            if (rg.IsMatch(inputNoSign))
                                return Ok(new { reply = r.Response });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Regex error for rule {Id}: {Msg}", r.Id, ex.Message);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating bot rule {Id}", r.Id);
            }
        }

        // Fallback: Thử dùng Gemini AI nếu có
        if (_geminiService != null)
        {
            _logger.LogInformation("Gemini service available, attempting AI response");
            try
            {
                var contextPrompt = $@"Bạn là trợ lý ảo của Mái Ấm Tình Thương - một tổ chức từ thiện giúp đỡ trẻ em và gia đình khó khăn.

Thông tin về Mái Ấm:
- Email: MaiAmYeuThuong@gmail.com
- Số điện thoại: (+84) 902115231
- Giờ làm việc: 8:00 - 17:00 (Thứ 2 - Thứ 7)
- Website: Hỗ trợ đóng góp tài chính, vật tư, chỗ ở

Hãy trả lời câu hỏi của người dùng một cách thân thiện, ngắn gọn (tối đa 150 từ), và hướng dẫn họ liên hệ nếu cần hỗ trợ cụ thể.

Câu hỏi: {req.Message}";
                
                _logger.LogInformation("Calling Gemini API with prompt length: {Length}", contextPrompt.Length);
                var aiReply = await _geminiService.ChatAsync(contextPrompt);
                _logger.LogInformation("Gemini API response received: {Length} chars", aiReply?.Length ?? 0);
                
                if (!string.IsNullOrWhiteSpace(aiReply) && !aiReply.StartsWith("Lỗi"))
                {
                    _logger.LogInformation("Using Gemini AI response");
                    return Ok(new { reply = aiReply });
                }
                else
                {
                    _logger.LogWarning("Gemini returned empty or error response: {Reply}", aiReply ?? "null");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini AI fallback failed, using default message");
            }
        }
        else
        {
            _logger.LogWarning("Gemini service is null - not configured or not injected");
        }

        // Fallback cuối cùng
        return Ok(new
        {
            reply = "Xin lỗi, mình chưa hiểu câu hỏi của bạn. Bạn có thể:\n• Hỏi về cách đóng góp\n• Hỏi về giờ làm việc\n• Liên hệ trực tiếp: (+84) 902115231 hoặc MaiAmYeuThuong@gmail.com"
        });
    }
}
