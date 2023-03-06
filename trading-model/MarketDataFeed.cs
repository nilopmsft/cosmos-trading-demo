namespace trading_model
{
    /// <summary>
    /// Market data feed entity
    /// </summary>
    public class MarketDataFeed
    {
        public string id { get; set; }
        public string symbol { get; set; }
        public DateTime timestamp { get; set; }
        public decimal avgAskPrice { get; set; }
        public decimal avgBidPrice { get; set; }
        public decimal? openPrice { get; set; }
        public decimal? closePrice { get; set; }
    }
}