using trading_model;

namespace stocktrades.Services
{
    public interface ICosmosService
    {
        public Task<IEnumerable<string>> RetrieveUnusedUserIdsAsync();

        public Task<List<CustomerPortfolio>> GetUserPortfolios(string username);

        public Task<List<StockPrice>> GetStockPriceHistory(string symbol);

        public Task<StockPriceSummary> GetStockPriceSummary();

        public Task<StockPrice?> GetStockPriceCurrent(string symbol);
    }
}