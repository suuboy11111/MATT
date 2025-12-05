using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MaiAmTinhThuong.Services
{
    public class GeminiService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public GeminiService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _apiKey = config["GeminiApi:ApiKey"];
            _baseUrl = config["GeminiApi:BaseUrl"];
        }

        public async Task<string> ChatAsync(string prompt)
        {
            try
            {
                var request = new
                {
                    model = "gemini-1.5",
                    prompt = prompt,
                    max_tokens = 200
                };

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, _baseUrl);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                httpRequest.Content = JsonContent.Create(request);

                var response = await _http.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                    return $"Lỗi API: {response.StatusCode}";

                var result = await response.Content.ReadFromJsonAsync<GeminiResponse>();
                return result?.Message ?? "";
            }
            catch (Exception ex)
            {
                // Tránh crash web, log lỗi nếu cần
                return $"Lỗi khi gọi Gemini: {ex.Message}";
            }
        }

    }

    public class GeminiResponse
    {
        public string Message { get; set; }
    }
}
