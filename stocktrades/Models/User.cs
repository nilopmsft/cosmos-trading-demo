using Newtonsoft.Json;
using stocktrades.Services;

namespace stocktrades.Models
{
    public class User
    {
        private readonly ICosmosService? _cosmosService;

        public User()
        {

        }
        public User(string userId)
        {
            this.userId = userId;
            portfolio = new List<trading_model.CustomerPortfolio>();
        }

        public User(string userId, ICosmosService cosmosService) { 
            _cosmosService = cosmosService;
            this.userId = userId;
            portfolio = _cosmosService.GetUserPortfolios(userId).Result;
        }
        public string id { get { return this.userId.ToString(); } }
        public string userId { get; set; }
        public string? sessionId { get; set; }

        [JsonIgnore]
        public List<trading_model.CustomerPortfolio> portfolio { get; set; }
    }
}
