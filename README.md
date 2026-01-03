# Payload Log Query
A lightweight ASP.NET Core web application designed to query and stream logs from Local Storage or Azure Blob Storage.

## Features
- **Stream-First Architecture**: Uses Server-Sent Events (SSE) for low-latency log streaming.
- **Unified Querying**: Supports filtering by Service, Session, Time Range, Status Code, and Keywords.
- **Azure Blob Storage Support**: Seamlessly reads logs from Azure Containers using `DefaultAzureCredential`.
- **Automatic Decryption**: Automatically handles encrypted log messages using an internal library simulation.
- **.NET 8 Best Practices**: Uses DI, Configuration, and Azure SDK extensions.

## Getting Started

### Prerequisites
- .NET 8.0 SDK

### Configuration
`appsettings.json` controls the log source.

#### Local Mode (Default)
Query logs from a local directory.
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

#### Azure Mode
Query logs from Azure Blob Storage Container.
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
*Note: Authentication uses `DefaultAzureCredential`. Ensure you are logged in via `az login` or have appropriate environment variables set.*

### Running the App
1. **Build:**
   ```bash
   dotnet build payload-log-query.sln
   ```
2. **Generate Sample Data (Local only):**
   ```bash
   dotnet run -- generate-data
   ```
3. **Run:**
   ```bash
   dotnet run
   ```
4. **Access UI:**
   Open `http://localhost:5038` (or similar) in your browser.

## Development

### Project Structure
- **`Abstractions`**: Core interfaces (`ILogProvider`, `IDecryptionService`).
- **`Providers`**: `AzureBlobLogProvider` and `LocalLogProvider`.
- **`Services`**: `KeyVaultDecryptionService` (uses internal crypto lib) and `LocalDecryptionService`.
- **`Utils`**: `InternalLogCrypto` (simulation of internal library) and `LogGenerator`.
- **`Options`**: strongly-typed configuration.

### Log Decryption
The application is designed to handle encrypted logs using `Utils.InternalLogCrypto`.
- `IDecryptionService` abstracts the decryption logic.
- `KeyVaultDecryptionService` delegates to the internal library `DecryptLogMessage`.
