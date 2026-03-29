using BdStockOMS.API.FIX;

namespace BdStockOMS.API.FIX;

public interface IFIXConnectorFactory
{
    IFIXConnector GetConnector(string exchange = "DSE");
    bool IsRealFIXConfigured(string exchange = "DSE");
}

public class FIXConnectorFactory : IFIXConnectorFactory
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILoggerFactory _loggerFactory;

    public FIXConnectorFactory(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILoggerFactory loggerFactory)
    {
        _scopeFactory = scopeFactory;
        _config       = config;
        _loggerFactory = loggerFactory;
    }

    public bool IsRealFIXConfigured(string exchange = "DSE")
    {
        var section = _config.GetSection($"FIX:{exchange}");
        return section.Exists()
            && !string.IsNullOrEmpty(section["SenderCompID"])
            && !string.IsNullOrEmpty(section["TargetCompID"])
            && !string.IsNullOrEmpty(section["Host"]);
    }

    public IFIXConnector GetConnector(string exchange = "DSE")
    {
        if (IsRealFIXConfigured(exchange))
        {
            // Future: return new QuickFIXConnector(_config, exchange);
            // For now fall through to simulated
        }

        var logger = _loggerFactory.CreateLogger<SimulatedFIXConnector>();
        return new SimulatedFIXConnector(_scopeFactory, logger, exchange);
    }
}
