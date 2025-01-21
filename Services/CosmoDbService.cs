using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class CosmosDbService
{
    private readonly Container _container;

    public CosmosDbService(CosmosClient cosmosClient, string databaseName, string containerName)
    {
        _container = cosmosClient.GetContainer(databaseName, containerName);
    }

    public async Task AddItemAsync<T>(T item)
    {
        // Assuming the partition key is based on "DeviceId" in this case
        string partitionKeyValue = item.GetType().GetProperty("DeviceId")?.GetValue(item)?.ToString();

        if (string.IsNullOrEmpty(partitionKeyValue))
        {
            throw new InvalidOperationException("PartitionKey is missing or invalid.");
        }

        await _container.CreateItemAsync(item, new PartitionKey(partitionKeyValue));
    }


    // Retrieve an item by ID and partition key
    public async Task<T> GetItemAsync<T>(string id, string partitionKey)
    {
        try
        {
            var response = await _container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return default;
        }
    }

    //Query items in CosmosDB
    public async Task<IEnumerable<T>> QueryItemsAsync<T>(string query)
    {
        var queryDefinition = new QueryDefinition(query);
        var queryIterator = _container.GetItemQueryIterator<T>(queryDefinition);

        List<T> results = new List<T>();
        while (queryIterator.HasMoreResults)
        {
            var response = await queryIterator.ReadNextAsync();
            results.AddRange(response.ToList());
        }

        return results;
    }

    // Retrieve all items from the container
    public async Task<IEnumerable<T>> GetItemsAsync<T>()
    {
        var query = _container.GetItemQueryIterator<T>();
        List<T> results = new List<T>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }
        return results;
    }
}