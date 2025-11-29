# Payload Log Query

A simple ASP.NET Core web application that displays and queries payload logs for services running in AKS with Kong Mesh. Authentication is intentionally omitted. The app exposes:

- `GET /payload-log?serviceName={serviceName}&sessionId={sessionId}` — paginated JSON results with filters
- `GET /payload-log/stream` — Server-Sent Events (SSE) stream for progressive loading
- `GET /metadata` — list of available `serviceName -> [sessionId]` pairs for the UI dropdowns

## Features
- Local directory or Azure Blob Storage log sources (configurable)
- Pagination (`page`, `pageSize`) and filters (keyword, time range, status code)
- SSE streaming endpoint for progressive loading
- Minimal UI with dropdowns and filters; timestamp in a dedicated column and all other content in a single large column
- Caching of service/session pairs for fast UI population

## Requirements
- .NET SDK 10.0 (e.g., `10.0.100`)

## Quick Start
1. Build:
   ```bash
   dotnet build
   ```
2. Run:
   ```bash
   dotnet run
   ```
3. Open:
   - Browser: `http://localhost:5037/`
   - Default source is `Local` reading from `Logs/`

A sample file is included: `Logs/sample-service-12345.log`. Select `serviceName=sample-service`, `sessionId=12345` in the UI.

## Configuration
The app uses `appsettings.json` for configuration.

```json
{
  "LogSource": {
    "Source": "Local"  // Local or Azure
  },
  "Local": {
    "LogDirectory": "Logs"
  },
  "Azure": {
    "ConnectionString": "",
    "ContainerName": ""
  }
}
```

- Set `LogSource.Source` to `Local` to read logs from a directory.
- Set `LogSource.Source` to `Azure` to read logs from an Azure Storage container.
- Log file naming must follow: `{serviceName}-{sessionId}.log`.

You can also use environment variables:
- `LogSource__Source`
- `Local__LogDirectory`
- `Azure__ConnectionString`
- `Azure__ContainerName`

Example (Azure):
```bash
export LogSource__Source=Azure
export Azure__ConnectionString="<your-connection-string>"
export Azure__ContainerName="<your-container>"
```

## API Reference
- `GET /payload-log`
  - Query parameters:
    - `serviceName` (string, required)
    - `sessionId` (string, required)
    - `q` (string, optional) — keyword
    - `from` (ISO8601 datetime, optional)
    - `to` (ISO8601 datetime, optional)
    - `status` (int, optional) — HTTP status code (e.g., 200)
    - `page` (int, default 1)
    - `pageSize` (int, default 100, max 500)
  - Response (JSON):
    ```json
    {
      "entries": [ { "timestamp": "2025-11-29T08:00:00Z", "content": "..." } ],
      "page": 1,
      "pageSize": 100,
      "hasMore": false,
      "totalMatched": 10
    }
    ```

- `GET /payload-log/stream`
  - Same query parameters as `/payload-log` except `page` and `pageSize` are ignored.
  - Response: `text/event-stream` messages with JSON payloads:
    ```text
    data: {"timestamp":"2025-11-29T08:00:00Z","content":"..."}
    ```

- `GET /metadata`
  - Response (JSON): `{ "serviceA": ["sess1","sess2"], "serviceB": ["sess9"] }`

## Frontend UI
- Page: `Index` at `http://localhost:5037/`
- Controls:
  - `Service` dropdown populated from `/metadata`
  - `Session` dropdown based on selected service
  - `Keyword`, `Status Code`, `From`, `To`, `Page`, `Page Size`
  - `Query` button fetches paged JSON results
  - `Stream` button opens SSE stream for progressive loading
- Table:
  - `Timestamp` column (ISO8601 or parsed from line)
  - `Content` column containing the full raw log line

## Log Format Assumptions
- Timestamp is parsed from the beginning of each line if present (e.g., `2025-11-29T08:00:00Z` or similar ISO8601 variants).
- Status code is extracted via heuristics:
  - `status=NNN`, `statusCode=NNN`, or JSON-like `"status": NNN`.
- Lines without a parseable timestamp are not time-filtered but still appear when other filters match.

## Pagination & Streaming
- Pagination returns a slice of filtered lines with `hasMore` and `totalMatched` for UX.
- SSE streaming yields matching lines progressively; use for large logs or a “live” feel. Note SSE is unidirectional; ensure intermediaries support streaming.

## Sample Data
A sample local file `Logs/sample-service-12345.log` is included with request/response entries containing timestamp, method, URL, status code, and bodies. Use it for testing the UI and API.

## Azure Setup Notes
- Ensure the Azure Storage connection string has read access to your container.
- Blob names should follow `{serviceName}-{sessionId}.log`.
- The app only performs reads; no writes to storage.

## Deployment Notes
- Designed for AKS + Kong Mesh where authentication is handled externally or not required.
- Consider setting environment variables via Kubernetes `Deployment` manifests.
- If running behind proxies or ingress, confirm SSE (`/payload-log/stream`) is supported.

## Troubleshooting
- Service/session list empty:
  - Check source config and that files exist (Local) or blobs exist (Azure).
- No results when filtering by time:
  - Ensure logs include parseable timestamps at the line start.
- Azure errors:
  - Validate `Azure__ConnectionString` and `Azure__ContainerName`.

## Future Enhancements
- Server-side chunked paging for very large logs
- Tail mode for actively appended logs
- Custom parsers per service to extract structured fields
- Download raw log file and export filtered results

