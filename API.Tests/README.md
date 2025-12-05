# API.Tests

NUnit integration test project for Scan-Lab API endpoints.

## Prerequisites

Same as Libs.Tests - requires PostgreSQL running locally.

## Running Tests

```bash
dotnet test
```

## Test Structure

```
API.Tests/
├── Controllers/       # Controller integration tests using WebApplicationFactory
└── README.md
```

## Using WebApplicationFactory

Tests use `Microsoft.AspNetCore.Mvc.Testing` to create an in-memory test server:

```csharp
public class ScannerProfileControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ScannerProfileControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Test]
    public async Task GetProfiles_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/ScannerProfile/profiles");
        Assert.That(response.IsSuccessStatusCode, Is.True);
    }
}
```

## Authentication in Tests

For endpoints requiring JWT authentication, add token to requests:

```csharp
_client.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
```
