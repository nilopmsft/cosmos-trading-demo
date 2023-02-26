﻿using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using trading_model;

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
        private Container user_container
        {
            get => _client.GetDatabase("trading").GetContainer("users");
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

            string user_sql = "SELECT VALUE u.userId FROM users u";

            query = new QueryDefinition(
                query: user_sql
            );

            using FeedIterator<string> user_feed = user_container.GetItemQueryIterator<string>(
                queryDefinition: query
            );

            List<string> user_results = new();

            while (user_feed.HasMoreResults)
            {
                var response = await user_feed.ReadNextAsync();
                foreach (string item in response)
                {
                    user_results.Add(item);
                }
            }

            return results.Except(user_results);
        }

        public async Task ClaimUserAsync(Models.User user)
        {
            await user_container.CreateItemAsync(user);
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
    }
}
