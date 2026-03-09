namespace BdStockOMS.API.Exchange;

public interface IExchangeConnectorFactory
{
    IExchangeConnector GetConnector(string exchangeCode);
}

public class ExchangeConnectorFactory : IExchangeConnectorFactory
{
    private readonly IServiceProvider _sp;

    public ExchangeConnectorFactory(IServiceProvider sp) => _sp = sp;

    public IExchangeConnector GetConnector(string exchangeCode)
    {
        var connector = _sp.GetKeyedService<IExchangeConnector>(exchangeCode.ToUpper());
        if (connector is null)
            throw new InvalidOperationException(
                $"No exchange connector registered for key '{exchangeCode}'");
        return connector;
    }
}
