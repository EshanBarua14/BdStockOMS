using System.Text;
using BdStockOMS.API.Data;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── DATABASE ───────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// ── AUTH SERVICES ──────────────────────────────
// Register IAuthService → AuthService
// When controller asks for IAuthService,
// DI gives it an AuthService instance
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStockService, StockService>(); 
// Scoped = one instance per HTTP request

// ── JWT AUTHENTICATION ─────────────────────────
// NEW — tell ASP.NET how to validate JWT tokens
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(options =>
{
    // Default scheme = JWT Bearer
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Validate that token was issued by US
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],

        // Validate that token is for OUR app
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],

        // Validate the signature — most important
        // Ensures token wasn't tampered with
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey)
        ),

        // Validate token hasn't expired
        ValidateLifetime = true,

        // No clock skew — token expires exactly on time
        ClockSkew = TimeSpan.Zero
    };
});

// ── SWAGGER ────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BD Stock OMS API",
        Version = "v1",
        Description = "Bangladesh Stock Exchange — Order Management System"
    });

    // Tell Swagger about JWT auth
    // This adds "Authorize" button to Swagger UI
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "BD Stock OMS API v1"
        );
        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

// ORDER MATTERS — Authentication before Authorization
app.UseAuthentication();
// UseAuthentication = reads the JWT token
// from request header, validates it,
// populates User.Claims

app.UseAuthorization();
// UseAuthorization = checks [Authorize] attributes
// uses the claims from UseAuthentication

app.MapControllers();
app.Run();