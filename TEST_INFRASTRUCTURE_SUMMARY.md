# Test Infrastructure Setup - Complete

## What Was Created

### 1. Test Projects

#### **Libs.Tests** (NUnit)
- **Location:** `/Libs.Tests/`
- **Target Framework:** .NET 9
- **Dependencies:**
  - NUnit 3 (test framework)
  - Microsoft.EntityFrameworkCore 9.0.8
  - Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4
  - Reference to Libs project

#### **API.Tests** (NUnit)
- **Location:** `/API.Tests/`
- **Target Framework:** .NET 9
- **Dependencies:**
  - NUnit 3 (test framework)
  - Microsoft.AspNetCore.Mvc.Testing 9.0.0 (WebApplicationFactory)
  - Microsoft.EntityFrameworkCore 9.0.8
  - Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4
  - References to API and Libs projects

---

### 2. Test Helpers & Utilities

#### **TestDatabaseFixture.cs**
**Purpose:** Manages PostgreSQL test database lifecycle

**Features:**
- Creates unique test database per test run
- Applies EF Core migrations automatically
- Provides fresh database for each test
- Cleans up database after tests complete

**Connection String:**
```
Host=localhost;Database=ScanLabTest_{GUID};Username=postgres;Password=changeme
```

**Usage:**
```csharp
using var fixture = new TestDatabaseFixture();
var context = fixture.CreateContext();
```

---

#### **TestDataBuilder.cs**
**Purpose:** Factory methods to create test data with sensible defaults

**Available Builders:**
- `CreateTestScanner()` - Scanner with test directories
- `CreateTestCustomer()` - Customer with first/last name
- `CreateTestOrder()` - Order with relationships
- `CreateTestRoll()` - Roll with status

**Commented Out (Until Phase 1):**
- `CreateTestProfile()` - ScannerProfile with HS1800Strategy default
- `CreateTestProfileConfig()` - ProfileConfiguration

**Usage:**
```csharp
var customer = TestDataBuilder.CreateTestCustomer(
    firstName: "John",
    lastName: "Doe"
);

var scanner = TestDataBuilder.CreateTestScanner(
    watchedDir: "/test/watched",
    destinationDir: "/test/dest",
    archiveDir: "/test/archive"
);

var order = TestDataBuilder.CreateTestOrder(
    scanner: scanner,
    customer: customer,
    customerInitials: "JD"
);
```

---

#### **FileSystemTestHelper.cs**
**Purpose:** File system operations for tests (real directories, not mocked)

**Features:**
- `CreateTempTestDirectory()` - Creates temp folder in system temp
- `CreateTestFile()` - Creates file with content
- `CreateTestImages()` - Creates multiple test image files
- `CleanupTestDirectory()` - Safe directory deletion
- `CreateHS1800Structure()` - Creates daily folder structure
- `CreateRollFolder()` - Creates roll folder with images

**Usage:**
```csharp
var testDir = FileSystemTestHelper.CreateTempTestDirectory();
var dailyFolder = FileSystemTestHelper.CreateHS1800Structure(testDir);
var rollDir = FileSystemTestHelper.CreateRollFolder(dailyFolder, "0001234", imageCount: 10);
```

---

#### **TestBase.cs**
**Purpose:** Base classes with automatic setup/teardown

**Three variants:**

1. **TestBase** - Full setup (database + file system)
```csharp
public class MyTests : TestBase
{
    [Test]
    public void MyTest()
    {
        // DbFixture available
        // TestDirectory available
    }
}
```

2. **DatabaseTestBase** - Database only
```csharp
public class RepositoryTests : DatabaseTestBase
{
    // DbFixture available
}
```

3. **FileSystemTestBase** - File system only
```csharp
public class StrategyTests : FileSystemTestBase
{
    // TestDirectory available
}
```

---

### 3. Documentation

#### **Libs.Tests/README.md**
- Prerequisites (PostgreSQL setup)
- Running tests (all, filtered, single)
- Test structure overview
- Guidelines and examples
- Docker PostgreSQL setup instructions

#### **API.Tests/README.md**
- WebApplicationFactory usage
- Authentication in tests
- Controller testing examples

#### **MANUAL_TEST_CASES.md**
- 24 structured test cases
- Covers all 6 phases of migration
- Checkboxes for pass/fail
- Notes sections for observations
- Test summary template

---

## Running Tests

### Prerequisites

1. **PostgreSQL Running:**
```bash
# Option 1: Local PostgreSQL
# Ensure postgres is running with user 'postgres', password 'changeme'

# Option 2: Docker
docker run --name scanlab-test-db \
  -e POSTGRES_PASSWORD=changeme \
  -p 5432:5432 \
  -d postgres:16
```

2. **Verify Connection:**
```bash
psql -h localhost -U postgres -c "SELECT version();"
```

### Running Automated Tests

```bash
# All tests in both projects
dotnet test

# Specific project
dotnet test Libs.Tests/Libs.Tests.csproj
dotnet test API.Tests/API.Tests.csproj

# With verbosity
dotnet test --logger "console;verbosity=detailed"
```

### Running Manual Tests

1. Open `MANUAL_TEST_CASES.md`
2. Follow test cases sequentially
3. Check off pass/fail
4. Document any issues in notes sections

---

## Testing Strategy Per Phase

### **Phase 1: Database Foundation**
**Approach:** Migration verification + unit tests

**Write After Implementation:**
- Test profile seeding
- Test FK relationships
- Test migration idempotency

**Example:**
```csharp
[Test]
public async Task ProfileSeeder_CreatesDefaultProfiles()
{
    using var fixture = new TestDatabaseFixture();
    await ProfileSeeder.SeedProfiles(fixture.Context);

    var profiles = await fixture.Context.ScannerProfiles.ToListAsync();
    Assert.That(profiles.Count, Is.EqualTo(3));
}
```

---

### **Phase 2: Strategy Pattern**
**Approach:** TDD - Write tests FIRST

**Tests to Write:**
- `HS1800StrategyTests` - Path resolution, delay, recursive watching
- `SP500StrategyTests` - Flat structure, manual mode
- `ScannerStrategyFactoryTests` - Validation, instantiation

**Delay Configuration:**
- Use `CompletionDelaySeconds = 1` in tests (not 25)
- Configure via ProfileConfiguration table
- Mutable in Admin app later

**Example:**
```csharp
[Test]
public void ResolveWatchPath_AppendsToday_InCorrectFormat()
{
    var strategy = new HS1800Strategy();
    var scanner = TestDataBuilder.CreateTestScanner(watchedDir: "/test");
    var today = DateTime.Now.ToString("yyyyMMdd");

    var result = strategy.ResolveWatchPath(scanner);

    Assert.That(result, Is.EqualTo($"/test/{today}"));
}
```

---

### **Phase 3: Watcher Service**
**Approach:** Integration tests with real file system + callbacks

**Tests to Write:**
- Start/stop watcher
- Auto-processing callback triggered
- Multiple watchers (multiplexing)
- Error handling

**Important:** Tests will be slower (1-second delay) but more accurate

**Example:**
```csharp
[Test, Timeout(5000)] // 5 second timeout
public async Task WatcherDetectsDirectory_TriggersCallback()
{
    var profile = TestDataBuilder.CreateTestProfile();
    var scanner = TestDataBuilder.CreateTestScanner(TestDirectory, profile);
    var roll = TestDataBuilder.CreateTestRoll();

    bool callbackCalled = false;
    var service = new FileSystemWatcherService();
    service.OnAutoProcessRoll = async (rollId, staffId) =>
    {
        callbackCalled = true;
    };

    service.StartWatcherForRoll(roll, Guid.NewGuid());

    // Create directory
    var dailyPath = FileSystemTestHelper.CreateHS1800Structure(TestDirectory);
    var rollDir = FileSystemTestHelper.CreateRollFolder(dailyPath, "0001234");

    await Task.Delay(1500); // Wait for 1 sec delay + buffer

    Assert.That(callbackCalled, Is.True);
}
```

---

### **Phase 4-5: Repositories & API**
**Approach:** Integration tests with test database

**Repository Tests:**
```csharp
public class ProfileRepositoryTests : DatabaseTestBase
{
    [Test]
    public async Task AddProfile_WithValidStrategy_Succeeds()
    {
        var repository = new ProfileRepository(DbFixture.Context);
        var profile = TestDataBuilder.CreateTestProfile();

        var result = await repository.AddProfile(profile);

        Assert.That(result.IsSuccess, Is.True);
    }
}
```

**API Controller Tests:**
```csharp
public class ScannerProfileControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ScannerProfileControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Test]
    public async Task GetProfiles_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/ScannerProfile/profiles");
        Assert.That(response.IsSuccessStatusCode, Is.True);
    }
}
```

---

### **Phase 6-7: UI (Client/Admin Apps)**
**Approach:** Manual testing + ViewModel unit tests

**ViewModel Tests:**
```csharp
[Test]
public async Task LoadProfiles_PopulatesCollection()
{
    var mockApiService = CreateMockApiService();
    var vm = new ProfileManagementViewModel(mockApiService);

    await Task.Delay(500); // Allow async load

    Assert.That(vm.Profiles.Count, Is.GreaterThan(0));
}
```

**Manual Tests:** Use MANUAL_TEST_CASES.md

---

## Important Testing Notes

### 1. Real File System (Not Mocked)
**Decision:** Per your preference, we use real temp directories

**Pros:**
- Tests actual file system behavior
- Catches permission issues
- More confidence in production

**Cons:**
- Slightly slower
- Potential for leftover temp files (cleanup handles this)

---

### 2. Real Time Delays (Not Mocked)
**Decision:** Tests actually wait 1 second for HS-1800 strategy

**Configuration:**
- HS-1800 production: 25 seconds
- HS-1800 testing: 1 second (configured in ProfileConfiguration)
- Updated via Admin app

**Test Example:**
```csharp
[Test]
public async Task ShouldAutoProcess_Waits1Second()
{
    var strategy = new HS1800Strategy(); // Uses 1 sec from config
    var stopwatch = Stopwatch.StartNew();

    await strategy.ShouldAutoProcess(scanner, testDir);

    stopwatch.Stop();
    Assert.That(stopwatch.Elapsed.TotalSeconds, Is.InRange(0.9, 1.2));
}
```

---

### 3. Test PostgreSQL Database
**Decision:** Per your preference, use real PostgreSQL (not in-memory)

**Benefits:**
- Exact production environment
- Tests migrations accurately
- Catches PostgreSQL-specific issues

**Setup:**
Each test gets:
1. Unique database (e.g., `ScanLabTest_a3f2b1c4...`)
2. Fresh schema via `EnsureCreated()`
3. Automatic cleanup via `EnsureDeleted()`

---

### 4. Test Execution Speed
**Expected Times:**
- Unit tests (no DB/FS): <100ms each
- Database tests: ~200-500ms each
- File system tests: ~100-300ms each
- Watcher integration tests: ~1-3 seconds each (1 sec delay)

**Total Suite Estimate:**
- Fast tests (~50): ~10 seconds
- Integration tests (~30): ~30 seconds
- Slow tests (~10 watchers): ~20 seconds
- **Total: ~60 seconds for full suite**

---

## Next Steps

### Before Phase 1 Implementation:
1. ✅ Test projects created
2. ✅ Helper utilities created
3. ✅ Documentation written
4. ⬜ **Verify PostgreSQL connection:**
```bash
dotnet test Libs.Tests --filter "TestCategory=Smoke"
```

### During Each Phase:
1. Write tests (TDD for Phases 2-5)
2. Implement code
3. Run tests: `dotnet test`
4. Fix failures
5. Move to next phase

### After All Phases:
1. Run full suite: `dotnet test`
2. Execute manual test cases
3. Review coverage
4. Document any test failures

---

## Test Coverage Goals

### Minimum Coverage:
- [ ] All strategy methods tested
- [ ] All repository CRUD operations tested
- [ ] All API endpoints tested
- [ ] Happy path + error cases
- [ ] Edge cases (concurrent, missing files, etc.)

### Nice to Have:
- [ ] Performance tests (watcher under load)
- [ ] Stress tests (many concurrent watchers)
- [ ] End-to-end tests (full workflow)

---

## Troubleshooting

### PostgreSQL Connection Failed
```bash
# Check if running
docker ps | grep scanlab-test-db

# Check connection
psql -h localhost -U postgres -c "SELECT 1;"

# Restart container
docker restart scanlab-test-db
```

### Tests Hang
- Check for orphaned watchers (restart API)
- Check for locked temp directories
- Increase test timeout: `[Test, Timeout(10000)]`

### Database Lock Errors
- Ensure only one test runs at a time (NUnit default)
- Check for unclosed DbContext instances

---

## Summary

**Test Infrastructure Status:** ✅ Complete

**What's Ready:**
- 2 test projects (Libs.Tests, API.Tests)
- 4 helper classes (Database, DataBuilder, FileSystem, TestBase)
- 2 README files with usage examples
- 24 manual test cases
- Clear testing strategy per phase

**What to Do Next:**
1. Verify PostgreSQL connection
2. Begin Phase 1 implementation
3. Write Phase 2 tests (TDD)
4. Follow migration plan: `SCANNER_PROFILE_MIGRATION.md`

**Estimated Implementation Time:** 27-37 hours
**Test Writing Time:** ~8-12 hours (included in phase estimates)

---

## Current Status (2025-11-29)

### ✅ Completed
- [x] Test infrastructure fully created
- [x] Both test projects compile successfully (Libs.Tests, API.Tests)
- [x] All helper classes implemented
- [x] Documentation complete
- [x] Fixed model mapping issues:
  - Customer uses FirstName/LastName (not CustomerInitials)
  - CustomerInitials is on Order model
  - Commented out ScannerProfile/ProfileConfiguration methods until Phase 1

### ⏳ Ready for Next Steps

**Before Phase 1:**
1. Start PostgreSQL:
   ```bash
   docker run --name scanlab-test-db \
     -e POSTGRES_PASSWORD=changeme \
     -p 5432:5432 \
     -d postgres:16
   ```

2. Verify test infrastructure with smoke tests:
   ```bash
   dotnet test --filter "TestCategory=Smoke"
   ```

**When Ready for Phase 1:**
1. Follow SCANNER_PROFILE_MIGRATION.md Phase 1 steps
2. Create ScannerProfile and ProfileConfiguration models
3. Run migrations
4. Uncomment profile-related methods in TestDataBuilder.cs (lines 14-47)
5. Update SmokeTests.cs to test profile functionality (optional)
