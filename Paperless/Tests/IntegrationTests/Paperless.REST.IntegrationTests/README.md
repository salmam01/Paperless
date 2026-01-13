# Integration Test for Paperless

## Document Upload Integration Test

The `DocumentUploadIntegrationTests` covers the complete "document upload" use case:

1. **Upload** - Document is uploaded via REST API
2. **Storage** - Document is stored in MinIO (mocked)
3. **RabbitMQ** - Message is sent to queue (mocked)
4. **Database** - Document is saved in PostgreSQL (real Testcontainer database)

### How It Works

- **WebApplicationFactory**: Creates a test host for the ASP.NET Core application
- **Testcontainers**: Uses real PostgreSQL containers for realistic testing
- **Mocks**: External services (MinIO, RabbitMQ) are mocked

### Requirements

- .NET 8.0 SDK
- xUnit Test Framework
- Microsoft.AspNetCore.Mvc.Testing
- Testcontainers.PostgreSql (NuGet package)
- Docker Desktop (must be running for Testcontainers)

## Running the Tests

### Visual Studio / Rider

1. **Important**: Make sure Docker Desktop is running
2. Open Test Explorer
3. Run `DocumentUploadIntegrationTests`
4. All tests should pass

### Command Line

```bash

# Navigate to test project
cd Paperless/Tests/IntegrationTests/Paperless.REST.IntegrationTests

# Run all tests
dotnet test

# Run only integration tests
dotnet test --filter "FullyQualifiedName~DocumentUploadIntegrationTests"

# With detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Docker (optional)

```bash
# Make sure all containers are running
cd Paperless
docker compose up -d

# Run tests in container
docker compose exec paperless-rest dotnet test
```

## Test Scenarios

### Document Upload Tests

#### 1. UploadDocument_CompleteFlow_Success

Tests the complete upload flow. Expects:

- HTTP 201 Created
- Document saved in database
- RabbitMQ message published

#### 2. UploadDocument_InvalidFile_ReturnsBadRequest

Tests validation for empty files. Expects:

- HTTP 400 Bad Request

#### 3. UploadDocument_PDF_StoredInMinIO

Tests PDF upload and storage. Expects:

- HTTP 201 Created
- Storage service called

## Troubleshooting

### Docker is not running

Testcontainers needs Docker Desktop. Make sure Docker Desktop is running before executing tests.

### Container startup timeout

- Check if Docker Desktop has enough resources (CPU/RAM)
- Increase timeout in Testcontainers configuration if needed
- Check Docker logs for more details

### Port already in use

Testcontainers uses random ports, so this shouldn't be an issue. If it is, check if other containers are running.

### Program class not found

The `Program.cs` needs `public partial class Program { }` at the end so WebApplicationFactory can access it. (This is
already added)

### Categories missing in database

The factory automatically creates test categories during setup. If tests fail, check if categories were created
correctly.

## Notes

If you have questions or run into issues, check the main project README.md.
