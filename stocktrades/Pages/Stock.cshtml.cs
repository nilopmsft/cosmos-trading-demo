using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace stocktrades.Pages
{
    public class StockModel : PageModel
    {
        public void OnGet(string symbol)
        {
            ViewData["symbol"] = symbol;
        }
    }
}
