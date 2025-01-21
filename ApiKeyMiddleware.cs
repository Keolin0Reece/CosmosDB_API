using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CosmosDbAppService
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _apiKey;
        private readonly ILogger<ApiKeyMiddleware> _logger;

        public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
        {
            _next = next;
            _apiKey = configuration["ApiKeys:PrimaryKey"];
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            _logger.LogInformation("Processing request for {Path}", context.Request.Path);

            if (!context.Request.Headers.TryGetValue("Authorization", out var extractedApiKey))
            {
                _logger.LogInformation("Authorization header is missing for request to {Path}", context.Request.Path);

                context.Response.StatusCode = 401; // Unauthorized
                await context.Response.WriteAsync("API Key is missing.");
                return;
            }

            if (!extractedApiKey.Equals($"Bearer {_apiKey}"))
            {
                _logger.LogInformation("Invalid API Key provided for request to {Path}", context.Request.Path);

                context.Response.StatusCode = 403; // Forbidden
                await context.Response.WriteAsync("Invalid API Key.");
                return;
            }

            _logger.LogInformation("Valid API Key provided for request to {Path}", context.Request.Path);

            await _next(context);
        }
    }
}
