namespace BdStockOMS.API.Services;

public interface IExchangeScraperFactory
{
    IDseScraperService GetDseScraper();
    ICseScraperService GetCseScraper();
    bool IsDseMarketOpen();
    bool IsCseMarketOpen();
}

public class ExchangeScraperFactory : IExchangeScraperFactory
{
    private readonly IDseScraperService _dse;
    private readonly ICseScraperService _cse;

    public ExchangeScraperFactory(IDseScraperService dse, ICseScraperService cse)
    {
        _dse = dse;
        _cse = cse;
    }

    public IDseScraperService GetDseScraper() => _dse;
    public ICseScraperService GetCseScraper() => _cse;
    public bool IsDseMarketOpen() => _dse.IsMarketOpen();
    public bool IsCseMarketOpen() => _cse.IsMarketOpen();
}
