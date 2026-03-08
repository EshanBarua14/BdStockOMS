using BdStockOMS.API.Repositories;
using BdStockOMS.API.Repositories.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;
using BdStockOMS.API.Data;
using BdStockOMS.API.Middleware;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ── DATABASE ───────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// ── REDIS ──────────────────────────────────────
var redisConn = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConn));
builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = redisConn);

// ── SERVICES ───────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICCDService, CCDService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ICommissionCalculatorService, CommissionCalculatorService>();
builder.Services.AddScoped<IRMSValidationService, RMSValidationService>();
builder.Services.AddScoped<IFundRequestService, FundRequestService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IWatchlistService, WatchlistService>();
builder.Services.AddScoped<IPortfolioPnlService, PortfolioPnlService>();
builder.Services.AddScoped<IBrokerageReportService, BrokerageReportService>();
builder.Services.AddScoped<ISystemSettingService, SystemSettingService>();
builder.Services.AddScoped<IOrderAmendmentService, OrderAmendmentService>();
builder.Services.AddScoped<ITraderReassignmentService, TraderReassignmentService>();
builder.Services.AddScoped<IMarketDataService, MarketDataService>();
builder.Services.AddScoped<ICorporateActionService, CorporateActionService>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();

// ── REPOSITORIES ──────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// ── BACKGROUND SERVICES ────────────────────────
builder.Services.AddHostedService<BdStockOMS.API.BackgroundServices.StockPriceUpdateService>();
builder.Services.AddHostedService<BdStockOMS.API.BackgroundServices.AccountUnlockService>();

// ── SIGNALR ────────────────────────────────────
builder.Services.AddSignalR();

// ── JWT AUTHENTICATION ─────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey   = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidIssuer              = jwtSettings["Issuer"],
        ValidateAudience         = true,
        ValidAudience            = jwtSettings["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = new SymmetricSecurityKey(
                                       Encoding.UTF8.GetBytes(secretKey)),
        ValidateLifetime         = true,
        ClockSkew                = TimeSpan.Zero
    };
});

// ── CORS ───────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("BdStockOMSPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "https://*.vercel.app")
              .SetIsOriginAllowedToAllowWildcardSubdomains()
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
// ── RATE LIMITING (built-in ASP.NET Core) ─────────
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", o =>
    {
        o.PermitLimit  = 5;
        o.Window       = TimeSpan.FromMinutes(15);
        o.QueueLimit   = 0;
    });
    options.AddFixedWindowLimiter("api", o =>
    {
        o.PermitLimit  = 100;
        o.Window       = TimeSpan.FromMinutes(1);
        o.QueueLimit   = 0;
    });
    options.RejectionStatusCode = 429;
});

// ── HEALTH CHECKS ────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BdStockOMS.API.Data.AppDbContext>(name: "sqlserver", tags: new[] { "db", "sql" })
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379",
        name: "redis",
        tags: new[] { "cache", "redis" });

// ── SWAGGER ────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title       = "BD Stock OMS API",
        Version     = "v1",
        Description = "Bangladesh Stock Exchange — Order Management System"
    });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name        = "Authorization",
        Type        = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme      = "Bearer",
        BearerFormat = "JWT",
        In          = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token here"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── MIDDLEWARE PIPELINE (ORDER MATTERS) ────────
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "BD Stock OMS API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseCors("BdStockOMSPolicy");
app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<TokenBlacklistMiddleware>();
app.UseAuthorization();
app.UseMiddleware<IdempotencyMiddleware>();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status    = report.Status.ToString(),
            checks    = report.Entries.Select(e => new
            {
                name      = e.Key,
                status    = e.Value.Status.ToString(),
                duration  = e.Value.Duration.TotalMilliseconds
            }),
            timestamp = DateTime.UtcNow
        };
        await context.Response.WriteAsJsonAsync(result);
    }
});
app.MapControllers();
app.MapHub<BdStockOMS.API.Hubs.StockPriceHub>("/hubs/stockprice");
app.Run();
