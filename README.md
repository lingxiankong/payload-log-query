# Payload Log Query

A lightweight ASP.NET Core web application designed to query and stream payload logs from Local Storage or Azure Blob Storage. It features a responsive UI with real-time streaming capabilities and cursor-based pagination.

## Features

- **Stream-First Architecture**: Uses Server-Sent Events (SSE) to stream logs directly to the browser, ensuring low latency even for large datasets.
- **Unified Querying**: Supports filtering by Service, Session, Time Range, Status Code, and Keywords.
- **Cursor-Based Pagination**: Efficient "Load More" functionality using timestamp cursors (`ExcludeFrom` logic) to fetch subsequent batches without overlap.
- **Secure Azure Integration**: Connects to Azure Blob Storage using Managed Identity (`DefaultAzureCredential`), eliminating the need for connection strings.
- **Log Decryption**: Extensible `IDecryptionService` to handle encrypted log entries (supports Local mock and KeyVault stub).
- **Zero-Maintenance UI**: Metadata is cached for fast loading of Service/Session lists.

## Requirements

- .NET SDK 8 or later

## Quick Start

1. **Build the project:**
   ```bash
   dotnet build PayloadLogQuery.csproj
   ```

2. **Generate sample data (optional):**
   ```bash
   dotnet run -- generate-data
   # Generates Logs/demoService-demoSession1.log
   ```

3. **Run the application:**
   ```bash
   dotnet run
   ```

4. **Access the UI:**
   - Open browser to `http://localhost:5037`
   - Select `demoService` and `demoSession1` (if generated) or use your own logs in `Logs/` directory.

## Configuration

The application is configured via `appsettings.json` or Environment Variables.

### Local Storage (Default)
```json
{
  "LogSource": {
    "Source": "Local"
  },
  "Local": {
    "LogDirectory": "Logs"
  }
}
```

### Azure Blob Storage
Uses Azure Managed Identity. Ensure the environment has `AzureBlobDataReader` access.

```json
{
  "LogSource": {
    "Source": "Azure"
  },
  "Azure": {
    "StorageAccountName": "yourstorageaccount",
    "ContainerName": "yourcontainer"
  }
}
```

**Environment Variables:**
- `LogSource__Source`: `Local` or `Azure`
- `Azure__StorageAccountName`: Your Azure Storage Account Name
- `Azure__ContainerName`: Blob Container Name

## Streaming Implementation

The core log retrieval is built on **Server-Sent Events (SSE)**.

1.  **Backend**:
    - The `ILogProvider.StreamAsync` method returns an `IAsyncEnumerable<LogEntry>`.
    - The `/payload-log/stream` endpoint writes these entries directly to the HTTP response stream as `data: {...}` lines.
    - This allows the server to process and send logs line-by-line without buffering the entire dataset in memory.

2.  **Frontend**:
    - Uses the browser's `EventSource` API to consume the stream.
    - **Load More**: When the user clicks "Load More", the frontend closes the current stream and opens a new one, passing the timestamp of the last received log as the `from` parameter with `excludeFrom=true`. This ensures the stream picks up exactly where it left off.

## API Reference

### `GET /metadata`
Returns a JSON object mapping Service Names to available Session IDs.
```json
{
  "serviceA": ["session1", "session2"],
  "demoService": ["demoSession1"]
}
```

### `GET /payload-log/stream`
Streams log entries matching the criteria.

**Parameters:**
- `serviceName` (Required): Name of the service.
- `sessionId` (Required): Session identifier.
- `q` (Optional): Keyword search within the log content.
- `status` (Optional): Filter by HTTP Status Code (e.g., 200, 404).
- `from` (Optional): Start timestamp (ISO 8601).
- `to` (Optional): End timestamp (ISO 8601).
- `limit` (Optional): Max number of records to stream (default 100).
- `excludeFrom` (Optional): If `true`, the `from` timestamp is exclusive (`>`). Used for pagination.

**Response Structure (SSE)**:
```text
data: {"timestamp":"2025-12-20T10:00:01Z", "content":"..."}

data: {"timestamp":"2025-12-20T10:00:02Z", "content":"..."}
```

## Project Structure

- **`Abstractions/`**: Core interfaces (`ILogProvider`, `IDecryptionService`).
- **`Providers/`**: Implementations for Local file system and Azure Blob Storage.
- **`Services/`**: Decryption logic (`LocalDecryptionService`, `KeyVaultDecryptionService`) and caching.
- **`Middleware/`**: (None currently, logic is in `Program.cs` minimal APIs).
- **`wwwroot/js/logs.js`**: Frontend logic for Metadata loading, EventSource handling, and infinite scroll simulation via "Load More".
