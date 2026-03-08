using System.Linq.Expressions;
using BdStockOMS.API.Data;
using BdStockOMS.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Repositories;

public class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _db;
    protected readonly DbSet<T> _set;

    public BaseRepository(AppDbContext db)
    {
        _db  = db;
        _set = db.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id) =>
        await _set.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync() =>
        await _set.ToListAsync();

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
        await _set.Where(predicate).ToListAsync();

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate) =>
        await _set.FirstOrDefaultAsync(predicate);

    public async Task AddAsync(T entity) =>
        await _set.AddAsync(entity);

    public void Update(T entity) =>
        _set.Update(entity);

    public void Remove(T entity) =>
        _set.Remove(entity);

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
