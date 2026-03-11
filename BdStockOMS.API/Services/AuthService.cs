using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.Auth;
using BdStockOMS.API.Models;
using BdStockOMS.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BdStockOMS.API.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> RegisterBrokerageAsync(RegisterBrokerageDto dto);
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, string ipAddress);
    Task<Result<AuthResponseDto>> RefreshTokenAsync(string token, string ipAddress);
    Task<Result> LogoutAsync(string accessToken, string refreshToken, string ipAddress);
    Task<User?> GetUserByIdAsync(int userId);
    Task<List<BrokerageListItemDto>> GetActiveBrokeragesAsync();
    Task<AuthResponseDto?> RegisterInvestorAsync(RegisterInvestorDto dto);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IAuditService _audit;
    private readonly ITokenBlacklistService _blacklist;

    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes    = 30;

    public AuthService(AppDbContext db, IConfiguration config,
                       IRefreshTokenRepository refreshTokenRepo,
                       IAuditService audit,
                       ITokenBlacklistService blacklist)
    {
        _db               = db;
        _config           = config;
        _refreshTokenRepo = refreshTokenRepo;
        _audit            = audit;
        _blacklist        = blacklist;
    }

    public async Task<AuthResponseDto?> RegisterBrokerageAsync(RegisterBrokerageDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return null;

        var brokerageRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "BrokerageHouse");
        if (brokerageRole == null) return null;

        var brokerage = new BrokerageHouse
        {
            Name          = dto.FirmName,
            LicenseNumber = dto.LicenseNumber,
            Address       = dto.FirmAddress,
            Email         = dto.FirmEmail,
            Phone         = dto.FirmPhone,
            IsActive      = true,
            CreatedAt     = DateTime.UtcNow
        };
        _db.BrokerageHouses.Add(brokerage);
        await _db.SaveChangesAsync();

        var user = new User
        {
            FullName         = dto.FullName,
            Email            = dto.Email,
            PasswordHash     = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId           = brokerageRole.Id,
            BrokerageHouseId = brokerage.Id,
            IsActive         = true,
            CreatedAt        = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = GenerateJwt(user, brokerageRole.Name);
        return new AuthResponseDto
        {
            Token    = token,
            UserId   = user.Id,
            FullName = user.FullName,
            Email    = user.Email,
            Role     = brokerageRole.Name
        };
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, string ipAddress)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .Include(u => u.BrokerageHouse)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        // Unknown email
        if (user == null)
        {
            await RecordLoginHistoryAsync(null, dto.Email, ipAddress, false, "User not found");
            return Result<AuthResponseDto>.Failure("Invalid email or password.", "INVALID_CREDENTIALS");
        }

        // Account inactive
        if (!user.IsActive)
        {
            await RecordLoginHistoryAsync(user.Id, dto.Email, ipAddress, false, "Account inactive");
            return Result<AuthResponseDto>.Failure("Account is inactive.", "ACCOUNT_INACTIVE");
        }

        // Locked out
        if (user.IsLocked)
        {
            if (user.LockoutUntil.HasValue && user.LockoutUntil > DateTime.UtcNow)
            {
                await RecordLoginHistoryAsync(user.Id, dto.Email, ipAddress, false, "Account locked");
                return Result<AuthResponseDto>.Failure(
                    $"Account locked until {user.LockoutUntil:HH:mm} UTC.", "ACCOUNT_LOCKED");
            }
            // Auto-unlock if lockout expired
            user.IsLocked         = false;
            user.FailedLoginCount = 0;
            user.LockoutUntil     = null;
        }

        // Wrong password
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.IsLocked     = true;
                user.LockoutUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
            }
            await _db.SaveChangesAsync();
            await RecordLoginHistoryAsync(user.Id, dto.Email, ipAddress, false, "Invalid password");
            await _audit.LogAsync(user.Id, "LOGIN_FAILED", "User", user.Id, null, null, ipAddress);

            var remaining = MaxFailedAttempts - user.FailedLoginCount;
            return user.IsLocked
                ? Result<AuthResponseDto>.Failure("Account locked due to too many failed attempts.", "ACCOUNT_LOCKED")
                : Result<AuthResponseDto>.Failure($"Invalid password. {remaining} attempt(s) remaining.", "INVALID_CREDENTIALS");
        }

        // Success — reset failed count
        user.FailedLoginCount = 0;
        user.IsLocked         = false;
        user.LockoutUntil     = null;
        user.LastLoginAt      = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var accessToken  = GenerateJwt(user, user.Role!.Name);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, ipAddress);

        await RecordLoginHistoryAsync(user.Id, dto.Email, ipAddress, true, null);
        await _audit.LogAsync(user.Id, "LOGIN_SUCCESS", "User", user.Id, null, null, ipAddress);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            Token               = accessToken,
            RefreshToken        = refreshToken.Token,
            ExpiresAt           = DateTime.UtcNow.AddMinutes(15),
            UserId              = user.Id,
            FullName            = user.FullName,
            Email               = user.Email,
            Role                = user.Role!.Name,
            BrokerageHouseId    = user.BrokerageHouseId,
            BrokerageHouseName  = user.BrokerageHouse?.Name ?? string.Empty
        });
    }

    public async Task<Result<AuthResponseDto>> RefreshTokenAsync(string token, string ipAddress)
    {
        var existing = await _refreshTokenRepo.GetActiveTokenAsync(token);

        if (existing == null)
            return Result<AuthResponseDto>.Failure("Invalid or expired refresh token.", "INVALID_REFRESH_TOKEN");

        var user = existing.User;
        if (!user.IsActive)
            return Result<AuthResponseDto>.Failure("Account is inactive.", "ACCOUNT_INACTIVE");

        // Revoke old token
        existing.RevokedAt   = DateTime.UtcNow;
        existing.RevokedByIp = ipAddress;

        // Issue new pair
        var newRefreshToken      = await CreateRefreshTokenAsync(user.Id, ipAddress);
        existing.ReplacedByToken = newRefreshToken.Token;
        await _db.SaveChangesAsync();

        var role        = await _db.Roles.FindAsync(user.RoleId);
        var accessToken = GenerateJwt(user, role!.Name);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            Token        = accessToken,
            RefreshToken = newRefreshToken.Token,
            UserId       = user.Id,
            FullName     = user.FullName,
            Email        = user.Email,
            Role         = role.Name
        });
    }

    public async Task<Result> LogoutAsync(string accessToken, string refreshToken, string ipAddress)
    {
        try
        {
            // Blacklist the JWT by JTI
            var handler = new JwtSecurityTokenHandler();
            var jwt     = handler.ReadJwtToken(accessToken);
            var jti     = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            var userIdStr = jwt.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            if (!string.IsNullOrEmpty(jti))
            {
                var remaining = jwt.ValidTo - DateTime.UtcNow;
                if (remaining > TimeSpan.Zero)
                    await _blacklist.BlacklistTokenAsync(jti, remaining);
            }

            // Revoke refresh token
            var existing = await _refreshTokenRepo.GetActiveTokenAsync(refreshToken);
            if (existing != null)
            {
                existing.RevokedAt   = DateTime.UtcNow;
                existing.RevokedByIp = ipAddress;
                await _db.SaveChangesAsync();
            }

            if (int.TryParse(userIdStr, out int userId))
                await _audit.LogAsync(userId, "LOGOUT", "User", userId, null, null, ipAddress);

            return Result.Success();
        }
        catch
        {
            return Result.Failure("Logout failed.", "LOGOUT_ERROR");
        }
    }

    public async Task<User?> GetUserByIdAsync(int userId) =>
        await _db.Users
                 .Include(u => u.Role)
                 .Include(u => u.BrokerageHouse)
                 .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

    // ── PRIVATE HELPERS ────────────────────────────────────────

    private string GenerateJwt(User user, string roleName)
    {
        var key   = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier,   user.Id.ToString()),
            new Claim(ClaimTypes.Email,            user.Email),
            new Claim(ClaimTypes.Name,             user.FullName),
            new Claim(ClaimTypes.Role,             roleName),
            new Claim("userId",                    user.Id.ToString()),
            new Claim("brokerageHouseId",          user.BrokerageHouseId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             _config["JwtSettings:Issuer"],
            audience:           _config["JwtSettings:Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(int userId, string ipAddress)
    {
        var token = new RefreshToken
        {
            UserId      = userId,
            Token       = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            CreatedByIp = ipAddress,
            CreatedAt   = DateTime.UtcNow,
            ExpiresAt   = DateTime.UtcNow.AddDays(7)
        };
        await _refreshTokenRepo.AddAsync(token);
        await _refreshTokenRepo.SaveChangesAsync();
        return token;
    }

    private async Task RecordLoginHistoryAsync(int? userId, string email,
                                               string ip, bool success, string? reason)
    {
        _db.LoginHistories.Add(new LoginHistory
        {
            UserId        = userId,
            Email         = email,
            IpAddress     = ip,
            IsSuccess     = success,
            FailureReason = reason,
            AttemptedAt   = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
    public async Task<List<BrokerageListItemDto>> GetActiveBrokeragesAsync()
    {
        return await _db.BrokerageHouses
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new BrokerageListItemDto
            {
                Id    = b.Id,
                Name  = b.Name,
                Email = b.Email,
                Phone = b.Phone,
            })
            .ToListAsync();
    }

    public async Task<AuthResponseDto?> RegisterInvestorAsync(RegisterInvestorDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return null;

        var investorRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Investor");
        if (investorRole == null) return null;

        var brokerage = await _db.BrokerageHouses
            .FirstOrDefaultAsync(b => b.Id == dto.BrokerageHouseId && b.IsActive);
        if (brokerage == null) return null;

        var user = new User
        {
            FullName         = dto.FullName,
            Email            = dto.Email,
            Phone            = dto.Phone,
            PasswordHash     = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId           = investorRole.Id,
            BrokerageHouseId = dto.BrokerageHouseId,
            BONumber         = dto.BONumber,
            IsActive         = true,
            CreatedAt        = DateTime.UtcNow,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token        = GenerateJwt(user, investorRole.Name);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, "registration");
        await _audit.LogAsync(user.Id, "INVESTOR_REGISTERED", "User", user.Id, null, null, "registration");

        return new AuthResponseDto
        {
            Token               = token,
            RefreshToken        = refreshToken.Token,
            ExpiresAt           = DateTime.UtcNow.AddMinutes(15),
            UserId              = user.Id,
            FullName            = user.FullName,
            Email               = user.Email,
            Role                = investorRole.Name,
            BrokerageHouseId    = brokerage.Id,
            BrokerageHouseName  = brokerage.Name,
        };
    }

}