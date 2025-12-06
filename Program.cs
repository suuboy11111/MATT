using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using MaiAmTinhThuong.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register custom services
builder.Services.AddScoped<MaiAmTinhThuong.Services.SupportRequestService>();
builder.Services.AddScoped<MaiAmTinhThuong.Services.SupporterService>();
builder.Services.AddScoped<MaiAmTinhThuong.Services.NotificationService>();

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
        options.UseNpgsql(connectionString));
    Console.WriteLine("PostgreSQL database configured");
}
else if (!usePostgreSQL && !string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
    Console.WriteLine("SQL Server database configured");
}
else
{
    throw new InvalidOperationException("No valid database connection string found. Please set DATABASE_URL environment variable.");
}

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // Thêm session middleware

app.UseAuthentication(); // Thêm dòng này để login hoạt động
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
            
            // QUAN TRỌNG: Chạy migration TRƯỚC để tạo các bảng cơ bản (AspNetUsers, etc.)
            try
            {
                db.Database.Migrate();
                logger.LogInformation("✅ Database migration completed successfully.");
                
                // Kiểm tra xem AspNetUsers đã được tạo chưa
                var checkConnection = db.Database.GetDbConnection();
                await checkConnection.OpenAsync();
                using var checkCommand = checkConnection.CreateCommand();
                checkCommand.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'AspNetUsers'";
                var tableExists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;
                await checkConnection.CloseAsync();
                
                if (!tableExists)
                {
                    logger.LogWarning("⚠️ AspNetUsers table not found after migration. This might be a fresh database.");
                    // Tiếp tục để thử lại
                    throw new Exception("AspNetUsers table not created by migration");
                }
                
                return true; // Thành công
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("PendingModelChangesWarning"))
            {
                // Có pending changes - cần tạo migration mới hoặc apply
                logger.LogWarning("⚠️ Pending model changes detected. This might require a new migration.");
                // Không return, tiếp tục thử lại
            }
            catch (Exception migrateEx)
            {
                logger.LogWarning(migrateEx, "⚠️ Migration failed, will retry...");
                // Không return, tiếp tục thử lại
            }
            
            // Sau khi migration chạy, kiểm tra và thêm các cột mới nếu chưa có
            var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            
            // Kiểm tra xem đang dùng SQL Server hay PostgreSQL
            var isPostgreSQL = connection.GetType().Name.Contains("Npgsql");
            
            if (isPostgreSQL)
            {
                // PostgreSQL commands - chỉ thêm nếu bảng đã tồn tại
                // Thêm Gender column
                command.CommandText = @"
                    DO $$ 
                    BEGIN
                        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'AspNetUsers')
                           AND NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                          WHERE table_name = 'AspNetUsers' AND column_name = 'Gender') THEN
                            ALTER TABLE ""AspNetUsers"" ADD COLUMN ""Gender"" text;
                        END IF;
                    END $$;";
                await command.ExecuteNonQueryAsync();
            
                // Thêm DateOfBirth column
                command.CommandText = @"
                    DO $$ 
                    BEGIN
                        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'AspNetUsers')
                           AND NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                          WHERE table_name = 'AspNetUsers' AND column_name = 'DateOfBirth') THEN
                            ALTER TABLE ""AspNetUsers"" ADD COLUMN ""DateOfBirth"" timestamp without time zone;
                        END IF;
                    END $$;";
                await command.ExecuteNonQueryAsync();
                
                // Thêm Address column
                command.CommandText = @"
                    DO $$ 
                    BEGIN
                        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'AspNetUsers')
                           AND NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                          WHERE table_name = 'AspNetUsers' AND column_name = 'Address') THEN
                            ALTER TABLE ""AspNetUsers"" ADD COLUMN ""Address"" varchar(200);
                        END IF;
                    END $$;";
                await command.ExecuteNonQueryAsync();
                
                // Thêm PhoneNumber2 column
                command.CommandText = @"
                    DO $$ 
                    BEGIN
                        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'AspNetUsers')
                           AND NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                          WHERE table_name = 'AspNetUsers' AND column_name = 'PhoneNumber2') THEN
                            ALTER TABLE ""AspNetUsers"" ADD COLUMN ""PhoneNumber2"" text;
                        END IF;
                    END $$;";
                await command.ExecuteNonQueryAsync();
                
                // Thêm CreatedAt column
                command.CommandText = @"
                    DO $$ 
                    BEGIN
                        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'AspNetUsers')
                           AND NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                          WHERE table_name = 'AspNetUsers' AND column_name = 'CreatedAt') THEN
                            ALTER TABLE ""AspNetUsers"" ADD COLUMN ""CreatedAt"" timestamp without time zone;
                        END IF;
                    END $$;";
                await command.ExecuteNonQueryAsync();
                
                // Thêm UpdatedAt column
                command.CommandText = @"
                    DO $$ 
                    BEGIN
                        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'AspNetUsers')
                           AND NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                          WHERE table_name = 'AspNetUsers' AND column_name = 'UpdatedAt') THEN
                            ALTER TABLE ""AspNetUsers"" ADD COLUMN ""UpdatedAt"" timestamp without time zone;
                        END IF;
                    END $$;";
                await command.ExecuteNonQueryAsync();
            
            // Tạo bảng Notifications nếu chưa có (PostgreSQL) - CHỈ nếu AspNetUsers đã tồn tại
            command.CommandText = @"
                DO $$ 
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'AspNetUsers')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Notifications') THEN
                        CREATE TABLE ""Notifications"" (
                            ""Id"" SERIAL PRIMARY KEY,
                            ""Title"" varchar(200) NOT NULL,
                            ""Message"" varchar(1000),
                            ""Type"" text NOT NULL,
                            ""UserId"" varchar(450),
                            ""IsRead"" boolean NOT NULL DEFAULT false,
                            ""CreatedAt"" timestamp without time zone NOT NULL,
                            ""Link"" text,
                            CONSTRAINT ""FK_Notifications_AspNetUsers_UserId"" 
                                FOREIGN KEY (""UserId"") REFERENCES ""AspNetUsers"" (""Id"") ON DELETE CASCADE
                        );
                        CREATE INDEX ""IX_Notifications_UserId"" ON ""Notifications"" (""UserId"");
                    END IF;
                END $$;";
            await command.ExecuteNonQueryAsync();
        }
        else
        {
            // SQL Server commands
            // Thêm Gender column
            command.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'Gender')
                BEGIN
                    ALTER TABLE [dbo].[AspNetUsers] ADD [Gender] nvarchar(max) NULL;
                END";
            await command.ExecuteNonQueryAsync();
        
            // Thêm DateOfBirth column
            command.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'DateOfBirth')
                BEGIN
                    ALTER TABLE [dbo].[AspNetUsers] ADD [DateOfBirth] datetime2 NULL;
                END";
            await command.ExecuteNonQueryAsync();
            
            // Thêm Address column
            command.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'Address')
                BEGIN
                    ALTER TABLE [dbo].[AspNetUsers] ADD [Address] nvarchar(200) NULL;
                END";
            await command.ExecuteNonQueryAsync();
            
            // Thêm PhoneNumber2 column
            command.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'PhoneNumber2')
                BEGIN
                    ALTER TABLE [dbo].[AspNetUsers] ADD [PhoneNumber2] nvarchar(max) NULL;
                END";
            await command.ExecuteNonQueryAsync();
            
            // Thêm CreatedAt column
            command.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'CreatedAt')
                BEGIN
                    ALTER TABLE [dbo].[AspNetUsers] ADD [CreatedAt] datetime2 NULL;
                END";
            await command.ExecuteNonQueryAsync();
            
            // Thêm UpdatedAt column
            command.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'UpdatedAt')
                BEGIN
                    ALTER TABLE [dbo].[AspNetUsers] ADD [UpdatedAt] datetime2 NULL;
                END";
            await command.ExecuteNonQueryAsync();
            
            // Tạo bảng Notifications nếu chưa có (SQL Server) - CHỈ nếu AspNetUsers đã tồn tại
            command.CommandText = @"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUsers')
                   AND NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
                BEGIN
                    CREATE TABLE [dbo].[Notifications] (
                        [Id] int IDENTITY(1,1) NOT NULL,
                        [Title] nvarchar(200) NOT NULL,
                        [Message] nvarchar(1000) NULL,
                        [Type] nvarchar(max) NOT NULL,
                        [UserId] nvarchar(450) NULL,
                        [IsRead] bit NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [Link] nvarchar(max) NULL,
                        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Notifications_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_Notifications_UserId] ON [dbo].[Notifications] ([UserId]);
                END";
            await command.ExecuteNonQueryAsync();
        }
        
                await connection.CloseAsync();
                
                logger.LogInformation("✅ Database setup completed successfully.");
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

        // Tạo user admin
        var adminEmail = "admin@localhost.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            var user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Quản trị viên",
                ProfilePicture = "default1-avatar.png",  // Cung cấp giá trị mặc định
                Role = "Admin",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(user, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Admin");
                logger.LogInformation("Admin user created successfully.");
            }
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
