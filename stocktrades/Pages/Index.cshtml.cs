using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using stocktrades.Services;
using System.Text;

namespace stocktrades.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        private readonly ICosmosService _cosmosService;

        public Models.User? user { get; set; }

        public IndexModel(ILogger<IndexModel> logger, ICosmosService cosmosService)
        {
            _logger = logger;
            _cosmosService = cosmosService;
        }

        public async Task OnGetAsync()
        {
            bool userid_insession = HttpContext.Session.TryGetValue("userId", out byte[]? userId);
            if (!userid_insession)
            {
                IEnumerable<string> available_users = await _cosmosService.RetrieveUnusedUserIdsAsync();

                string user_id = available_users.First();

                HttpContext.Session.SetString("userId", user_id);

                this.user = new Models.User(user_id, _cosmosService);
                this.user.sessionId = HttpContext.Session.Id;

                await _cosmosService.ClaimUserAsync(user);
            } else
            {
                string user_id = (userId == null ? string.Empty : Encoding.UTF8.GetString(userId));
                this.user = new Models.User(user_id, _cosmosService);
            }


        }
    }
}