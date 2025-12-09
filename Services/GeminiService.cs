using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System; // Explicitly import System namespace to avoid ambiguity

namespace MaiAmTinhThuong.Services
{
    public class GeminiService
    {
        private readonly Client? _client;
        private readonly string _model;
        private readonly ILogger<GeminiService>? _logger;

        public GeminiService(IConfiguration config, ILogger<GeminiService>? logger = null)
        {
            _logger = logger;
            
            // Use fully qualified name to avoid ambiguity with Google.GenAI.Types.Environment
            var apiKey = config["GeminiApi:ApiKey"] ?? System.Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
            _model = config["GeminiApi:Model"] ?? "gemini-2.5-flash"; // Mặc định dùng gemini-2.5-flash (theo tài liệu mới nhất)
            
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger?.LogWarning("Gemini API key is not configured. Set GeminiApi:ApiKey in appsettings.json or GEMINI_API_KEY environment variable.");
                _client = null;
            }
            else
            {
                try
                {
                    // Google.GenAI SDK tự động lấy API key từ environment variable GEMINI_API_KEY
                    // Set environment variable cho process hiện tại
                    System.Environment.SetEnvironmentVariable("GEMINI_API_KEY", apiKey);
                    
                    // Khởi tạo Client - SDK sẽ tự động lấy API key từ GEMINI_API_KEY
                    _client = new Client();
                    _logger?.LogInformation("GeminiService initialized with model: {Model}", _model);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to initialize Gemini client: {Error}", ex.Message);
                    _client = null;
                }
            }
        }

        public async Task<string> ChatAsync(string prompt)
        {
            if (_client == null)
            {
                _logger?.LogWarning("Gemini client is not available - API key not configured");
                return ""; // Trả về empty để fallback về rule-based
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                return "";
            }

            try
            {
                _logger?.LogInformation("Calling Gemini API with model: {Model}, prompt length: {Length}", _model, prompt.Length);
                
                // Sử dụng Google.GenAI SDK theo tài liệu chính thức
                // Format: contents có thể là string hoặc Content object
                var response = await _client.Models.GenerateContentAsync(
                    model: _model,
                    contents: prompt
                );

                if (response?.Candidates != null && response.Candidates.Count > 0)
                {
                    var text = response.Candidates[0].Content?.Parts?[0]?.Text ?? "";
                    _logger?.LogInformation("Gemini API response received: {Length} chars", text.Length);
                    return text.Trim();
                }

                _logger?.LogWarning("Gemini API returned empty response");
                return "";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calling Gemini API");
                return ""; // Trả về empty để fallback về rule-based
            }
        }
    }
}
