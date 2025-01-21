using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;


[ApiController]
[Route("api/[controller]")]
public class CosmosController : ControllerBase
{
    private readonly CosmosDbService _cosmosDbService;
    private readonly ILogger<CosmosController> _logger;

    public CosmosController(CosmosDbService cosmosDbService, ILogger<CosmosController> logger)
    {
        _cosmosDbService = cosmosDbService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] dynamic requestBody)
    {
        try
        {
            _logger.LogInformation("Processing POST request to store data in Cosmos DB.");

            // Parse the raw data from the requestBody
            string eventId = requestBody.EventId;
            string eventName = requestBody.Event;
            string deviceId = requestBody.DeviceId;
            string publishedAt = requestBody.PublishedAt;
            string data = requestBody.Data;
            string userId = requestBody.userid;
            string productId = requestBody.ProductId;
            string fwVersion = requestBody.FwVersion;

            _logger.LogInformation("Parsed request data: EventId={EventId}, DeviceId={DeviceId}", eventId, deviceId);

            // Validate the 'Data' field
            if (string.IsNullOrEmpty(data))
            {
                _logger.LogInformation("Data field is missing in the request.");
                return BadRequest(new { error = "Data is required." });
            }

            // Convert the 'Data' field to a dictionary
            Dictionary<string, object> eventData;
            try
            {
                eventData = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            }
            catch (JsonException ex)
            {
                _logger.LogInformation(ex, "Failed to deserialize the 'Data' field.");
                return BadRequest(new { error = "Data field is not valid JSON." });
            }

            // Create the ParticleEvent object
            var particleEvent = new ParticleEvent
            {
                EventId = eventId,
                Event = eventName,
                DeviceId = deviceId,
                Data = eventData,
                PublishedAt = DateTime.Parse(publishedAt),
                UserId = userId,
                ProductId = productId,
                FwVersion = fwVersion
            };

            // Store in Cosmos DB
            await _cosmosDbService.AddItemAsync(particleEvent);
            _logger.LogInformation("Data successfully stored in Cosmos DB.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "An error occurred while processing the POST request.");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("event-data")]
    public async Task<IActionResult> GetDeviceData()
    {
        try
        {
            _logger.LogInformation("Processing GET request to fetch device data.");

            var query = "SELECT c.deviceId, c.Data.pC, c.Data.dID FROM c";
            var items = await _cosmosDbService.QueryItemsAsync<dynamic>(query);

            _logger.LogInformation("Successfully fetched device data.");
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "An error occurred while fetching device data.");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
public class ParticleEvent
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("EventId")]
    public string EventId { get; set; }

    [JsonProperty("Event")]
    public string Event { get; set; }

    [JsonProperty("Data")]
    public Dictionary<string, object> Data { get; set; }

    [JsonProperty("deviceId")]
    public string DeviceId { get; set; }

    [JsonProperty("PublishedAt")]
    public DateTime PublishedAt { get; set; }

    [JsonProperty("userid")]
    public string UserId { get; set; }

    [JsonProperty("ProductId")]
    public string ProductId { get; set; }

    [JsonProperty("FwVersion")]
    public string FwVersion { get; set; }
}