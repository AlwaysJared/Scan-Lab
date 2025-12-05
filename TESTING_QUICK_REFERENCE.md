# Testing Quick Reference Card

## Quick Start

### 1. Start PostgreSQL
```bash
docker run --name scanlab-test-db -e POSTGRES_PASSWORD=changeme -p 5432:5432 -d postgres:16
```

### 2. Run All Tests
```bash
dotnet test
```

### 3. Run Specific Test
```bash
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

---

## Writing a New Test

### Database Test
```csharp
using Libs.Tests.Helpers;
using NUnit.Framework;

public class MyRepositoryTests : DatabaseTestBase
{
    [Test]
    public async Task MyTest()
    {
        // Arrange - DbFixture available
        var repository = new MyRepository(DbFixture.Context);
        var testData = TestDataBuilder.CreateTestScanner();

        // Act
        var result = await repository.DoSomething(testData);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
    }
}
```

### File System Test
```csharp
public class StrategyTests : FileSystemTestBase
{
    [Test]
    public void MyTest()
    {
        // Arrange - TestDirectory available
        var strategy = new HS1800Strategy();
        var testDir = FileSystemTestHelper.CreateHS1800Structure(TestDirectory);

        // Act
        var result = strategy.ResolveWatchPath(...);

        // Assert
        Assert.That(result, Does.Contain(testDir));
    }
}
```

### Integration Test (Database + File System)
```csharp
public class WatcherTests : TestBase
{
    [Test, Timeout(5000)]
    public async Task MyTest()
    {
        // Both DbFixture and TestDirectory available
        var scanner = TestDataBuilder.CreateTestScanner(TestDirectory);
        DbFixture.Context.Scanners.Add(scanner);
        await DbFixture.Context.SaveChangesAsync();

        var rollDir = FileSystemTestHelper.CreateRollFolder(TestDirectory, "0001234");

        // ... test logic
    }
}
```

---

## Test Data Builders

```csharp
// Customer
var customer = TestDataBuilder.CreateTestCustomer(
    firstName: "Jane",
    lastName: "Smith"
);

// Scanner
var scanner = TestDataBuilder.CreateTestScanner(
    watchedDir: "/custom/path",
    destinationDir: "/custom/dest",
    archiveDir: "/custom/archive"
);

// Order
var order = TestDataBuilder.CreateTestOrder(
    orderId: "CUSTOM123",
    scanner: scanner,
    customer: customer,
    customerInitials: "JS"
);

// Roll
var roll = TestDataBuilder.CreateTestRoll(
    order: order,
    rollNumber: 42,
    status: RollStatus.ScanningInProgress
);
```

**Note:** Profile-related builders (`CreateTestProfile`, `CreateTestProfileConfig`) are commented out until Phase 1 implementation creates the models.

---

## File System Helpers

```csharp
// Create temp directory
var testDir = FileSystemTestHelper.CreateTempTestDirectory();

// Create HS-1800 daily structure
var dailyFolder = FileSystemTestHelper.CreateHS1800Structure(testDir);
// Result: /tmp/ScanLabTest.../20251129/

// Create roll with images
var rollDir = FileSystemTestHelper.CreateRollFolder(dailyFolder, "0001234", imageCount: 10);
// Result: /tmp/.../20251129/0001234/ with IMG_0001.jpg ... IMG_0010.jpg

// Cleanup (automatic in TestBase teardown)
FileSystemTestHelper.CleanupTestDirectory(testDir);
```

---

## Common Assertions

```csharp
// Success/Failure
Assert.That(response.IsSuccess, Is.True);
Assert.That(response.Message, Does.Contain("error"));

// Collections
Assert.That(profiles, Has.Count.EqualTo(3));
Assert.That(scanners, Is.Not.Empty);
Assert.That(profiles, Has.Some.Matches<ScannerProfile>(p => p.ProfileName == "HS-1800"));

// Strings
Assert.That(path, Does.StartWith("/test"));
Assert.That(fileName, Does.Match(@"IMG_\d{4}\.jpg"));

// Files/Directories
Assert.That(File.Exists(filePath), Is.True);
Assert.That(Directory.GetFiles(dir).Length, Is.GreaterThan(0));

// Exceptions
Assert.ThrowsAsync<InvalidOperationException>(async () => await method());

// Time ranges (for delay tests)
Assert.That(elapsed.TotalSeconds, Is.InRange(0.9, 1.2)); // 1 sec ± 0.1
```

---

## Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test Libs.Tests/Libs.Tests.csproj

# Specific class
dotnet test --filter "FullyQualifiedName~HS1800StrategyTests"

# Specific method
dotnet test --filter "FullyQualifiedName~HS1800StrategyTests.ResolveWatchPath_AppendsToday_InCorrectFormat"

# With detailed output
dotnet test --logger "console;verbosity=detailed"

# In watch mode (re-run on file change)
dotnet watch test
```

---

## Test Attributes

```csharp
[Test] // Standard test

[Test, Timeout(5000)] // 5 second timeout

[TestCase(1, "HS1800Strategy")] // Parameterized
[TestCase(2, "SP500Strategy")]
public void MyTest(int id, string strategy) { }

[SetUp] // Runs before each test
[TearDown] // Runs after each test

[OneTimeSetUp] // Runs once before all tests in class
[OneTimeTearDown] // Runs once after all tests in class

[Ignore("Reason")] // Skip test temporarily

[Category("Integration")] // Group tests
// Run: dotnet test --filter "TestCategory=Integration"
```

---

## Debugging Tests

### VS Code
1. Set breakpoint in test
2. Click "Debug Test" in Test Explorer
3. Or use launch configuration

### Logs
```csharp
[Test]
public void MyTest()
{
    Console.WriteLine("Debug output");
    TestContext.WriteLine("Test-specific output");
}
```

### Check Database State
```csharp
[Test]
public async Task MyTest()
{
    // ... test code ...

    // Inspect database
    var scanners = await DbFixture.Context.Scanners.ToListAsync();
    Console.WriteLine($"Scanner count: {scanners.Count}");
}
```

---

## Manual Test Cases

Open: `MANUAL_TEST_CASES.md`

**Quick Flow:**
1. Start at TC-001
2. Execute each test
3. Check [ ] Pass or [ ] Fail
4. Add notes for failures
5. Move to next test

**24 test cases covering:**
- Database migrations (TC-001 to TC-002)
- Profile management (TC-003 to TC-008)
- Auto-processing (TC-009 to TC-013)
- Manual processing (TC-014 to TC-016)
- API endpoints (TC-017 to TC-020)
- Edge cases (TC-021 to TC-024)

---

## Common Issues

### "Connection refused" to PostgreSQL
```bash
# Check if running
docker ps

# Start container
docker start scanlab-test-db

# Or create new
docker run --name scanlab-test-db -e POSTGRES_PASSWORD=changeme -p 5432:5432 -d postgres:16
```

### Tests hang indefinitely
- Check for active watchers (restart API)
- Increase timeout: `[Test, Timeout(10000)]`
- Check for infinite loops

### "Database already exists"
- Each test should get unique DB name
- Check TestDatabaseFixture.cs uses Guid in name
- Manually clean: `DROP DATABASE ScanLabTest_...;`

### Temp directories not cleaned
- Automatic cleanup in TestBase.TearDown
- Manual: Check `/tmp/` for `ScanLabTest_*` folders

---

## File Locations

```
Scan-Lab/
├── Libs.Tests/
│   ├── Helpers/
│   │   ├── TestBase.cs
│   │   ├── TestDatabaseFixture.cs
│   │   ├── TestDataBuilder.cs
│   │   └── FileSystemTestHelper.cs
│   ├── Services/ScannerStrategies/
│   ├── Repositories/
│   └── README.md
├── API.Tests/
│   ├── Controllers/
│   └── README.md
├── MANUAL_TEST_CASES.md
├── SCANNER_PROFILE_MIGRATION.md
└── TEST_INFRASTRUCTURE_SUMMARY.md
```

---

## Next Steps

1. ✅ Test infrastructure complete
2. ⬜ Verify PostgreSQL: `dotnet test --filter "TestCategory=Smoke"`
3. ⬜ Begin Phase 1: Database migrations
4. ⬜ Write Phase 2 tests (TDD)
5. ⬜ Follow migration plan step-by-step
