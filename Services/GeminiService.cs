using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MaiAmTinhThuong.Services
{
    public class GeminiService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly ILogger<GeminiService>? _logger;

        public GeminiService(HttpClient http, IConfiguration config, ILogger<GeminiService>? logger = null)
        {
            _http = http;
            _apiKey = config["GeminiApi:ApiKey"] ?? "";
            _model = config["GeminiApi:Model"] ?? "gemini-1.5-flash"; // Mặc định dùng Flash (nhanh, miễn phí tốt)
            _logger = logger;
        }

        public async Task<string> ChatAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger?.LogWarning("Gemini API key not configured");
                return ""; // Trả về empty để fallback về rule-based
            }

            try
            {
                // Google Gemini API endpoint
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

                // Request format theo Google Gemini API
                var request = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        topK = 40,
                        topP = 0.95,
                        maxOutputTokens = 500
                    }
                };

                var response = await _http.PostAsJsonAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return ""; // Trả về empty để fallback
                }

                var result = await response.Content.ReadFromJsonAsync<GeminiApiResponse>();

                if (result?.Candidates != null && result.Candidates.Length > 0)
                {
                    var text = result.Candidates[0].Content?.Parts?[0]?.Text ?? "";
                    return text.Trim();
                }

                return "";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calling Gemini API");
                return ""; // Trả về empty để fallback về rule-based
            }
        }
    }

    // Response model theo Google Gemini API format
    public class GeminiApiResponse
    {
        public GeminiCandidate[]? Candidates { get; set; }
    }

    public class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }

    public class GeminiContent
    {
        public GeminiPart[]? Parts { get; set; }
    }

    public class GeminiPart
    {
        public string? Text { get; set; }
    }
}
