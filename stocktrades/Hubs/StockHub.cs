using Microsoft.AspNetCore.SignalR;
using trading_model;

namespace stocktrades.Hubs
{
    public class StockHub : Hub
    {
        public async Task SendOrder(Order order)
        {

        }
    }
}
