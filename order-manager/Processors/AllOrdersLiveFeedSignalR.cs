using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using trading_model;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System.Text;

namespace order_executor.Processors
{
    public static class AllOrdersLiveFeedSignalR
    {
        [FunctionName("AllOrdersLiveFeedSignalR")]
        public async static Task Run([EventHubTrigger("ems-orderstoexecute", Connection = "ordersHubConnection", ConsumerGroup = "signalr")] EventData[] events,
            [SignalR(HubName = "allOrders", ConnectionStringSetting = "ordersSignalRHub")] IAsyncCollector<SignalRMessage> outputRealtimeOrderMessages,
            ILogger log)
        {
            //Process received events
            await Parallel.ForEachAsync(events, async (eventData, token) =>
            {
                try
                {
                    //Deserialize event to business entity
                    string orderJson = Encoding.UTF8.GetString(eventData.EventBody);
                    var order = JsonConvert.DeserializeObject<Order>(orderJson);
                    log.LogInformation(orderJson);
                    await outputRealtimeOrderMessages.AddAsync(
                        new SignalRMessage
                        {
                            Target = "executedOrder",
                            Arguments = new[] { orderJson }
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
