using Microsoft.AspNetCore.Mvc;
using stocktrades.Services;
using trading_model;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace stocktrades.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockPricesController : ControllerBase
    {
        private readonly ICosmosService _cosmosService;

        public StockPricesController(ICosmosService cosmosService)
        {
            _cosmosService = cosmosService;
        }
        // GET: api/<StockPricesController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<StockPricesController>/5
        [HttpGet("{symbol}")]
        public Task<List<StockPrice>> Get(string symbol)
        {
            return _cosmosService.GetStockPriceHistory(symbol);
        }
    }
}
