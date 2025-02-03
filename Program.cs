using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using CosmosDbAppService.Models;
using CosmosDbAppService;

var builder = WebApplication.CreateBuilder(args);


// Add controllers and configure them to use Newtonsoft.Json
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.Formatting = Formatting.Indented;
        options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
    });

builder.Services.AddEndpointsApiExplorer();

// Bind the Cosmos DB settings from appsettings.json
builder.Services.Configure<CosmosDbSettings>(
    builder.Configuration.GetSection("CosmosDb"));

// Add Cosmos DB client as a singleton
builder.Services.AddSingleton<CosmosDbService>(s =>
{
    // Fetch settings from appsettings.json
    var config = s.GetRequiredService<IConfiguration>();
    var cosmosSettings = config.GetSection("CosmosDb").Get<CosmosDbSettings>();

    // Initialize CosmosClient using settings
    var cosmosClient = new CosmosClient(cosmosSettings.AccountEndpoint, cosmosSettings.AccountKey);
    return new CosmosDbService(cosmosClient, cosmosSettings.DatabaseName, cosmosSettings.ContainerName);
});

var app = builder.Build();

app.UseMiddleware<ApiKeyMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
