# Cosmos DB API Service

A .NET Core Web API service that provides an interface for storing and retrieving event data using Azure Cosmos DB. This project demonstrates implementation patterns for Cosmos DB integration with authentication middleware.

## Project Overview

This service is designed to handle particle events and device data, providing endpoints for:
- Storing event data
- Querying events by various parameters
- Retrieving time-based data
- Accessing specific device properties

## Architecture

### Key Components

1. **API Controllers**
   - `CosmosController`: Handles all HTTP endpoints for Cosmos DB operations
   - Supports POST and GET operations with various filtering options
   - Implements error handling and validation

2. **Services**
   - `CosmosDbService`: Core service for Cosmos DB operations
   - Manages CRUD operations and query execution
   - Implements partition key handling

3. **Middleware**
   - `ApiKeyMiddleware`: Handles API key authentication
   - Validates Bearer tokens in Authorization header
   - Provides 401/403 responses for invalid authentication

### Security

- API Key authentication required for all endpoints
- Bearer token format: `Bearer {apiKey}`
- Keys configured through application settings

## API Endpoints

### POST /api/cosmos
Stores a new particle event in the database.

**Request Body:**
```json
{
    "EventId": "string",
    "Event": "string",
    "DeviceId": "string",
    "PublishedAt": "string",
    "Data": "json_string",
    "userid": "string",
    "ProductId": "string",
    "FwVersion": "string"
}
```

### GET /api/cosmos/event-data
Retrieves device event data with optional filtering.

**Query Parameters:**
- `deviceId` (optional): Filter by device
- `userId` (optional): Filter by user
- `eventName` (optional): Filter by event type

### GET /api/cosmos/last-hour
Retrieves data for a specific device within a time range.

**Query Parameters:**
- `deviceId`: Required device identifier
- `startDate` (optional): ISO 8601 format
- `endDate` (optional): ISO 8601 format

### GET /api/cosmos/get-property
Retrieves specific property values from device data.

**Query Parameters:**
- `deviceId`: Device identifier
- `Data`: Property name to retrieve
- `startDate`: Start date in ISO 8601 format
- `endDate`: End date in ISO 8601 format

## Configuration

### Required Settings

```json
{
  "CosmosDb": {
    "AccountEndpoint": "your_cosmos_db_endpoint",
    "AccountKey": "your_account_key",
    "DatabaseName": "your_database_name",
    "ContainerName": "your_container_name"
  },
  "ApiKeys": {
    "PrimaryKey": "your_api_key"
  }
}
```

### Dependencies

- .NET Core 6.0+
- Microsoft.Azure.Cosmos
- Newtonsoft.Json
- Microsoft.AspNetCore.Mvc.NewtonsoftJson

## Project Structure

```
├── Controllers/
│   └── CosmosController.cs
├── Services/
│   └── CosmosDbService.cs
├── Middleware/
│   └── ApiKeyMiddleware.cs
├── Models/
│   └── CosmosDbSettings.cs
└── Program.cs
```

## Error Handling

The service implements comprehensive error handling:

- 400 Bad Request: Invalid input data
- 401 Unauthorized: Missing API key
- 403 Forbidden: Invalid API key
- 500 Internal Server Error: Server-side issues

All error responses include descriptive messages in the response body.

## Best Practices Implemented

1. **Dependency Injection**
   - Services registered with appropriate lifetimes
   - Configuration injected through IConfiguration

2. **Security**
   - API Key authentication
   - HTTPS redirection
   - Authorization middleware

3. **Data Access**
   - Partition key validation
   - Async operations
   - Query optimization

4. **Error Handling**
   - Global exception handling
   - Specific error messages
   - Appropriate status codes
