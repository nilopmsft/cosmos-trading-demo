using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using trading_model;
using System.Text;
using System.Text.RegularExpressions;

namespace stocktrades.Services
{
    public class CosmosService : ICosmosService
    {
        public readonly CosmosClient _client;

        public CosmosService(string connectionString)
        {
            _client = new CosmosClient(
                connectionString: connectionString
            );
        }

        private Container portfolio_container
        {
            get => _client.GetDatabase("trading").GetContainer("customerPortfolio");
        }
        private Container session_container
        {
            get => _client.GetDatabase("trading").GetContainer("sessionState");
        }

        private Container marketdata_container
        {
            get => _client.GetDatabase("trading").GetContainer("marketdata");
        }

        private Container stocksummary_container
        {
            get => _client.GetDatabase("trading").GetContainer("stockPriceSummary");
        }

        public async Task<IEnumerable<string>> RetrieveUnusedUserIdsAsync()
        {
            string sql = "SELECT DISTINCT VALUE c.customerId FROM customerPortfolio c";

            var query = new QueryDefinition(
                query: sql
            );

            using FeedIterator<string> feed = portfolio_container.GetItemQueryIterator<string>(
                queryDefinition: query
            );

            List<string> results = new();

            while (feed.HasMoreResults)
            {
                var response = await feed.ReadNextAsync();
                foreach (string item in response)
                {
                    results.Add(item);
                }
            }

            string user_sql = "SELECT VALUE s.content FROM sessionState s";

            query = new QueryDefinition(
                query: user_sql
            );

            using FeedIterator<string> user_feed = session_container.GetItemQueryIterator<string>(
                queryDefinition: query
            );

            List<string> user_results = new();

            while (user_feed.HasMoreResults)
            {
                var response = await user_feed.ReadNextAsync();
                foreach (string item in response)
                {
                    // This is where I am ripping apart the content of the session state content directly like 
                    // an absolute moron. It's a quick and dirty hack to grab what users are currently being 
                    // used by decoding the session information like a dweeb.
                    // But this means I can get rid of a whole useless container in Cosmos, this by all accounts
                    // is a win.
                    string session_content = Encoding.UTF8.GetString(Convert.FromBase64String(item));
                    Regex regex = new Regex("userId(.*)\\.");
                    string session_user = regex.Split(session_content).Last();
                    user_results.Add(session_user);
                }
            }

            return results.Except(user_results);
        }

        public async Task<List<CustomerPortfolio>> GetUserPortfolios(string username)
        {
            List<CustomerPortfolio> portfolios = new List<CustomerPortfolio>();
            try
            {
                PartitionKey partitionKey = new PartitionKeyBuilder()
                 .Add(username)
                 .Add("equities")
                 .Build();
                var query = new QueryDefinition(
                    query: "SELECT * FROM CustomerPortfolio"
                );
                using FeedIterator<CustomerPortfolio> user_feed = portfolio_container.GetItemQueryIterator<CustomerPortfolio>(
                    queryDefinition: query,
                    requestOptions: new QueryRequestOptions()
                    {

                        PartitionKey = partitionKey
                    }
                );

                while (user_feed.HasMoreResults)
                {
                    var response = await user_feed.ReadNextAsync();
                    foreach (CustomerPortfolio item in response)
                    {
                        portfolios.Add(item);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return portfolios;
        }

        public async Task<List<StockPrice>> GetStockPriceHistory(string symbol)
        {
            List<StockPrice> price_history = new();
            try
            {
                for (DateTime i = DateTime.UtcNow.AddDays(-7); (DateTime.UtcNow - i).TotalDays >= 0; i = i.AddDays(1))
                {
                    StockPrice? stock_price = null;

                    string stock_date_id = String.Format("{0}-{1}-{2}-{3}", i.Year, i.Month, i.Day, symbol);

                    //Lookup (point read) customer portfolio
                    using (var response = await marketdata_container.ReadItemStreamAsync(stock_date_id, new PartitionKey(symbol)))
                    {
                        if (response.StatusCode != HttpStatusCode.NotFound)
                        {
                            //Deserialize portfolio object if already exists for that customer/symbol
                            JsonSerializer serializer = new JsonSerializer();
                            using (StreamReader streamReader = new StreamReader(response.Content))
                            using (var reader = new JsonTextReader(streamReader))
                            {
                                stock_price = serializer.Deserialize<StockPrice>(reader);
                                if (stock_price != null)
                                {
                                    price_history.Add(stock_price);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return price_history;
        }

        public async Task<StockPriceSummary> GetStockPriceSummary()
        {
            StockPriceSummary stocks = new();

            //Build hierarchical partition key (currently preview feature)
            PartitionKey partitionKey = new PartitionKeyBuilder()
                 .Add("stockprice_summary")
                 .Build();

            //Lookup (point read) customer portfolio
            using (var response = await stocksummary_container.ReadItemStreamAsync("stockprice_summary", partitionKey))
            {
                if (response.StatusCode != HttpStatusCode.NotFound)
                {
                    //Deserialize portfolio object if already exists for that customer/symbol
                    JsonSerializer serializer = new JsonSerializer();
                    using (StreamReader streamReader = new StreamReader(response.Content))
                    using (var reader = new JsonTextReader(streamReader))
                    {
                        if(reader != null)
                        {
                            stocks = serializer.Deserialize<StockPriceSummary>(reader);
                        }
                    }
                }
            }

            return stocks;
        }
    }
}
