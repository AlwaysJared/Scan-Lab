# Libs.Tests

NUnit test project for Scan-Lab shared library components.

## Prerequisites

- PostgreSQL running locally on default port (5432)
- Database user: `postgres` with password: `changeme`
- The test database `ScanLabTest` will be created/deleted automatically

### Docker PostgreSQL Setup (Optional)

If you don't have PostgreSQL installed:

```bash
docker run --name scanlab-test-db \
  -e POSTGRES_PASSWORD=changeme \
  -p 5432:5432 \
  -d postgres:16
```

## Running Tests

### All tests:
```bash
dotnet test
```

### Specific category:
```bash
dotnet test --filter "TestCategory=Unit"
dotnet test --filter "TestCategory=Integration"
```

### Single test:
```bash
dotnet test --filter "FullyQualifiedName~HS1800StrategyTests.ResolveWatchPath_AppendsToday_InCorrectFormat"
```

## Test Structure

```
Libs.Tests/
├── Helpers/              # Shared test utilities
│   ├── TestBase.cs       # Base classes for tests
│   ├── TestDatabaseFixture.cs  # PostgreSQL test database setup
│   ├── TestDataBuilder.cs      # Helper to create test data
│   └── FileSystemTestHelper.cs # File system test utilities
├── Services/
│   └── ScannerStrategies/  # Strategy pattern tests
└── Repositories/          # Repository integration tests
```

## Test Guidelines

1. **Inherit from base classes:**
   - `TestBase` - Full setup (DB + file system)
   - `DatabaseTestBase` - Database only
   - `FileSystemTestBase` - File system only

2. **Use test data builders:**
   ```csharp
   var profile = TestDataBuilder.CreateTestProfile();
   var scanner = TestDataBuilder.CreateTestScanner(profile: profile);
   ```

3. **Clean up is automatic:**
   - Test databases are deleted after each test
   - Temporary directories are cleaned up

4. **Test PostgreSQL connection:**
   ```csharp
   [Test]
   public void DatabaseConnection_Succeeds()
   {
       Assert.That(DbFixture.Context.Database.CanConnect(), Is.True);
   }
   ```

## Important Notes

- Each test gets a **fresh database** with migrations applied
- File system tests use **real temp directories** (not mocked)
- Tests run **sequentially** with database cleanup between each
- The 1-second delay for HS-1800 strategy is configured via ProfileConfiguration
