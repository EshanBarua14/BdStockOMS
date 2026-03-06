namespace BdStockOMS.API.Repositories.Interfaces;

// T = any model type (User, Stock, Order etc)
// This interface defines operations ALL
// repositories must support
// It's like a CONTRACT — any class that
// implements this MUST have these methods

public interface IRepository<T> where T : class
    // where T : class = T must be a class (not int, bool etc)
{
    // Get one record by its Id
    // Task = async operation (won't freeze the app)
    Task<T?> GetByIdAsync(int id);
    // T? = might return null if not found

    // Get all records
    Task<IEnumerable<T>> GetAllAsync();
    // IEnumerable = a list we can loop through

    // Add a new record
    Task<T> AddAsync(T entity);

    // Update existing record
    Task<T> UpdateAsync(T entity);

    // Delete a record by Id
    Task<bool> DeleteAsync(int id);

    // Check if a record exists
    Task<bool> ExistsAsync(int id);
}