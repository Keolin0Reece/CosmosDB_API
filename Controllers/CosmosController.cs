using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using CosmosDbAppService.Models;

/// <summary>
/// Controller for handling Cosmos DB operations related to particle events and device data.
/// Provides endpoints for storing and retrieving event data with various filtering options.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CosmosController : ControllerBase
{
    private readonly CosmosDbService _cosmosDbService;

    public CosmosController(CosmosDbService cosmosDbService)
    {
        _cosmosDbService = cosmosDbService;
    }

    /// <summary>
    /// Stores a new particle event in the Cosmos DB.
    /// </summary>
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

    /// <summary>
    /// Retrieves device event data based on optional filter criteria.
    /// </summary>
    /// <param name="deviceId">Optional device identifier</param>
    /// <param name="userId">Optional user identifier</param>
    /// <param name="eventName">Optional event name</param>
    /// <returns>Filtered event data or total count if no filters are provided</returns>
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

    /// <summary>
    /// Retrieves data for a specific device within a time range.
    /// If no time range is specified, returns data from the last hour.
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startDate">Optional start date in ISO 8601 format</param>
    /// <param name="endDate">Optional end date in ISO 8601 format</param>
    /// <returns>Device data within the specified time range</returns>
    [HttpGet("last-hour")]
    public async Task<IActionResult> GetLastHour([FromQuery] string deviceId, [FromQuery] string startDate = null, [FromQuery] string endDate = null)
    {
        try
        {
            DateTime startTime, endTime;

            // If startDate and endDate are provided, parse them
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                if (!DateTime.TryParse(startDate, out startTime) || !DateTime.TryParse(endDate, out endTime))
                {
                    return BadRequest(new { error = "Invalid startDate or endDate format. Use ISO 8601 format." });
                }
            }
            else
            {
                // If startDate or endDate are null, use the previous hour as the default range
                endTime = DateTime.UtcNow;
                startTime = endTime.AddHours(-1);
            }

            // Format the dates in ISO 8601 format (matching the 'PublishedAt' format)
            var formattedStartTime = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
            var formattedEndTime = endTime.ToString("yyyy-MM-ddTHH:mm:ss");

            // Cosmos DB query to fetch records between the specified time range
            var query = $"SELECT c.Data, c.PublishedAt FROM c WHERE c.PublishedAt >= '{formattedStartTime}' AND c.PublishedAt <= '{formattedEndTime}' AND c.deviceId = '{deviceId}'";

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

     /// <summary>
    /// Retrieves a specific property from device data within a time range.
    /// </summary>
    [HttpGet("get-property")]
    public async Task<IActionResult> GetProperty([FromQuery] string deviceId, [FromQuery] string Data, [FromQuery] string startDate = null, [FromQuery] string endDate = null)
    {
        try
        {
            long formattedStartTime = ConvertIso8601ToUnix(startDate);
            long formattedEndTime = ConvertIso8601ToUnix(endDate);

            // Cosmos DB query to fetch records between the specified time range
            var query = $"SELECT c.Data.{Data}, c.Data.ts FROM c WHERE c.Data.ts >= {formattedStartTime} AND c.Data.ts <= {formattedEndTime} AND c.deviceId = '{deviceId}'";

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

    /// <summary>
    /// Converts an ISO 8601 date string to Unix timestamp.
    /// </summary>
    static long ConvertIso8601ToUnix(string isoDate)
    {
        DateTimeOffset dateTimeOffset = DateTimeOffset.Parse(isoDate);
        return dateTimeOffset.ToUnixTimeSeconds(); // Returns Unix timestamp in seconds
    }
}
