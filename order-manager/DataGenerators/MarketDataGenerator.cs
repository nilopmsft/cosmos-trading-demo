using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using System.Linq;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using trading_model;

using Bogus;
using Bogus.Distributions.Gaussian;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using Microsoft.Azure.SignalR;
using System.Collections;

namespace order_executor.DataGenerators
{

    public class sym
    {
        // Just a holder so we can more easily generate symbols
        public string symbol { get; set; }
    }
    public class MarketDataGenerator
    {
        static Random random = new Random();
        static MarketDataGenerator()
        {
            //Instance CosmosClient
            cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"), new CosmosClientOptions() { AllowBulkExecution = true });
            container = cosmosClient.GetContainer("trading", "stockPriceSummary");
            marketdata_container = cosmosClient.GetContainer("trading", "marketdata");
        }

        static CosmosClient cosmosClient;
        static Container container;
        static Container marketdata_container;

        StockPriceSummary current_price;

        [FunctionName("MarketDataGenerator")]
        public async Task Run([TimerTrigger("*/5 * * * * *")]TimerInfo myTimer,
            [EventHub("marketdata", Connection = "ordersHubConnection")] IAsyncCollector<MarketDataFeed> outputMarketData, 
            ILogger log)
        { 
            current_price = await GetCurrentPrices();

            if (current_price.summary.Count == 0) 
            {
                log.LogInformation("No summary data found, creating totally new symbols");
                List<sym> symbols = new List<sym>();
                symbols.Add(new sym { symbol =  "MSFT" });
                var fake_symbols = new Faker<sym>()
                    .RuleFor(o => o.symbol, f => f.Random.String(7, 'A', 'Z').Substring(0, f.Random.Int(2, 6)));

                symbols.AddRange(fake_symbols.Generate(19));

                foreach(sym symbol_hold in symbols)
                {
                    log.LogInformation($"Creating starting point for {symbol_hold.symbol}");
                    bool large = (random.NextDouble() > 0.75);
                    int top = large ? 10000 : 100;
                    int bottom = large ? 1000 : 1;
                    string symbol = symbol_hold.symbol;
                    var fake_price = new Faker<MarketDataFeed>()
                        .RuleFor(o => o.id, $"{DateTime.UtcNow.AddDays(-7).Year}-{DateTime.UtcNow.AddDays(-7).Month}-{DateTime.UtcNow.AddDays(-7).Day}-{symbol}")
                        .RuleFor(o => o.timestamp, DateTime.UtcNow.AddDays(-7))
                        .RuleFor(o => o.symbol, symbol)
                        .RuleFor(o => o.avgBidPrice, f => f.Finance.Amount(bottom, top, 2))
                        .RuleFor(o => o.avgAskPrice, (f, o) => o.avgBidPrice + (o.avgBidPrice * f.Random.Decimal(0.005m, 0.15m)))
                        .RuleFor(o => o.openPrice, (f, o) => ((o.avgBidPrice + o.avgAskPrice) / 2))
                        .RuleFor(o => o.closePrice, (f, o) => ((o.avgBidPrice + o.avgAskPrice) / 2));

                    MarketDataFeed root = fake_price.Generate();

                    await outputMarketData.AddAsync(root);
                    log.LogInformation($"Adding next 6 days for for {symbol_hold.symbol}");
                    for (DateTime i = DateTime.UtcNow.AddDays(-6); (DateTime.UtcNow - i).TotalDays >= 0; i = i.AddDays(1))
                    {
                        bool jump = (random.NextDouble() >= 0.95);
                        var fake_price_next = new Faker<MarketDataFeed>()
                            .RuleFor(o => o.id, $"{i.Year}-{i.Month}-{i.Day}-{symbol}")
                            .RuleFor(o => o.timestamp, i)
                            .RuleFor(o => o.symbol, symbol)
                            .RuleFor(o => o.avgBidPrice, f => jump ? f.Random.GaussianDecimal((double)root.avgBidPrice, (double)(root.avgBidPrice * 0.18m)) : f.Random.GaussianDecimal((double)root.avgBidPrice, (double)(root.avgBidPrice * 0.05m)))
                            .RuleFor(o => o.avgAskPrice, (f, o) => o.avgBidPrice + (o.avgBidPrice * f.Random.Decimal(0.005m, 0.15m)))
                            .RuleFor(o => o.openPrice, f => jump ? f.Random.GaussianDecimal((double)root.avgBidPrice, (double)(root.avgBidPrice * 0.18m)) : f.Random.GaussianDecimal((double)root.avgBidPrice, (double)(root.avgBidPrice * 0.05m)))
                            .RuleFor(o => o.closePrice, f => jump ? f.Random.GaussianDecimal((double)root.avgBidPrice, (double)(root.avgBidPrice * 0.18m)) : f.Random.GaussianDecimal((double)root.avgBidPrice, (double)(root.avgBidPrice * 0.05m)));

                        await outputMarketData.AddAsync(fake_price_next.Generate());
                    }
                }
            } else
            {
                foreach(KeyValuePair<string, AvgPrices> stock in current_price.summary)
                {
                    bool jump = (random.NextDouble() >= 0.95);
                    TimeSpan time_since_update = DateTime.UtcNow - stock.Value.timestamp;
                    if (time_since_update.TotalDays >= 1)
                    {
                        log.LogInformation("No recent data found, checking last 7 days");
                        for(DateTime i = DateTime.UtcNow.AddDays(-7); (DateTime.UtcNow - i).TotalDays >= 0; i = i.AddDays(1))
                        {
                            log.LogInformation($"Checking for {stock.Key} on {i.ToString()}");
                            try
                            {
                                StockPrice stock_price = null;

                                string stock_date_id = String.Format("{0}-{1}-{2}-{3}", i.Year, i.Month, i.Day, stock.Key);

                                //Lookup (point read) customer portfolio
                                using (var response = await marketdata_container.ReadItemStreamAsync(stock_date_id, new PartitionKey(stock.Key)))
                                {
                                    if (response.StatusCode != HttpStatusCode.NotFound)
                                    {
                                        //Deserialize portfolio object if already exists for that customer/symbol
                                        JsonSerializer serializer = new JsonSerializer();
                                        using (StreamReader streamReader = new StreamReader(response.Content))
                                        using (var reader = new JsonTextReader(streamReader))
                                        {
                                            stock_price = serializer.Deserialize<StockPrice>(reader);
                                        }
                                    }
                                }

                                if (stock_price == null)
                                {
                                    log.LogInformation($"Couldn't find entry for {stock.Key} on {i}");
                                    var fake_price = new Faker<MarketDataFeed>()
                                        .RuleFor(o => o.id, $"{i.Year}-{i.Month}-{i.Day}-{stock.Key}")
                                        .RuleFor(o => o.timestamp, i)
                                        .RuleFor(o => o.symbol, stock.Key)
                                        .RuleFor(o => o.avgBidPrice, f => jump ? f.Random.GaussianDecimal((double)stock.Value.avgBidPrice, (double)(stock.Value.avgBidPrice * 0.18m)) : f.Random.GaussianDecimal((double)stock.Value.avgBidPrice, (double)(stock.Value.avgBidPrice * 0.05m)))
                                        .RuleFor(o => o.avgAskPrice, (f, o) => o.avgBidPrice + (o.avgBidPrice * f.Random.Decimal(0.005m, 0.15m)))
                                        .RuleFor(o => o.openPrice, f => jump ? f.Random.GaussianDecimal((double)stock.Value.avgBidPrice, (double)(stock.Value.avgBidPrice * 0.18m)) : f.Random.GaussianDecimal((double)stock.Value.avgBidPrice, (double)(stock.Value.avgBidPrice * 0.05m)))
                                        .RuleFor(o => o.closePrice, f => jump ? f.Random.GaussianDecimal((double)stock.Value.avgBidPrice, (double)(stock.Value.avgBidPrice * 0.18m)) : f.Random.GaussianDecimal((double)stock.Value.avgBidPrice, (double)(stock.Value.avgBidPrice * 0.05m)));

                                    await outputMarketData.AddAsync(fake_price.Generate());
                                }
                            }
                            catch (Exception ex)
                            {
                                log.LogError(ex.Message, ex);
                            }
                        }
                    } else
                    {
                        log.LogInformation($"Recent data found for {stock.Key}, creating new normal entry");
                        var fake_price = new Faker<MarketDataFeed>()
                            .RuleFor(o => o.id, $"{DateTime.UtcNow.Year}-{DateTime.UtcNow.Month}-{DateTime.UtcNow.Day}-{stock.Key}")
                            .RuleFor(o => o.timestamp, DateTime.UtcNow)
                            .RuleFor(o => o.symbol, stock.Key)
                            .RuleFor(o => o.avgBidPrice, f => jump ? f.Random.GaussianDecimal((double)stock.Value.avgBidPrice, (double)(stock.Value.avgBidPrice * 0.18m)) : f.Random.GaussianDecimal((double)stock.Value.avgBidPrice, (double)(stock.Value.avgBidPrice * 0.05m)))
                            .RuleFor(o => o.avgAskPrice, (f, o) => o.avgBidPrice + (o.avgBidPrice * f.Random.Decimal(0.005m, 0.15m)))
                            .RuleFor(o => o.openPrice, stock.Value.openPrice)
                            .RuleFor(o => o.closePrice, (f, o) => (o.avgAskPrice + o.avgBidPrice) / 2);

                        await outputMarketData.AddAsync(fake_price.Generate());
                    }

                }
            }


        }

        public async Task<StockPriceSummary> GetCurrentPrices()
        {
            StockPriceSummary stocks = new();

            //Build hierarchical partition key (currently preview feature)
            PartitionKey partitionKey = new PartitionKeyBuilder()
                 .Add("stockprice_summary")
                 .Build();

            //Lookup (point read) customer portfolio
            using (var response = await container.ReadItemStreamAsync("stockprice_summary", partitionKey))
            {
                if (response.StatusCode != HttpStatusCode.NotFound)
                {
                    //Deserialize portfolio object if already exists for that customer/symbol
                    JsonSerializer serializer = new JsonSerializer();
                    using (StreamReader streamReader = new StreamReader(response.Content))
                    using (var reader = new JsonTextReader(streamReader))
                    {
                        stocks = serializer.Deserialize<StockPriceSummary>(reader);
                    }
                }
            }

            return stocks;
        }
    }
}
