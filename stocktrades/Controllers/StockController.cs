using Microsoft.AspNetCore.Mvc;
using stocktrades.Services;
using System.Text;
using trading_model;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace stocktrades.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockController : ControllerBase
    {
        private readonly ICosmosService _cosmosService;
        private static HttpClient _httpClient = new HttpClient();
        private IConfiguration _configuration;

        private string orderUrl = "";

        public StockController(ICosmosService cosmosService, IConfiguration configuration)
        {
            _cosmosService = cosmosService;
            _configuration = configuration;
            orderUrl = _configuration["CreateOrderApiUrl"];
        }

        // GET api/<StockController>/5
        [HttpGet("{symbol}")]
        public Task<List<StockPrice>> Get(string symbol)
        {
            return _cosmosService.GetStockPriceHistory(symbol);
        }

        // POST api/<StockController>/buy/{symbol}
        [HttpPost("{symbol}")]
        [ActionName("buy")]
        public async Task Buy(string symbol, [FromBody] int quantity)
        {
            StockPrice? stock = await _cosmosService.GetStockPriceCurrent(symbol);
            string user_id = "";
            if (stock == null)
            {
                return;
            }
            bool userid_insession = HttpContext.Session.TryGetValue("userId", out byte[]? userId);
            if (!userid_insession)
            {
                return;
            }
            else
            {
                user_id = (userId == null ? string.Empty : Encoding.UTF8.GetString(userId));
            }

            Order new_order = new Order
            {
                customerId = user_id,
                quantity = quantity,
                symbol = symbol,
                price = stock.avgBidPrice,
                action = "buy"
            };

            var response = await _httpClient.PostAsJsonAsync<Order>(orderUrl, new_order);
        }

        // POST api/<StockController>/sell
        [HttpPost("{symbol}")]
        [ActionName("sell")]
        public async Task Sell(string symbol, [FromBody] int quantity)
        {
            StockPrice? stock = await _cosmosService.GetStockPriceCurrent(symbol);
            string user_id = "";
            if (stock == null)
            {
                return;
            }
            bool userid_insession = HttpContext.Session.TryGetValue("userId", out byte[]? userId);
            if (!userid_insession)
            {
                return;
            }
            else
            {
                user_id = (userId == null ? string.Empty : Encoding.UTF8.GetString(userId));
            }

            Order new_order = new Order
            {
                customerId = user_id,
                quantity = quantity,
                symbol = symbol,
                price = stock.avgAskPrice,
                action = "sell"
            };

            var response = await _httpClient.PostAsJsonAsync<Order>(orderUrl, new_order);
        }
    }
}
