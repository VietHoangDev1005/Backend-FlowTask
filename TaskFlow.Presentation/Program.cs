using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using TaskFlow.Infrastructure.DBContext;

// Log khởi động: ghi được cả khi cấu hình bị lỗi trước lúc Serilog đọc appsettings.
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var config = builder.Configuration;

    // ---------- Serilog (cấu hình ở mục "Serilog" trong appsettings) ----------
    builder.Host.UseSerilog((context, services, logger) => logger
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // ---------- PostgreSQL (EF Core, Database First) ----------
    var postgres = config.GetConnectionString("Postgres");
    if (string.IsNullOrWhiteSpace(postgres))
        throw new InvalidOperationException("Thiếu ConnectionStrings:Postgres (điền trong appsettings.Development.json hoặc biến môi trường ConnectionStrings__Postgres).");
    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(postgres));

    // ---------- Redis (Upstash): OTP, bộ đếm, khóa chặn email ----------
    var redis = config.GetConnectionString("Redis");
    if (string.IsNullOrWhiteSpace(redis))
        throw new InvalidOperationException("Thiếu ConnectionStrings:Redis.");
    // abortConnect=False trong chuỗi kết nối: app vẫn khởi động khi Redis chưa sẵn sàng, tự nối lại sau.
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redis));

    // ---------- JWT ----------
    var jwtKey = config["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
        throw new InvalidOperationException("Thiếu Jwt:Key hoặc ngắn hơn 32 ký tự.");
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = config["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = config["Jwt:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
            // SignalR (WebSocket) không gửi được header Authorization, nhận token qua query ?access_token=
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var token = ctx.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(token) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        ctx.Token = token;
                    return Task.CompletedTask;
                }
            };
        });
    builder.Services.AddAuthorization();

    // ---------- CORS (Web trên Vercel gọi sang API) ----------
    var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    {
        if (origins.Length > 0)
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    }));

    // ---------- API, Swagger ----------
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Render đã chặn HTTP bên ngoài và tự gắn HTTPS, chỉ chuyển hướng khi chạy local có HTTPS.
    if (app.Environment.IsDevelopment())
        app.UseHttpsRedirection();

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // ---------- /health: kiểm tra PostgreSQL và Redis ----------
    // Kết quả Redis được nhớ 60 giây để không tốn hạn mức lệnh của Upstash.
    var redisCheck = (At: DateTime.MinValue, Ok: false);
    app.MapGet("/health", async (AppDbContext db, IConnectionMultiplexer mux) =>
    {
        var dbOk = await db.Database.CanConnectAsync();

        if (DateTime.UtcNow - redisCheck.At > TimeSpan.FromSeconds(60))
        {
            bool ok;
            try { await mux.GetDatabase().PingAsync(); ok = true; }
            catch { ok = false; }
            redisCheck = (DateTime.UtcNow, ok);
        }

        var result = new { status = dbOk && redisCheck.Ok ? "healthy" : "unhealthy", postgres = dbOk, redis = redisCheck.Ok };
        return dbOk && redisCheck.Ok ? Results.Ok(result) : Results.Json(result, statusCode: 503);
    }).AllowAnonymous();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Ứng dụng dừng ngay khi khởi động");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
