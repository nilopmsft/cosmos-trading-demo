using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace trading_model
{
    /// <summary>
    /// Stock price entity
    /// </summary>
    public class StockPrice
    {
        public string id { get; set; }
        public string symbol { get; set; }
        [DisplayName("Last Updated")]
        public DateTime timestamp { get; set; }
        [DisplayName("Last Ask Price")]
        [DisplayFormat(DataFormatString = "${0:#.00}")]
        public decimal lastAskPrice { get; set; }
        [DisplayName("Last Bid Price")]
        [DisplayFormat(DataFormatString = "${0:#.00}")]
        public decimal lastBidPrice { get; set; }
        [DisplayName("Average Ask Price")]
        [DisplayFormat(DataFormatString = "${0:#.00}")]
        public decimal avgAskPrice { get; set; }
        [DisplayName("Average Bid Price")]
        [DisplayFormat(DataFormatString = "${0:#.00}")]
        public decimal avgBidPrice { get; set; }
        [DisplayName("Minimum Ask Price")]
        [DisplayFormat(DataFormatString = "${0:#.00}")]
        public decimal minAskPrice { get; set; }
        [DisplayName("Minimum Bid Price")]
        [DisplayFormat(DataFormatString = "${0:#.00}")]
        public decimal minBidPrice { get; set; }
        [DisplayName("Maximum Ask Price")]
        [DisplayFormat(DataFormatString = "${0:#.00}")]
        public decimal maxAskPrice { get; set; }
        [DisplayName("Maximum Bid Price")]
        [DisplayFormat(DataFormatString = "${0:#.00}")]
        public decimal maxBidPrice { get; set; }
        [DisplayName("Open Price")]
        [DisplayFormat(DataFormatString = "${0:#.00}")]
        public decimal openPrice { get; set; }
        [DisplayName("Close Price")]
        [DisplayFormat(DataFormatString = "${0:#.00}")]
        public decimal closePrice { get; set; }
    }
}