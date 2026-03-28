using System.Text.Json;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Models.Admin;

namespace BdStockOMS.API.Services;

public class AdminFixService : IAdminFixService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AdminFixService> _logger;
    private static bool _connected = false;
    private static DateTime? _connectedAt = null;
    private static readonly List<FixMessageLog> _messages = new();
    private static int _msgSeq = 1;
    private const string CONFIG_KEY = "fix_engine_config";

    public AdminFixService(AppDbContext db, ILogger<AdminFixService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task<FIXConfigDto?> GetConfigAsync()
    {
        var json = await KV.Get(_db, CONFIG_KEY);
        if (string.IsNullOrEmpty(json)) return GetDefaultConfig();
        try { return JsonSerializer.Deserialize<FIXConfigDto>(json); }
        catch { return GetDefaultConfig(); }
    }

    public async Task UpdateConfigAsync(FIXConfigDto dto)
    {
        var json = JsonSerializer.Serialize(dto);
        await KV.Set(_db, CONFIG_KEY, json, "fix");
        _logger.LogInformation("FIX config updated");
        AddMessage("ADMIN", "Config updated", "info");
    }

    public Task<object> GetStatusAsync()
    {
        var uptime = _connectedAt.HasValue
            ? (int)(DateTime.UtcNow - _connectedAt.Value).TotalSeconds
            : 0;
        return Task.FromResult<object>(new
        {
            connected        = _connected,
            connectedAt      = _connectedAt,
            uptimeSeconds    = uptime,
            messagesSent     = _messages.Count(m => m.Direction == "OUT"),
            messagesReceived = _messages.Count(m => m.Direction == "IN"),
            lastHeartbeat    = _connected ? DateTime.UtcNow.AddSeconds(-3) : (DateTime?)null,
            sessionStatus    = _connected ? "ACTIVE" : "DISCONNECTED",
            recentMessages   = _messages.TakeLast(50).Reverse().ToList(),
        });
    }

    public Task ConnectAsync()
    {
        _connected   = true;
        _connectedAt = DateTime.UtcNow;
        AddMessage("IN",  "Logon accepted - session established", "logon");
        AddMessage("OUT", "Heartbeat sent (MsgType=0)", "heartbeat");
        _logger.LogInformation("FIX engine connected");
        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        AddMessage("OUT", "Logout sent (MsgType=5)", "logout");
        _connected   = false;
        _connectedAt = null;
        _logger.LogInformation("FIX engine disconnected");
        return Task.CompletedTask;
    }

    private static void AddMessage(string direction, string body, string msgType)
    {
        _messages.Add(new FixMessageLog
        {
            SeqNum    = _msgSeq++,
            Direction = direction,
            MsgType   = msgType,
            Body      = body,
            Timestamp = DateTime.UtcNow,
        });
        if (_messages.Count > 500)
            _messages.RemoveRange(0, _messages.Count - 500);
    }

    private static FIXConfigDto GetDefaultConfig() => new(
        Enabled: false,
        SenderCompId: "BDSTOCKOMS",
        TargetCompId: "DSE",
        Host: "fix.dse.com.bd",
        Port: 9876,
        HeartbeatIntervalSec: 30,
        ReconnectIntervalSec: 10,
        LogMessages: true,
        UseSSL: true,
        FixVersion: "FIX.4.4",
        ResetOnLogon: true,
        ResetOnLogout: true,
        MaxReconnectAttempts: 5,
        MessageQueueSize: 1000,
        SendingTimeToleranceSec: 120
    );
}

public class FixMessageLog
{
    public int      SeqNum    { get; set; }
    public string   Direction { get; set; } = "";
    public string   MsgType   { get; set; } = "";
    public string   Body      { get; set; } = "";
    public DateTime Timestamp { get; set; }
}