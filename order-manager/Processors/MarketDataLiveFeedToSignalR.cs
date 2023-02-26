using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using trading_model;

namespace order_executor.Processors
{
    public static class MarketDataLiveFeedToSignalR
    {
        [FunctionName("MarketDataLiveFeedToSignalR")]
        public static async Task Run([CosmosDBTrigger(
                databaseName: "trading",
                containerName: "marketdata",
                Connection = "CosmosDBConnection",
                LeaseContainerName = "leases",
                LeaseContainerPrefix = "marketdata-livefeed-",
                FeedPollDelay = 5000,
                MaxItemsPerInvocation = 100,
                CreateLeaseContainerIfNotExists = true)]IReadOnlyList<StockPrice> input,
            [SignalR(HubName = "stockHub", ConnectionStringSetting = "ordersSignalRHub")] IAsyncCollector<SignalRMessage> outputRealtimeOrderMessages,
            ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                //Process received events
                await Parallel.ForEachAsync(input, async (eventData, token) =>
                {
                    try
                    {
                        await outputRealtimeOrderMessages.AddAsync(
                            new SignalRMessage
                            {
                                Target = "marketData",
                                Arguments = new[] { eventData }
                            }
                        );
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex.Message, ex);
                    }
                });
            }
        }
    }
}
