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

    public CosmosController(CosmosDbService cosmosDbService)
    {
        _cosmosDbService = cosmosDbService;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] dynamic requestBody)
    {
        try
        {
            // Parse the raw data from the requestBody
            string eventId = requestBody.EventId;
            string eventName = requestBody.Event;
            string deviceId = requestBody.DeviceId;
            string publishedAt = requestBody.PublishedAt;
            string data = requestBody.Data;
            string userId = requestBody.userid;
            string productId = requestBody.ProductId;
            string fwVersion = requestBody.FwVersion;

            // Validate the 'Data' field
            if (string.IsNullOrEmpty(data))
            {
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

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("event-data")]
    public async Task<IActionResult> GetDeviceData([FromQuery] string deviceId = null, [FromQuery] string userId = null, [FromQuery] string eventName = null)
    {
        try
        {
            string query;

            // Check if any query parameters are provided
            if (!string.IsNullOrEmpty(deviceId) || !string.IsNullOrEmpty(userId) || !string.IsNullOrEmpty(eventName))
            {
                // Build the query dynamically based on provided parameters
                query = "SELECT c.deviceId FROM c WHERE 1=1";

                if (!string.IsNullOrEmpty(deviceId))
                {
                    query += $" AND c.deviceId = '{deviceId}'";
                }

                if (!string.IsNullOrEmpty(userId))
                {
                    query += $" AND c.userid = '{userId}'";
                }

                if (!string.IsNullOrEmpty(eventName))
                {
                    query += $" AND c.Event = '{eventName}'";
                }
            }
            else
            {
                // No parameters provided, return the count of items in the container
                query = "SELECT VALUE COUNT(1) FROM c";
            }

            var items = await _cosmosDbService.QueryItemsAsync<dynamic>(query);

            return Ok(items);
        }
        catch (Exception ex)
        {
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