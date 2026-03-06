using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.Auth;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BdStockOMS.API.Services;

// Interface — defines what AuthService can do
// Controller depends on this interface
// not the concrete class (good practice)
public interface IAuthService
{
    Task<AuthResponseDto> RegisterBrokerageAsync(RegisterBrokerageDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
}

public class AuthService : IAuthService
{
    // DbContext — to talk to database
    private readonly AppDbContext _context;

    // IConfiguration — to read appsettings.json
    // We need JWT secret key from there
    private readonly IConfiguration _configuration;

    // Constructor — DI injects these automatically
    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // ── REGISTER BROKERAGE ────────────────────────
    public async Task<AuthResponseDto> RegisterBrokerageAsync(
        RegisterBrokerageDto dto)
    {
        // Check if email already exists
        // We don't want two accounts with same email
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == dto.Email);

        if (emailExists)
            throw new InvalidOperationException(
                "Email already registered"
            );

        // Check license number is unique
        var licenseExists = await _context.BrokerageHouses
            .AnyAsync(b => b.LicenseNumber == dto.LicenseNumber);

        if (licenseExists)
            throw new InvalidOperationException(
                "License number already registered"
            );

        // Create the BrokerageHouse record
        var brokerageHouse = new BrokerageHouse
        {
            Name = dto.FirmName,
            LicenseNumber = dto.LicenseNumber,
            Email = dto.FirmEmail,
            Phone = dto.FirmPhone,
            Address = dto.FirmAddress,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.BrokerageHouses.AddAsync(brokerageHouse);
        // SaveChanges here so brokerageHouse.Id is populated
        await _context.SaveChangesAsync();

        // Get the BrokerageHouse role ID
        // RoleId = 1 based on our seed data
        var brokerageRole = await _context.Roles
            .FirstAsync(r => r.Name == "BrokerageHouse");

        // Hash the password using BCrypt
        // BCrypt.HashPassword = one-way hash
        // workFactor 11 = how slow to compute
        // (slower = harder for hackers to crack)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(
            dto.Password,
            workFactor: 11
        );

        // Create the user for this brokerage
        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = passwordHash,
            Phone = dto.Phone,
            RoleId = brokerageRole.Id,
            BrokerageHouseId = brokerageHouse.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Generate and return JWT token
        return GenerateToken(user, brokerageRole.Name,
            brokerageHouse.Name);
    }

    // ── LOGIN ─────────────────────────────────────
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        // Find user by email
        // Include = also load related Role and BrokerageHouse
        // (EF Core joins automatically)
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.BrokerageHouse)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        // User not found
        if (user == null)
            throw new InvalidOperationException(
                "Invalid email or password"
            );

        // User is deactivated
        if (!user.IsActive)
            throw new InvalidOperationException(
                "Account is deactivated"
            );

        // User is locked by IT Support
        if (user.IsLocked)
            throw new InvalidOperationException(
                "Account is locked. Contact IT Support"
            );

        // Verify password against stored hash
        // BCrypt.Verify = hashes the input and
        // compares to stored hash
        var passwordValid = BCrypt.Net.BCrypt.Verify(
            dto.Password,
            user.PasswordHash
        );

        if (!passwordValid)
            throw new InvalidOperationException(
                "Invalid email or password"
            );

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Generate and return JWT token
        return GenerateToken(
            user,
            user.Role.Name,
            user.BrokerageHouse.Name
        );
    }

    // ── GENERATE JWT TOKEN ────────────────────────
    // Private — only used inside this service
    private AuthResponseDto GenerateToken(
        User user,
        string roleName,
        string brokerageHouseName)
    {
        // Read JWT settings from appsettings.json
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]!;
        var issuer = jwtSettings["Issuer"]!;
        var audience = jwtSettings["Audience"]!;
        var expiryDays = int.Parse(jwtSettings["ExpiryInDays"]!);

        // Create the signing key from our secret
        // SymmetricSecurityKey = both sides use same key
        // Encoding.UTF8.GetBytes = convert string to bytes
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey)
        );

        // SigningCredentials = how we sign the token
        // HmacSha256 = the hashing algorithm
        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        // Claims = data we put INSIDE the token
        // Anyone can READ these (they're base64 encoded)
        // But no one can MODIFY them (signature protects)
        var claims = new[]
        {
            new Claim("userId",
                user.Id.ToString()),
            new Claim("email",
                user.Email),
            new Claim("role",
                roleName),
            new Claim("brokerageHouseId",
                user.BrokerageHouseId.ToString()),
            new Claim("brokerageHouseName",
                brokerageHouseName),
            new Claim("fullName",
                user.FullName),
            // ClaimTypes.Role = standard claim
            // used by [Authorize(Roles="...")] attribute
            new Claim(ClaimTypes.Role,
                roleName)
        };

        var expiresAt = DateTime.UtcNow.AddDays(expiryDays);

        // Create the actual JWT token object
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        // JwtSecurityTokenHandler = converts token
        // object to the string we return to client
        var tokenString = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return new AuthResponseDto
        {
            Token = tokenString,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = roleName,
            BrokerageHouseId = user.BrokerageHouseId,
            BrokerageHouseName = brokerageHouseName
        };
    }
}