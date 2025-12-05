using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MatchType = MaiAmTinhThuong.Models.MatchType;

[Route("api/[controller]")]
[ApiController]
public class RuleBotController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RuleBotController> _logger;

    public RuleBotController(ApplicationDbContext db, ILogger<RuleBotController> logger)
    {
        _db = db;
        _logger = logger;
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

        // fallback
        return Ok(new
        {
            reply = "Xin lỗi, mình chưa hiểu. Bạn thử hỏi khác hoặc liên hệ: MaiAmYeuThuong@gmail.com"
        });
    }
}
