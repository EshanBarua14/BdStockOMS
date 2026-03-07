// Services/AuthService.cs
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.Auth;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BdStockOMS.API.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> RegisterBrokerageAsync(RegisterBrokerageDto dto);
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task<User?> GetUserByIdAsync(int userId);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponseDto?> RegisterBrokerageAsync(RegisterBrokerageDto dto)
    {
        // Check duplicate email
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return null;

        // Get BrokerageHouse role (RoleId = 1 from seed)
        var brokerageRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "BrokerageHouse");
        if (brokerageRole == null) return null;

        // Create brokerage house
        var brokerage = new BrokerageHouse
        {
            Name = dto.FirmName,
            LicenseNumber = dto.LicenseNumber,
            Address = dto.FirmAddress,
            Email = dto.FirmEmail,
            Phone = dto.FirmPhone,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.BrokerageHouses.Add(brokerage);
        await _db.SaveChangesAsync();

        // Create user
        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId = brokerageRole.Id,
            BrokerageHouseId = brokerage.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = GenerateJwt(user, brokerageRole.Name);
        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = brokerageRole.Name
        };
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActive);

        if (user == null) return null;
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)) return null;

        var token = GenerateJwt(user, user.Role!.Name);
        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.Name
        };
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _db.Users
            .Include(u => u.Role)
            .Include(u => u.BrokerageHouse)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
    }

    private string GenerateJwt(User user, string roleName)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, roleName),              // 🔑 Role claim
            new Claim("BrokerageHouseId",
                user.BrokerageHouseId.ToString())      // 🔑 Custom claim
        };

        var token = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"],
            audience: _config["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}