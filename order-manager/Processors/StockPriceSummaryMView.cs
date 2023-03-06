using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using trading_model;

namespace order_executor.Processors
{
    /// <summary>
    /// Cosmos DB triggered function (Change Feed) to generate customer portfolio view based on executed orders
    /// </summary>
    public static class StockPriceSummaryMView
    {
        static StockPriceSummaryMView()
        {
            //Instance CosmosClient
            cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"), new CosmosClientOptions() { AllowBulkExecution = true });
            container = cosmosClient.GetContainer("trading", "stockPriceSummary");
            marketdata_container = cosmosClient.GetContainer("trading", "marketdata");
        }

        static CosmosClient cosmosClient;
        static Container container;
        static Container marketdata_container;

        [FunctionName("StockPriceSummaryMView")]
        public static async Task Run([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer,
            ILogger log)
        {
            StockPriceSummary current_prices = await GetCurrentPrices();

            List<string> symbols = new();

            if(current_prices.summary.Count == 0)
            {
                string sql = "SELECT DISTINCT VALUE marketdata.symbol FROM marketdata";

                var query = new QueryDefinition(
                    query: sql
                );

                using FeedIterator<string> feed = marketdata_container.GetItemQueryIterator<string>(
                    queryDefinition: query
                );

                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (string item in response)
                    {
                        symbols.Add(item);
                    }
                }
            } else
            {
                symbols = current_prices.summary.Keys.ToList();
            }

            if (symbols.Count > 0)
            {
                StockPriceSummary stocks = new();
                foreach (string symbol in symbols)
                {
                    try
                    {
                        DateTime now = DateTime.UtcNow;
                        string date_key = string.Format("{0}-{1}-{2}-{3}", now.Year, now.Month, now.Day, symbol);
                        StockPrice current_stock_price = new();
                        using (var response = await marketdata_container.ReadItemStreamAsync(date_key, new PartitionKey(symbol)))
                        {
                            if (response.StatusCode != HttpStatusCode.NotFound)
                            {
                                //Deserialize portfolio object if already exists for that customer/symbol
                                JsonSerializer serializer = new JsonSerializer();
                                using (StreamReader streamReader = new StreamReader(response.Content))
                                using (var reader = new JsonTextReader(streamReader))
                                {
                                    current_stock_price = serializer.Deserialize<StockPrice>(reader);
                                }
                            }
                        }
                        stocks.summary.Add(
                            symbol,
                            new AvgPrices
                            {
                                timestamp = current_stock_price.timestamp,
                                avgAskPrice = current_stock_price.avgAskPrice,
                                avgBidPrice = current_stock_price.avgBidPrice,
                                openPrice = current_stock_price.openPrice,
                                closePrice = current_stock_price.closePrice
                            });
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex.Message, ex);
                    }
                }

                var resp = await container.UpsertItemAsync(stocks, new PartitionKey("stockprice_summary"));
            }
        }

        public static async Task<StockPriceSummary> GetCurrentPrices()
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