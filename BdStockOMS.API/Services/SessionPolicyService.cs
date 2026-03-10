using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public interface ISessionPolicyService
{
    Task<SessionPolicy> GetPolicyAsync(string roleName);
    Task EnforceSessionLimitAsync(int userId, string roleName, string currentSessionToken);
    Task<bool> IsSessionActiveAsync(int userId, string sessionToken);
    Task TouchSessionAsync(int userId, string sessionToken);
    Task RevokeAllSessionsAsync(int userId, string exceptSessionToken = "");
    Task<UserSession> CreateSessionAsync(int userId, string ipAddress, string? userAgent, string roleName);
    Task RevokeSessionAsync(string sessionToken);
    Task PurgeExpiredSessionsAsync();
}

public class SessionPolicyService : ISessionPolicyService
{
    private readonly AppDbContext _db;
    private readonly ISystemSettingService _settings;

    private static readonly Dictionary<string, SessionPolicy> _defaults = new()
    {
        ["SuperAdmin"]     = new SessionPolicy { RoleName = "SuperAdmin",     MaxConcurrentSessions = 1, InactivityTimeoutMinutes = 20, MfaRequired = true,  SingleSessionOnly = true  },
        ["Admin"]          = new SessionPolicy { RoleName = "Admin",          MaxConcurrentSessions = 1, InactivityTimeoutMinutes = 30, MfaRequired = true,  SingleSessionOnly = true  },
        ["Compliance"]     = new SessionPolicy { RoleName = "Compliance",     MaxConcurrentSessions = 2, InactivityTimeoutMinutes = 30, MfaRequired = false, SingleSessionOnly = false },
        ["CCD"]            = new SessionPolicy { RoleName = "CCD",            MaxConcurrentSessions = 2, InactivityTimeoutMinutes = 30, MfaRequired = false, SingleSessionOnly = false },
        ["Trader"]         = new SessionPolicy { RoleName = "Trader",         MaxConcurrentSessions = 3, InactivityTimeoutMinutes = 60, MfaRequired = false, SingleSessionOnly = false },
        ["Investor"]       = new SessionPolicy { RoleName = "Investor",       MaxConcurrentSessions = 5, InactivityTimeoutMinutes = 60, MfaRequired = false, SingleSessionOnly = false },
        ["BrokerageHouse"] = new SessionPolicy { RoleName = "BrokerageHouse", MaxConcurrentSessions = 3, InactivityTimeoutMinutes = 30, MfaRequired = false, SingleSessionOnly = false },
    };

    public SessionPolicyService(AppDbContext db, ISystemSettingService settings)
    {
        _db      = db;
        _settings = settings;
    }

    public async Task<SessionPolicy> GetPolicyAsync(string roleName)
    {
        var policy = _defaults.TryGetValue(roleName, out var def)
            ? new SessionPolicy
            {
                RoleName                 = def.RoleName,
                MaxConcurrentSessions    = def.MaxConcurrentSessions,
                InactivityTimeoutMinutes = def.InactivityTimeoutMinutes,
                MfaRequired              = def.MfaRequired,
                SingleSessionOnly        = def.SingleSessionOnly
            }
            : new SessionPolicy { RoleName = roleName, MaxConcurrentSessions = 1, InactivityTimeoutMinutes = 30 };

        // Override with runtime SystemSettings if present
        var maxResult     = await _settings.GetByKeyAsync($"Session:{roleName}:MaxConcurrentSessions");
        var timeoutResult = await _settings.GetByKeyAsync($"Session:{roleName}:InactivityTimeoutMinutes");
        var mfaResult     = await _settings.GetByKeyAsync($"Session:{roleName}:MfaRequired");
        var singleResult  = await _settings.GetByKeyAsync($"Session:{roleName}:SingleSessionOnly");

        if (maxResult.IsSuccess     && int.TryParse(maxResult.Value!.Value,     out int max))     policy.MaxConcurrentSessions    = max;
        if (timeoutResult.IsSuccess && int.TryParse(timeoutResult.Value!.Value, out int timeout)) policy.InactivityTimeoutMinutes = timeout;
        if (mfaResult.IsSuccess     && bool.TryParse(mfaResult.Value!.Value,    out bool mfa))    policy.MfaRequired              = mfa;
        if (singleResult.IsSuccess  && bool.TryParse(singleResult.Value!.Value, out bool single)) policy.SingleSessionOnly         = single;

        return policy;
    }

    public async Task EnforceSessionLimitAsync(int userId, string roleName, string currentSessionToken)
    {
        var policy = await GetPolicyAsync(roleName);

        if (policy.SingleSessionOnly)
        {
            await RevokeAllSessionsAsync(userId, currentSessionToken);
            return;
        }

        if (policy.MaxConcurrentSessions <= 0) return;

        var activeSessions = await _db.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked
                     && s.ExpiresAt > DateTime.UtcNow
                     && s.SessionToken != currentSessionToken)
            .OrderBy(s => s.LastSeenAt)
            .ToListAsync();

        var overLimit = activeSessions.Count - (policy.MaxConcurrentSessions - 1);
        if (overLimit > 0)
        {
            foreach (var session in activeSessions.Take(overLimit))
                session.IsRevoked = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> IsSessionActiveAsync(int userId, string sessionToken)
    {
        var session = await _db.UserSessions
            .FirstOrDefaultAsync(s => s.UserId == userId
                                   && s.SessionToken == sessionToken
                                   && !s.IsRevoked);
        if (session == null) return false;
        if (session.ExpiresAt < DateTime.UtcNow) return false;

        var user = await _db.Users.Include(u => u.Role)
                                  .FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Role != null)
        {
            var policy = await GetPolicyAsync(user.Role.Name);
            if (policy.InactivityTimeoutMinutes > 0)
            {
                var inactiveSince = DateTime.UtcNow - session.LastSeenAt;
                if (inactiveSince.TotalMinutes > policy.InactivityTimeoutMinutes)
                {
                    session.IsRevoked = true;
                    await _db.SaveChangesAsync();
                    return false;
                }
            }
        }

        return true;
    }

    public async Task TouchSessionAsync(int userId, string sessionToken)
    {
        var session = await _db.UserSessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SessionToken == sessionToken);
        if (session != null)
        {
            session.LastSeenAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task RevokeAllSessionsAsync(int userId, string exceptSessionToken = "")
    {
        var sessions = await _db.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked
                     && s.SessionToken != exceptSessionToken)
            .ToListAsync();
        foreach (var s in sessions)
            s.IsRevoked = true;
        await _db.SaveChangesAsync();
    }

    public async Task<UserSession> CreateSessionAsync(int userId, string ipAddress,
                                                       string? userAgent, string roleName)
    {
        var policy  = await GetPolicyAsync(roleName);
        var timeout = policy.InactivityTimeoutMinutes > 0
            ? policy.InactivityTimeoutMinutes
            : 60 * 24;

        var session = new UserSession
        {
            UserId       = userId,
            SessionToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            IpAddress    = ipAddress,
            UserAgent    = userAgent,
            CreatedAt    = DateTime.UtcNow,
            LastSeenAt   = DateTime.UtcNow,
            ExpiresAt    = DateTime.UtcNow.AddMinutes(timeout),
            IsRevoked    = false
        };
        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }

    public async Task RevokeSessionAsync(string sessionToken)
    {
        var session = await _db.UserSessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);
        if (session != null)
        {
            session.IsRevoked = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task PurgeExpiredSessionsAsync()
    {
        var expired = await _db.UserSessions
            .Where(s => s.ExpiresAt < DateTime.UtcNow || s.IsRevoked)
            .ToListAsync();
        _db.UserSessions.RemoveRange(expired);
        await _db.SaveChangesAsync();
    }
}
