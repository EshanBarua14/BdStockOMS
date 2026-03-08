using BdStockOMS.API.Data;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.BackgroundServices;

public class AccountUnlockService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AccountUnlockService> _logger;

    public AccountUnlockService(IServiceScopeFactory scopeFactory,
                                 ILogger<AccountUnlockService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var lockedUsers = await db.Users
                    .Where(u => u.IsLocked &&
                                u.LockoutUntil.HasValue &&
                                u.LockoutUntil.Value <= DateTime.UtcNow)
                    .ToListAsync(stoppingToken);

                if (lockedUsers.Any())
                {
                    foreach (var user in lockedUsers)
                    {
                        user.IsLocked          = false;
                        user.FailedLoginCount  = 0;
                        user.LockoutUntil      = null;
                    }
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Auto-unlocked {Count} user(s).", lockedUsers.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AccountUnlockService error.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
