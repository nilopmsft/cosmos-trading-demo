using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace trading_model
{
    public class AvgPrices
    {
        public DateTime timestamp { get; set; }
        public decimal avgAskPrice { get; set; }
        public decimal avgBidPrice { get; set; }
        public decimal? openPrice { get; set; }
        public decimal? closePrice { get; set; }
    }
    public class StockPriceSummary
    {
        public StockPriceSummary()
        {
            summary = new Dictionary<string, AvgPrices>();
        }
        public string id = "stockprice_summary";
        // String in this situation is the stock symbol
        public Dictionary<string, AvgPrices> summary { get; set; }

    }
}
