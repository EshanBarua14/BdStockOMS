using BdStockOMS.API.Models;

namespace BdStockOMS.API.Repositories.Interfaces;

// Extends the generic IRepository<User>
// Adds User-specific operations on top
public interface IUserRepository : IRepository<User>
// : IRepository<User> means this interface
// INHERITS all methods from IRepository
// PLUS adds its own user-specific methods
{
    // Find user by email — needed for login
    Task<User?> GetByEmailAsync(string email);

    // Get all users belonging to a brokerage firm
    Task<IEnumerable<User>> GetByBrokerageHouseAsync(int brokerageHouseId);

    // Get all users with a specific role
    Task<IEnumerable<User>> GetByRoleAsync(int roleId);

    // Get all investors assigned to a specific trader
    Task<IEnumerable<User>> GetInvestorsByTraderAsync(int traderId);

    // Check if email already exists (for registration)
    Task<bool> EmailExistsAsync(string email);
}