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
// Railway cung cấp DATABASE_URL, ưu tiên sử dụng nó
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Nếu có DATABASE_URL từ Railway, convert sang format Npgsql
if (!string.IsNullOrEmpty(databaseUrl) && databaseUrl.Contains("postgresql://"))
{
    // Railway DATABASE_URL format: postgresql://user:password@host:port/database
    // Convert sang Npgsql format: Host=host;Port=port;Database=database;Username=user;Password=password
    var uri = new Uri(databaseUrl);
    var host = uri.Host;
    var dbPort = uri.Port;
    var database = uri.AbsolutePath.TrimStart('/');
    var username = uri.UserInfo.Split(':')[0];
    var password = uri.UserInfo.Split(':').Length > 1 ? uri.UserInfo.Split(':')[1] : "";
    
    connectionString = $"Host={host};Port={dbPort};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
}

if (!string.IsNullOrEmpty(connectionString) && 
    (connectionString.Contains("postgres", StringComparison.OrdinalIgnoreCase) || 
     connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
     connectionString.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase) ||
     connectionString.Contains("postgresql://", StringComparison.OrdinalIgnoreCase)))
{
    // Sử dụng PostgreSQL (cho Railway, Render, etc.)
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // Sử dụng SQL Server (mặc định cho local)
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString ?? "Server=localhost;Database=HoTroNguoiCaoTuoiDB;Trusted_Connection=True;TrustServerCertificate=True;"));
}

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Đăng ký PayOSClient
builder.Services.AddSingleton<PayOSClient>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var clientId = configuration["PayOS:ClientId"];
    var apiKey = configuration["PayOS:ApiKey"];
    var checksumKey = configuration["PayOS:ChecksumKey"];

    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(checksumKey))
    {
        throw new InvalidOperationException("PayOS configuration is missing. Please check appsettings.json");
    }

    return new PayOSClient(clientId, apiKey, checksumKey);
});


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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Tự động chạy migration khi khởi động (Production)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        // Kiểm tra và thêm các cột mới nếu chưa có
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        
        // Kiểm tra xem đang dùng SQL Server hay PostgreSQL
        var isPostgreSQL = connection.GetType().Name.Contains("Npgsql");
        
        if (isPostgreSQL)
        {
            // PostgreSQL commands
            // Thêm Gender column
            command.CommandText = @"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'AspNetUsers' AND column_name = 'Gender') THEN
                        ALTER TABLE ""AspNetUsers"" ADD COLUMN ""Gender"" text;
                    END IF;
                END $$;";
            await command.ExecuteNonQueryAsync();
            
            // Thêm DateOfBirth column
            command.CommandText = @"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'AspNetUsers' AND column_name = 'DateOfBirth') THEN
                        ALTER TABLE ""AspNetUsers"" ADD COLUMN ""DateOfBirth"" timestamp without time zone;
                    END IF;
                END $$;";
            await command.ExecuteNonQueryAsync();
            
            // Thêm Address column
            command.CommandText = @"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'AspNetUsers' AND column_name = 'Address') THEN
                        ALTER TABLE ""AspNetUsers"" ADD COLUMN ""Address"" varchar(200);
                    END IF;
                END $$;";
            await command.ExecuteNonQueryAsync();
            
            // Thêm PhoneNumber2 column
            command.CommandText = @"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'AspNetUsers' AND column_name = 'PhoneNumber2') THEN
                        ALTER TABLE ""AspNetUsers"" ADD COLUMN ""PhoneNumber2"" text;
                    END IF;
                END $$;";
            await command.ExecuteNonQueryAsync();
            
            // Thêm CreatedAt column
            command.CommandText = @"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'AspNetUsers' AND column_name = 'CreatedAt') THEN
                        ALTER TABLE ""AspNetUsers"" ADD COLUMN ""CreatedAt"" timestamp without time zone;
                    END IF;
                END $$;";
            await command.ExecuteNonQueryAsync();
            
            // Thêm UpdatedAt column
            command.CommandText = @"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'AspNetUsers' AND column_name = 'UpdatedAt') THEN
                        ALTER TABLE ""AspNetUsers"" ADD COLUMN ""UpdatedAt"" timestamp without time zone;
                    END IF;
                END $$;";
            await command.ExecuteNonQueryAsync();
            
            // Tạo bảng Notifications nếu chưa có (PostgreSQL)
            command.CommandText = @"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Notifications') THEN
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
            
            // Tạo bảng Notifications nếu chưa có (SQL Server)
            command.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
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
        
        // Chạy migration tự động - bỏ qua warning về pending changes
        try
        {
            db.Database.Migrate();
            logger.LogInformation("Database migration completed successfully.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("PendingModelChangesWarning"))
        {
            // Bỏ qua warning về pending changes - đã xử lý thủ công ở trên
            logger.LogWarning("Skipping pending model changes warning - columns already added manually.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database.");
        // Không throw exception để ứng dụng vẫn có thể chạy
    }
}

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

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
        }
    }
}
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
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
    }
}


// Configure port for Railway (Railway sets PORT environment variable)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    app.Urls.Add($"http://0.0.0.0:{port}");
}

app.Run();
