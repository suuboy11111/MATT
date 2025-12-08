using Microsoft.Extensions.Caching.Memory;

namespace MaiAmTinhThuong.Services
{
    public class VerificationCodeService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<VerificationCodeService> _logger;

        public VerificationCodeService(IMemoryCache cache, ILogger<VerificationCodeService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Tạo mã xác nhận 6 số ngẫu nhiên
        /// </summary>
        public string GenerateCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        /// <summary>
        /// Lưu mã xác nhận vào cache với thời gian hết hạn 10 phút
        /// </summary>
        public void StoreCode(string email, string code, string userId)
        {
            var cacheKey = $"verification_code_{email}";
            var cacheEntry = new VerificationCodeCacheEntry
            {
                Code = code,
                UserId = userId,
                Email = email,
                CreatedAt = DateTime.UtcNow
            };

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) // Mã hết hạn sau 10 phút
            };

            _cache.Set(cacheKey, cacheEntry, options);
            _logger.LogInformation($"Verification code stored for {email}");
        }

        /// <summary>
        /// Xác thực mã xác nhận
        /// </summary>
        public VerificationCodeCacheEntry? VerifyCode(string email, string code)
        {
            var cacheKey = $"verification_code_{email}";
            
            if (!_cache.TryGetValue(cacheKey, out VerificationCodeCacheEntry? cacheEntry))
            {
                _logger.LogWarning($"Verification code not found or expired for {email}");
                return null;
            }

            if (cacheEntry == null || cacheEntry.Code != code)
            {
                _logger.LogWarning($"Invalid verification code for {email}");
                return null;
            }

            // Xóa mã sau khi xác thực thành công
            _cache.Remove(cacheKey);
            _logger.LogInformation($"Verification code verified successfully for {email}");
            
            return cacheEntry;
        }

        /// <summary>
        /// Xóa mã xác nhận (dùng khi cần resend)
        /// </summary>
        public void RemoveCode(string email)
        {
            var cacheKey = $"verification_code_{email}";
            _cache.Remove(cacheKey);
            _logger.LogInformation($"Verification code removed for {email}");
        }
    }

    public class VerificationCodeCacheEntry
    {
        public string Code { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}






