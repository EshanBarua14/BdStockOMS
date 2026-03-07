// Services/UserService.cs
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.User;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public interface IUserService
{
    Task<(UserResponseDto? User, string? Error)> CreateUserAsync(
        CreateUserDto dto, int creatorBrokerageHouseId);
    Task<List<UserResponseDto>> GetUsersByBrokerageAsync(int brokerageHouseId);
    Task<UserResponseDto?> GetUserByIdAsync(int userId);
    Task<bool> DeactivateUserAsync(int userId, int requestingBrokerageHouseId);
}

public class UserService : IUserService
{
    private readonly AppDbContext _db;

    // Roles that a BrokerageHouse owner is allowed to create
    private static readonly HashSet<string> AllowedRolesToCreate =
        new() { "Admin", "Trader", "Investor" };

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(UserResponseDto? User, string? Error)> CreateUserAsync(
        CreateUserDto dto, int creatorBrokerageHouseId)
    {
        // Validate role
        if (!AllowedRolesToCreate.Contains(dto.Role))
            return (null, $"Role '{dto.Role}' is not allowed. " +
                         $"Allowed roles: Admin, Trader, Investor.");

        // Check duplicate email
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return (null, "A user with this email already exists.");

        // Get the role entity
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == dto.Role);
        if (role == null)
            return (null, $"Role '{dto.Role}' not found in database.");

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId = role.Id,
            BrokerageHouseId = creatorBrokerageHouseId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(user).Reference(u => u.BrokerageHouse).LoadAsync();

        return (MapToDto(user, role.Name), null);
    }

    public async Task<List<UserResponseDto>> GetUsersByBrokerageAsync(int brokerageHouseId)
    {
        return await _db.Users
            .Include(u => u.Role)
            .Include(u => u.BrokerageHouse)
            .Where(u => u.BrokerageHouseId == brokerageHouseId && u.IsActive)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role!.Name,
                BrokerageHouseName = u.BrokerageHouse!.Name,
                BrokerageHouseId = u.BrokerageHouseId,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(int userId)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .Include(u => u.BrokerageHouse)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (user == null) return null;
        return MapToDto(user, user.Role!.Name);
    }

    public async Task<bool> DeactivateUserAsync(
        int userId, int requestingBrokerageHouseId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;

        // BrokerageHouse can only deactivate their own users
        if (user.BrokerageHouseId != requestingBrokerageHouseId) return false;

        user.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    private static UserResponseDto MapToDto(User user, string roleName) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = roleName,
        BrokerageHouseName = user.BrokerageHouse?.Name,
        BrokerageHouseId = user.BrokerageHouseId,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}