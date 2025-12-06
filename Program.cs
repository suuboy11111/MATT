using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using MaiAmTinhThuong.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using PayOS;
using MatchType = MaiAmTinhThuong.Models.MatchType;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient<GeminiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10); // 10s timeout
});


// Add services to the container.
builder.Services.AddControllersWithViews();

// QUAN TRỌNG: Cấu hình Cookie Policy để đảm bảo tất cả cookies được set đúng SameSite
// Điều này ảnh hưởng đến correlation cookie của OAuth
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // Cho phép SameSite=None cho cross-site requests (cần cho OAuth)
    options.MinimumSameSitePolicy = SameSiteMode.Unspecified;

    // Chỉ đảm bảo Secure cho cookie SameSite=None, không can thiệp correlation/external
    options.OnAppendCookie = cookieContext =>
    {
        if (cookieContext.CookieOptions.SameSite == SameSiteMode.None &&
            cookieContext.CookieOptions.Secure == false)
        {
            cookieContext.CookieOptions.Secure = true;
        }
    };
});

// Cấu hình Data Protection (QUAN TRỌNG cho OAuth state encryption)
// OAuth state được mã hóa bằng Data Protection keys
// Lưu ý: Keys phải giống nhau giữa request đi (GoogleLogin) và request về (GoogleCallback)
// QUAN TRỌNG: Railway có thể có multiple instances, mỗi instance có keys khác nhau
// → Phải persist keys vào database để tất cả instances dùng chung keys
var dataProtectionBuilder = builder.Services.AddDataProtection();

// Cấu hình Session (QUAN TRỌNG cho OAuth state)
// Lưu ý: OAuth state được lưu trong correlation cookie, không phải session
// Nhưng session vẫn cần cho các tính năng khác
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // QUAN TRỌNG: OAuth cần SameSite=None với Secure=true trong production
    if (builder.Environment.IsDevelopment())
    {
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
    }
    else
    {
        // Production: SameSite=None và Secure=true (bắt buộc cho OAuth)
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    }
    options.Cookie.Name = ".MaiAmTinhThuong.Session";
    options.Cookie.Path = "/"; // Đảm bảo cookie được gửi cho tất cả paths
});

// Register custom services
builder.Services.AddScoped<MaiAmTinhThuong.Services.SupportRequestService>();
builder.Services.AddScoped<MaiAmTinhThuong.Services.SupporterService>();
builder.Services.AddScoped<MaiAmTinhThuong.Services.NotificationService>();
builder.Services.AddScoped<MaiAmTinhThuong.Services.EmailService>();

// Hỗ trợ cả SQL Server và PostgreSQL
// Railway cung cấp DATABASE_URL (internal) hoặc DATABASE_PUBLIC_URL (public), ưu tiên sử dụng internal
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var databasePublicUrl = Environment.GetEnvironmentVariable("DATABASE_PUBLIC_URL");

string? connectionString = null;
bool usePostgreSQL = false;

// Ưu tiên DATABASE_URL (internal) từ Railway
if (string.IsNullOrEmpty(databaseUrl))
{
    // Nếu không có DATABASE_URL, thử dùng DATABASE_PUBLIC_URL (fallback)
    databaseUrl = databasePublicUrl;
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        Console.WriteLine("⚠️ Using DATABASE_PUBLIC_URL (fallback). Consider using DATABASE_URL for better performance.");
    }
}

if (!string.IsNullOrEmpty(databaseUrl))
{
    Console.WriteLine($"DATABASE_URL found: {databaseUrl.Substring(0, Math.Min(50, databaseUrl.Length))}...");
    
    if (databaseUrl.Contains("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        // Railway DATABASE_URL format: postgresql://user:password@host:port/database
        // Convert sang Npgsql format: Host=host;Port=port;Database=database;Username=user;Password=password
        try
        {
            var uri = new Uri(databaseUrl);
            var host = uri.Host;
            var dbPort = uri.Port;
            var database = uri.AbsolutePath.TrimStart('/');
            var username = uri.UserInfo.Split(':')[0];
            var password = uri.UserInfo.Split(':').Length > 1 ? uri.UserInfo.Split(':')[1] : "";
            
            connectionString = $"Host={host};Port={dbPort};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
            usePostgreSQL = true;
            Console.WriteLine($"✅ PostgreSQL connection configured: Host={host}, Database={database}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error parsing DATABASE_URL: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine($"⚠️ DATABASE_URL found but doesn't contain 'postgresql://'. Value: {databaseUrl.Substring(0, Math.Min(100, databaseUrl.Length))}...");
    }
}
else
{
    Console.WriteLine("❌ DATABASE_URL and DATABASE_PUBLIC_URL are both empty or not set.");
}

// Nếu không có DATABASE_URL hoặc DATABASE_PUBLIC_URL, thử đọc từ config (chỉ cho local development)
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    
    // Chỉ dùng SQL Server nếu không phải production và connection string có chứa SQL Server indicators
    if (!string.IsNullOrEmpty(connectionString) && 
        !connectionString.Contains("postgres", StringComparison.OrdinalIgnoreCase) &&
        !connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
    {
        // Chỉ dùng SQL Server nếu đang ở local development
        if (builder.Environment.IsDevelopment())
        {
            usePostgreSQL = false;
            Console.WriteLine("Using SQL Server for local development");
        }
        else
        {
            // Production nhưng không có DATABASE_URL hoặc DATABASE_PUBLIC_URL -> lỗi
            Console.WriteLine("❌ Production environment detected but no DATABASE_URL or DATABASE_PUBLIC_URL found.");
            Console.WriteLine("💡 Please set DATABASE_URL in Railway Variables tab:");
            Console.WriteLine("   Name: DATABASE_URL");
            Console.WriteLine("   Value: ${{Postgres.DATABASE_URL}}");
            throw new InvalidOperationException("DATABASE_URL or DATABASE_PUBLIC_URL environment variable is required for production deployment. Please set DATABASE_URL in Railway environment variables using ${{Postgres.DATABASE_URL}}.");
        }
    }
    else if (!string.IsNullOrEmpty(connectionString) && 
             (connectionString.Contains("postgres", StringComparison.OrdinalIgnoreCase) || 
              connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)))
    {
        usePostgreSQL = true;
        Console.WriteLine("PostgreSQL connection from config");
    }
}

// Configure database
if (usePostgreSQL && !string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseNpgsql(connectionString);
        // Suppress PendingModelChangesWarning để cho phép migration chạy
        options.ConfigureWarnings(warnings => 
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    });
    Console.WriteLine("PostgreSQL database configured");
}
else if (!usePostgreSQL && !string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
        // Suppress PendingModelChangesWarning để cho phép migration chạy
        options.ConfigureWarnings(warnings => 
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    });
    Console.WriteLine("SQL Server database configured");
}
else
{
    throw new InvalidOperationException("No valid database connection string found. Please set DATABASE_URL environment variable.");
}

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Không yêu cầu xác nhận khi đăng nhập (chỉ cần email đã được xác nhận từ lúc đăng ký)
    options.SignIn.RequireConfirmedEmail = false; // Tắt yêu cầu xác nhận email tự động, sẽ kiểm tra thủ công trong Register
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// QUAN TRỌNG: Persist Data Protection keys vào database
// Điều này đảm bảo tất cả instances trên Railway dùng chung keys
// → OAuth state sẽ được mã hóa/giải mã đúng giữa các instances
dataProtectionBuilder.PersistKeysToDbContext<ApplicationDbContext>();

// QUAN TRỌNG: Set application name để đảm bảo keys được isolate đúng
// Đổi application name mới để tránh reuse key cũ (tránh lỗi state)
dataProtectionBuilder.SetApplicationName("MaiAmTinhThuongProd");

// QUAN TRỌNG: Set default key lifetime (keys sẽ expire sau 90 ngày)
dataProtectionBuilder.SetDefaultKeyLifetime(TimeSpan.FromDays(90));

Console.WriteLine("✅ Data Protection keys will be persisted to database (shared across all instances)");

// Cấu hình cookie cho authentication (QUAN TRỌNG cho OAuth)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    if (builder.Environment.IsDevelopment())
    {
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
    }
    else
    {
        // Production: SameSite=None và Secure=true (BẮT BUỘC cho OAuth)
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    }
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
});

// Đảm bảo external cookie (Identity.External) cho OAuth callback được gửi cross-site
builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.Name = ".AspNetCore.External";
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.Path = "/";
    options.Cookie.Domain = "matt-production.up.railway.app";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
});

// Google OAuth
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.CallbackPath = "/Account/GoogleCallback";
            options.SaveTokens = true;
            
            // QUAN TRỌNG: Cấu hình correlation cookie (OAuth state được lưu trong cookie này)
            // Cookie này phải được gửi đi và nhận về giữa app và Google
            // Vấn đề: Railway có thể có multiple instances, cookie phải được set đúng để hoạt động
            options.CorrelationCookie.HttpOnly = true;
            options.CorrelationCookie.Path = "/"; // Đảm bảo cookie được gửi cho tất cả paths
            options.CorrelationCookie.MaxAge = TimeSpan.FromMinutes(10); // Set timeout đủ dài cho OAuth flow
            options.CorrelationCookie.IsEssential = true; // Đánh dấu cookie là essential để không bị block bởi cookie policy
            
            // QUAN TRỌNG: Detect production dựa trên hostname (Railway có thể không set ASPNETCORE_ENVIRONMENT)
            var isProduction = !builder.Environment.IsDevelopment() || 
                               Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT") != null ||
                               Environment.GetEnvironmentVariable("PORT") != null; // Railway set PORT
            
            if (isProduction)
            {
                // Production: SameSite=None và Secure=true (BẮT BUỘC cho OAuth cross-site redirect)
                // QUAN TRỌNG: Browser chỉ chấp nhận SameSite=None nếu có Secure=true
                options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                options.CorrelationCookie.Domain = "matt-production.up.railway.app";
                
                Console.WriteLine($"✅ Google OAuth Correlation Cookie (Production): SameSite=None, Secure=Always, MaxAge=10min, IsEssential=true");
            }
            else
            {
                // Development: SameSite=Lax và Secure=SameAsRequest
                options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                options.CorrelationCookie.Domain = null;
                Console.WriteLine($"✅ Google OAuth Correlation Cookie (Development): SameSite=Lax, Secure=SameAsRequest, IsEssential=true");
            }
            
            // QUAN TRỌNG: Không set StateDataFormat = null vì sẽ dùng default
            // Default format sẽ dùng Data Protection để mã hóa state
            options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
            {
                OnRemoteFailure = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(context.Failure, "❌ Google OAuth remote failure: {Message}", context.Failure?.Message);
            logger.LogError("🔎 RemoteFailure QueryString: {Query}", context.Request.QueryString.Value);
                    context.Response.Redirect("/Account/Login");
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });
    Console.WriteLine("✅ Google OAuth configured");
}
else
{
    Console.WriteLine("⚠️ Google OAuth configuration not found. Google login will be disabled.");
    Console.WriteLine("💡 To enable Google OAuth, add these environment variables:");
    Console.WriteLine("   - Authentication__Google__ClientId");
    Console.WriteLine("   - Authentication__Google__ClientSecret");
    Console.WriteLine("💡 Note: App will still work without Google OAuth. Users can register/login with email.");
}

// Đăng ký PayOSClient (optional - chỉ đăng ký nếu có config)
var payOSClientId = builder.Configuration["PayOS:ClientId"];
var payOSApiKey = builder.Configuration["PayOS:ApiKey"];
var payOSChecksumKey = builder.Configuration["PayOS:ChecksumKey"];

if (!string.IsNullOrEmpty(payOSClientId) && 
    !string.IsNullOrEmpty(payOSApiKey) && 
    !string.IsNullOrEmpty(payOSChecksumKey))
{
    builder.Services.AddSingleton<PayOSClient>(serviceProvider =>
    {
        return new PayOSClient(payOSClientId, payOSApiKey, payOSChecksumKey);
    });
    Console.WriteLine("✅ PayOS client configured");
}
else
{
    Console.WriteLine("⚠️ PayOS configuration not found. Payment features will be disabled.");
    Console.WriteLine("💡 To enable PayOS, add these environment variables:");
    Console.WriteLine("   - PayOS__ClientId");
    Console.WriteLine("   - PayOS__ApiKey");
    Console.WriteLine("   - PayOS__ChecksumKey");
}


var app = builder.Build();

// QUAN TRỌNG: Configure Forwarded Headers để detect HTTPS đúng cách
// Railway sử dụng reverse proxy, cần forward headers để biết request thực sự là HTTPS
// PHẢI được gọi TRƯỚC tất cả middleware khác
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // Trust Railway proxy (không cần verify symmetry)
    RequireHeaderSymmetry = false,
    // Trust all proxies (Railway internal network)
    KnownNetworks = { },
    KnownProxies = { }
});

// QUAN TRỌNG: Middleware để force HTTPS scheme trong production
// PHẢI được gọi SAU UseForwardedHeaders nhưng TRƯỚC UseCookiePolicy và UseSession
// Đảm bảo Request.Scheme = https trước khi OAuth middleware chạy
app.Use(async (context, next) =>
{
    // Chỉ force HTTPS trong production (Railway)
    var isProduction = !app.Environment.IsDevelopment() || 
                      Environment.GetEnvironmentVariable("PORT") != null;
    
    if (isProduction)
    {
        var originalScheme = context.Request.Scheme;
        var host = context.Request.Host.Host ?? "";
        
        // Kiểm tra X-Forwarded-Proto header (Railway set header này)
        if (context.Request.Headers.ContainsKey("X-Forwarded-Proto"))
        {
            var forwardedProto = context.Request.Headers["X-Forwarded-Proto"].ToString();
            if (forwardedProto == "https")
            {
                // Force scheme thành https để cookies được set đúng
                context.Request.Scheme = "https";
            }
        }
        // Nếu không có header nhưng là Railway domain, vẫn force https
        else if (host.Contains("railway.app"))
        {
            context.Request.Scheme = "https";
        }
        
        // QUAN TRỌNG: Đảm bảo IsHttps được set đúng
        // Một số middleware dựa vào IsHttps thay vì Scheme
        if (context.Request.Scheme == "https")
        {
            context.Request.IsHttps = true;
        }
        
        // Log để debug
        if (originalScheme != context.Request.Scheme)
        {
            Console.WriteLine($"🔄 Force HTTPS: {originalScheme} -> {context.Request.Scheme} (Host: {host})");
        }
    }
    
    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// QUAN TRỌNG: Cookie Policy middleware phải được gọi TRƯỚC UseRouting
// Để đảm bảo cookie policy được áp dụng cho tất cả requests
// LƯU Ý: Cookie Policy có thể can thiệp vào correlation cookie
// Nên đảm bảo correlation cookie đã được set IsEssential=true
// QUAN TRỌNG: Cookie Policy được gọi SAU UseForwardedHeaders và force HTTPS middleware
// để đảm bảo Request.Scheme = https trước khi cookie policy chạy
app.UseCookiePolicy();

app.UseRouting();

// QUAN TRỌNG: Middleware order cho OAuth
// 1. Session phải được gọi TRƯỚC Authentication để OAuth state được lưu
// 2. Authentication phải được gọi TRƯỚC Authorization
app.UseSession();

// Log nhanh cookies khi callback để kiểm tra correlation/external cookie có quay lại hay không
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/Account/GoogleCallback"))
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var cookies = string.Join(", ", context.Request.Cookies.Keys);
        var corrKey = context.Request.Cookies.Keys.FirstOrDefault(k => k.StartsWith(".AspNetCore.Correlation", StringComparison.OrdinalIgnoreCase));
        var corrVal = string.IsNullOrEmpty(corrKey) ? "" : context.Request.Cookies[corrKey];
        logger.LogInformation("🔎 OAuth callback cookies: {Cookies}", cookies);
        logger.LogInformation("🔎 Correlation cookie present (prefix check): {HasCorr}", !string.IsNullOrEmpty(corrKey));
        logger.LogInformation("🔎 Correlation cookie value (trim): {CorrVal}", string.IsNullOrEmpty(corrVal) ? "" : corrVal.Substring(0, Math.Min(50, corrVal.Length)));
        logger.LogInformation("🔎 External cookie present: {HasExternal}", context.Request.Cookies.ContainsKey(".AspNetCore.External"));
        logger.LogInformation("🔎 Callback QueryString: {Query}", context.Request.QueryString.Value);
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// Tự động chạy migration TRƯỚC KHI app start - với retry logic
// QUAN TRỌNG: Migration phải hoàn thành trước khi app nhận request
Console.WriteLine("🔄 Starting database migration...");

// Helper method để chạy migration đồng bộ
static async Task<bool> RunMigrationAsync(WebApplication app, int maxRetries, TimeSpan initialDelay)
{
    var delay = initialDelay;
    
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation($"🔄 Attempting database migration (attempt {attempt}/{maxRetries})...");
            
            // Kiểm tra xem có pending migrations không
            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation($"📦 Found {pendingMigrations.Count()} pending migration(s). Applying...");
                foreach (var migration in pendingMigrations)
                {
                    logger.LogInformation($"   - {migration}");
                }
            }
            else
            {
                logger.LogInformation("✅ No pending migrations found.");
            }
            
            // Apply migrations
            await db.Database.MigrateAsync();
            logger.LogInformation("✅ Database migration completed successfully.");
            
            // Kiểm tra xem các bảng quan trọng đã được tạo chưa
            var checkConnection = db.Database.GetDbConnection();
            await checkConnection.OpenAsync();
            using var checkCommand = checkConnection.CreateCommand();
            
            // Kiểm tra AspNetUsers
            checkCommand.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'AspNetUsers'";
            var aspNetUsersExists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;
            
            // Kiểm tra MaiAms
            checkCommand.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'MaiAms'";
            var maiAmsExists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;
            
            // Kiểm tra BlogPosts
            checkCommand.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'BlogPosts'";
            var blogPostsExists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;
            
            await checkConnection.CloseAsync();
            
            if (!aspNetUsersExists)
            {
                logger.LogWarning("⚠️ AspNetUsers table not found after migration.");
                throw new Exception("AspNetUsers table not created by migration");
            }
            
            if (!maiAmsExists)
            {
                logger.LogWarning("⚠️ MaiAms table not found after migration.");
                throw new Exception("MaiAms table not created by migration");
            }
            
            if (!blogPostsExists)
            {
                logger.LogWarning("⚠️ BlogPosts table not found after migration.");
                throw new Exception("BlogPosts table not created by migration");
            }
            
            logger.LogInformation("✅ All required tables exist: AspNetUsers, MaiAms, BlogPosts");
            return true; // Thành công
        }
        catch (Exception ex)
        {
            using var errorScope = app.Services.CreateScope();
            var errorLogger = errorScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            
            if (attempt < maxRetries)
            {
                errorLogger.LogWarning(ex, "❌ Database migration failed (attempt {Attempt}/{MaxRetries}). Retrying in {Delay} seconds...", attempt, maxRetries, delay.TotalSeconds);
                await Task.Delay(delay);
                delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2); // Exponential backoff
            }
            else
            {
                errorLogger.LogError(ex, "❌ Failed to migrate database after {MaxRetries} attempts. Application will start but database operations may fail.", maxRetries);
                return false; // Thất bại nhưng không throw exception
            }
        }
    }
    
    return false; // Nếu đến đây nghĩa là thất bại
}

// Chạy migration đồng bộ (blocking)
var migrationSuccess = RunMigrationAsync(app, maxRetries: 10, initialDelay: TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();

if (migrationSuccess)
{
    Console.WriteLine("✅ Database migration completed. Starting application...");
    
    // QUAN TRỌNG: Đảm bảo Data Protection keys được tạo trong database
    // Keys sẽ được tạo tự động khi lần đầu sử dụng, nhưng trigger ngay để đảm bảo
    try
    {
        using var scope = app.Services.CreateScope();
        var dataProtectionProvider = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        // Tạo một protector để trigger việc tạo keys
        var protector = dataProtectionProvider.CreateProtector("MaiAmTinhThuong.OAuth");
        var testData = "test";
        var protectedData = protector.Protect(testData);
        var unprotectedData = protector.Unprotect(protectedData);
        
        if (unprotectedData == testData)
        {
            logger.LogInformation("✅ Data Protection keys verified and ready for OAuth");
        }
        else
        {
            logger.LogWarning("⚠️ Data Protection keys test failed, but continuing...");
        }
    }
    catch (Exception ex)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "⚠️ Could not verify Data Protection keys, but continuing. Keys will be created on first use.");
    }
}
else
{
    Console.WriteLine("⚠️ Database migration failed but application will continue...");
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed data - chạy trong background để không block startup
_ = Task.Run(async () =>
{
    await Task.Delay(TimeSpan.FromSeconds(10)); // Đợi database sẵn sàng
    
    try
    {
        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // Tạo role Admin nếu chưa có
        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        // Tạo user admin - có thể override bằng environment variable
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@maiamtinhthuong.vn";
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin@123456";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            var user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Quản trị viên",
                ProfilePicture = "/images/default1-avatar.png",
                Role = "Admin",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(user, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Admin");
                logger.LogInformation($"✅ Admin user created successfully. Email: {adminEmail}");
            }
            else
            {
                logger.LogError($"❌ Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            logger.LogInformation($"ℹ️ Admin user already exists: {adminEmail}");
        }
    }
    catch (Exception ex)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error creating admin user: {Message}", ex.Message);
    }
    
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        if (!db.BotRules.Any())
        {
            db.BotRules.AddRange(new[]
            {
                new BotRule { Trigger = "xin chào", MatchType = MatchType.Contains, Response = "Chào bạn! Mình có thể giúp gì cho bạn?", Priority = 100 },
                new BotRule { Trigger = "đóng góp", MatchType = MatchType.Contains, Response = "Bạn có thể đóng góp tại /DongGop hoặc liên hệ số (+84) 902115231.", Priority = 90 },
                new BotRule { Trigger = "giờ làm việc", MatchType = MatchType.Exact, Response = "Mái Ấm mở cửa: 8:00 - 17:00 (T2-T7).", Priority = 80 },
                new BotRule { Trigger = "lien he", MatchType = MatchType.Regex, Response = "Bạn có thể gọi (+84)902115231 hoặc email MaiAmYeuThuong@gmail.com", Priority = 70 }
            });
            db.SaveChanges();
            logger.LogInformation("Bot rules seeded successfully.");
        }
        
        // Seed SupportTypes nếu chưa có
        if (!db.SupportTypes.Any())
        {
            db.SupportTypes.AddRange(new[]
            {
                new SupportType { Name = "Tài chính" },
                new SupportType { Name = "Vật tư" },
                new SupportType { Name = "Chỗ ở" }
            });
            db.SaveChanges();
            logger.LogInformation("Support types seeded successfully.");
        }
    }
    catch (Exception ex)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error seeding data: {Message}", ex.Message);
    }
});


// Configure port for Railway (Railway sets PORT environment variable)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    app.Urls.Add($"http://0.0.0.0:{port}");
}

app.Run();
