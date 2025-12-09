using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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
            _logger = logger;
            
            // Ưu tiên đọc từ environment variable (Railway), sau đó mới đọc từ config
            _apiKey = System.Environment.GetEnvironmentVariable("GEMINI_API_KEY") 
                     ?? config["GeminiApi:ApiKey"] 
                     ?? "";
            
            // Model name: ưu tiên từ config, sau đó environment variable, cuối cùng là default
            _model = config["GeminiApi:Model"] 
                    ?? System.Environment.GetEnvironmentVariable("GeminiApi_Model")
                    ?? "gemini-2.5-flash"; // Mặc định dùng gemini-2.5-flash (theo tài liệu mới nhất)
            
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger?.LogWarning("Gemini API key is not configured. Set GEMINI_API_KEY environment variable or GeminiApi:ApiKey in appsettings.json");
            }
            else
            {
                _logger?.LogInformation("GeminiService initialized with model: {Model}, API key length: {KeyLength}", _model, _apiKey.Length);
            }
        }

        public async Task<string> ChatAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger?.LogWarning("Gemini API key not configured");
                return ""; // Trả về empty để fallback về rule-based
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                return "";
            }

            try
            {
                _logger?.LogInformation("Calling Gemini API with model: {Model}, prompt length: {Length}", _model, prompt.Length);
                
                // Google Gemini API endpoint theo tài liệu chính thức
                // Sử dụng v1beta và header x-goog-api-key
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";

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
                        maxOutputTokens = 2048 // Tăng lên để tránh bị cắt
                    }
                };

                // Sử dụng header x-goog-api-key thay vì query parameter
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
                httpRequest.Headers.Add("x-goog-api-key", _apiKey);
                httpRequest.Content = JsonContent.Create(request);

                var response = await _http.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return ""; // Trả về empty để fallback
                }

                // Đọc response content để debug
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger?.LogInformation("Gemini API raw response: {Response}", responseContent);

                // Parse JSON response
                var result = JsonSerializer.Deserialize<GeminiApiResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Candidates != null && result.Candidates.Length > 0)
                {
                    var candidate = result.Candidates[0];
                    
                    // Kiểm tra finishReason
                    var finishReason = GetPropertyValue(responseContent, "candidates[0].finishReason");
                    if (finishReason == "MAX_TOKENS")
                    {
                        _logger?.LogWarning("Gemini API response was truncated due to MAX_TOKENS limit");
                    }
                    
                    if (candidate?.Content?.Parts != null && candidate.Content.Parts.Length > 0)
                    {
                        var text = candidate.Content.Parts[0]?.Text ?? "";
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            _logger?.LogInformation("Gemini API response received: {Length} chars", text.Length);
                            return text.Trim();
                        }
                    }
                    
                    // Nếu không có text trong parts, thử parse trực tiếp từ JSON
                    var textFromJson = ExtractTextFromJson(responseContent);
                    if (!string.IsNullOrWhiteSpace(textFromJson))
                    {
                        _logger?.LogInformation("Gemini API response extracted from JSON: {Length} chars", textFromJson.Length);
                        return textFromJson.Trim();
                    }
                    
                    _logger?.LogWarning("Gemini API response has candidates but no text. FinishReason: {Reason}, Response: {Response}", 
                        finishReason ?? "unknown", responseContent);
                }
                else
                {
                    _logger?.LogWarning("Gemini API response has no candidates. Full response: {Response}", responseContent);
                }

                return "";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calling Gemini API: {Error}", ex.Message);
                return ""; // Trả về empty để fallback về rule-based
            }
        }

        private string? GetPropertyValue(string json, string path)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var parts = path.Split('.');
                JsonElement element = doc.RootElement;
                
                foreach (var part in parts)
                {
                    if (part.Contains('[') && part.Contains(']'))
                    {
                        var name = part.Substring(0, part.IndexOf('['));
                        var index = int.Parse(part.Substring(part.IndexOf('[') + 1, part.IndexOf(']') - part.IndexOf('[') - 1));
                        element = element.GetProperty(name)[index];
                    }
                    else
                    {
                        element = element.GetProperty(part);
                    }
                }
                
                return element.GetString();
            }
            catch
            {
                return null;
            }
        }

        private string ExtractTextFromJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var candidate = candidates[0];
                    if (candidate.TryGetProperty("content", out var content))
                    {
                        if (content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                        {
                            var part = parts[0];
                            if (part.TryGetProperty("text", out var text))
                            {
                                return text.GetString() ?? "";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Error extracting text from JSON: {Error}", ex.Message);
            }
            return "";
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
