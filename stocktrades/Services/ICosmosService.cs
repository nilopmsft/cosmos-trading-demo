using trading_model;

namespace stocktrades.Services
{
    public interface ICosmosService
    {
        public Task<IEnumerable<string>> RetrieveUnusedUserIdsAsync();

        public Task ClaimUserAsync(Models.User user);

        public Task<List<CustomerPortfolio>> GetUserPortfolios(string username);
    }
}