using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using stocktrades.Services;
using System.Text;
using trading_model;

namespace stocktrades.Pages
{
    public class StockSummaryModel : PageModel
    {
        private readonly ICosmosService _cosmosService;
        public Models.User? user { get; set; }

        public StockSummaryModel(ICosmosService cosmosService)
        {
            _cosmosService = cosmosService;
        }
        public async Task OnGet(string symbol)
        {
            bool userid_insession = HttpContext.Session.TryGetValue("userId", out byte[]? userId);
            if (!userid_insession)
            {
                IEnumerable<string> available_users = await _cosmosService.RetrieveUnusedUserIdsAsync();

                string user_id = available_users.First();

                HttpContext.Session.SetString("userId", user_id);

                this.user = new Models.User(user_id, _cosmosService);
                this.user.sessionId = HttpContext.Session.Id;
            }
            else
            {
                string user_id = (userId == null ? string.Empty : Encoding.UTF8.GetString(userId));
                this.user = new Models.User(user_id, _cosmosService);
            }

            StockPrice? stock_nullable = await _cosmosService.GetStockPriceCurrent(symbol);
            if(stock_nullable != null)
            {
                ViewData["stock"] = stock_nullable;
            }
        }
    }
}
