using Orleans.Concurrency;

namespace StockApp.Contracts;

public interface IStockGrain : IGrainWithGuidKey
{
    Task SetStock(Stock stock);
    [ReadOnly]
    Task<Stock> GetStock(int sleep);
    Task<(bool, int)> ChangeStock(int quantity, int sleep = 4000);
}
