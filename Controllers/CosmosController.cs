using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using CosmosDbAppService.Models;


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

    [HttpGet("last-hour")]
    public async Task<IActionResult> GetLastHour([FromQuery] string deviceId)
    {
        try
        {
            // Get the current UTC time
            var currentTimeUtc = DateTime.UtcNow;

            // Calculate the timestamp for 1 hour ago (subtracting 1 hour)
            var oneHourAgoUtc = currentTimeUtc.AddHours(-1);

            // Format the time in ISO 8601 format (without milliseconds, matching the 'PublishedAt' format)
            var formattedTime = oneHourAgoUtc.ToString("yyyy-MM-ddTHH:mm:ss");

            // Cosmos DB query to fetch records from the last hour
            var query = $"SELECT c.Data.pV, c.PublishedAt FROM c WHERE c.PublishedAt >= '{formattedTime}' AND c.deviceId = '{deviceId}'";

            // Execute the query
            var items = await _cosmosDbService.QueryItemsAsync<dynamic>(query);

            // Return the results
            return Ok(items);
        }
        catch (Exception ex)
        {
            // Return an error response if something goes wrong
            return StatusCode(500, new { error = ex.Message });
        }
    }

}