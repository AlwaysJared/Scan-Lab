# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Scan Lab is a film scanning management system for Coastal Film Lab consisting of four interconnected .NET applications:

- **API** (.NET 8): REST API backend serving order, roll, and scanner management endpoints
- **Libs** (.NET 8): Shared library containing data models, Entity Framework Core context, repositories, helpers, and services
- **Client** (.NET 9 Avalonia): Desktop application for scan technicians to process orders and manage scanning workflow
- **Admin** (.NET 9 Avalonia): Administrative desktop application for system configuration

The system tracks film scanning orders through a workflow where orders contain rolls, and scan technicians process rolls using designated scanners. The API uses PostgreSQL for data persistence and manages file system monitoring for scanned image collection.

## Build and Run Commands

### Building Projects

Build the entire solution from the root:
```bash
dotnet build "Scan Lab.sln"
```

Build individual projects:
```bash
dotnet build API/API.csproj
dotnet build Libs/Libs.csproj
dotnet build Client/Client.csproj
dotnet build Admin/Admin.csproj
```

### Running the API

Development mode:
```bash
cd API
dotnet run
```

The API runs on `http://0.0.0.0:3624` (configured in API/Program.cs:7).

### Running Desktop Applications

```bash
cd Client
dotnet run

cd Admin
dotnet run
```

### Database Migrations

Run migrations from the root directory:
```bash
dotnet ef database update --project Libs --startup-project API
```

Create new migration:
```bash
dotnet ef migrations add MigrationName --project Libs --startup-project API
```

### Publishing

Use PowerShell scripts in the Scripts directory:

API (self-contained with database migration):
```powershell
cd Scripts
.\Publish-API.ps1
```

Client (builds for Windows and Linux):
```powershell
cd Scripts
.\Publish-Client.ps1
```

Admin:
```powershell
cd Scripts
.\Publish-Admin.ps1
```

## Architecture

### Data Layer (Libs)

**Database Context**: `Libs/Data/Context/ScanLabContext.cs` - Entity Framework Core context for PostgreSQL

**Models** (Libs/Data/Models/):
- `Order`: Contains OrderId (string, unique), Customer, Scanner, list of Rolls, OrderStatus, timestamps
- `Roll`: Belongs to Order via OrderId FK, has RollNumber, RollStatus, FilmType
- `Scanner`: Represents physical film scanner with export directory path
- `Customer`: Basic customer information with initials

**Repositories** (Libs/Repositories/): Implement data access patterns
- `OrderRepository`: CRUD operations for orders
- `RollRepository`: Roll management including status updates
- `ScannerRepository`: Scanner configuration and retrieval

**Helpers** (Libs/Helpers/):
- `IOHelpers.NetworkPathConverter`: Converts between Linux GVFS paths (AFP/SMB) and Windows UNC paths for cross-platform network directory access
- `ImageFileHelpers`: Image processing utilities using ImageSharp
- `DateTimeHelpers`: Timestamp utilities

**Services** (Libs/Services/):
- `FileSystemWatcherService`: Singleton service managing file watchers for scanner export directories

### API Layer

REST API using ASP.NET Core with controllers in API/Controllers/:
- `OrderController`: Order CRUD endpoints
- `RollController`: Roll management and status updates
- `ScannerController`: Scanner configuration

Configuration (API/appsettings.json):
- PostgreSQL connection string: `ScanLabDBConnection`
- Default credentials are placeholder values (`postgres/changeme`)

### Client Applications

Both Client and Admin are Avalonia desktop applications using MVVM pattern:

**Client Structure**:
- ViewModels: `DashboardViewModel`, `OrderFormViewModel`, `SettingsViewModel`, etc.
- Services: `ApiService` (HTTP client for API), `ScannerService` (scanner interaction)
- Uses CommunityToolkit.Mvvm and ReactiveUI for reactive bindings

**Admin Structure**:
- Similar MVVM pattern for administrative tasks
- Uses CommunityToolkit.Mvvm

### Order Processing Workflow

From notes.txt:

1. Order created through Client (or Admin) containing Scanner, Customer, Order number, and Rolls
2. Order added to queue on Dashboard
3. Scan tech marks roll as "scanning in progress" (sets parent order to "in progress")
4. Tech scans roll and exports to scanner's directory
5. Tech marks roll as "scanning complete"
6. System checks if all rolls in order are complete
7. If complete, prompts to finalize order; otherwise remains "in progress"

### Database Migration History

The system migrated from SQLite to PostgreSQL (see commit 535c22b). Commented-out SQLite code exists in API/Program.cs:11-12. Entity Framework Core 9 with Npgsql provider is now used.

## Key Technical Details

- **Target Frameworks**: API/Libs use .NET 8, Client/Admin use .NET 9
- **UI Framework**: Avalonia 11.2.1 with Fluent theme
- **ORM**: Entity Framework Core 9.0.7 with Npgsql 9.0.4
- **Image Processing**: ImageSharp 3.1.6
- **Publishing**: All projects configured for self-contained deployment
- **Runtime Identifiers**: Client/Admin support win-x64 and linux-x64

## Important Notes

- Roll numbers are NOT unique globally (index removed in migration 20250719125701) - they can repeat across different orders
- Order IDs (OrderId) serve as the primary key and must be unique
- The API's System.Text.Json package (9.0.7) is explicitly referenced to prevent IIS deployment issues (see commit 2d477d9)
- Network path resolution handles spaces in directory names via URL decoding (IOHelpers.NetworkPathConverter.ResolvePath)
- FileSystemWatcherService uses session-based management with ConcurrentDictionary for thread-safe operations
- ImageSharp updated to 3.1.12 to fix security vulnerabilities (NU1903/NU1902)

---

## Scanner Profile System

The Scanner Profile System provides a flexible, extensible architecture for supporting multiple scanner models with different file organization behaviors and completion detection methods.

### Overview

Different scanner models organize files differently and require unique handling:
- **Noritsu-based scanners** (HS-1800, LS-600): Use daily folders, auto-detect completion via time-based delays
- **Pakon scanners** (SP-500, SP-3000): Manual export process, no auto-detection

The Scanner Profile System uses the **Strategy Pattern** to encapsulate scanner-specific behaviors while maintaining a consistent interface.

---

### Phase 1: Database Foundation

**Implemented:** Database schema and data seeding for scanner profiles

#### New Database Models

**ScannerProfile** (`Libs/Data/Models/ScannerProfile.cs`):
```csharp
public class ScannerProfile
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public required string ProfileName { get; set; }

    [Required]
    public required string StrategyClassName { get; set; }

    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime? DateUpdated { get; set; }
}
```

**ProfileConfiguration** (`Libs/Data/Models/ProfileConfiguration.cs`):
```csharp
public class ProfileConfiguration
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProfileId { get; set; }

    public ScannerProfile? Profile { get; set; }

    [Required]
    public required string ConfigKey { get; set; }

    [Required]
    public required string ConfigValue { get; set; }

    public string? Description { get; set; }
}
```

**Scanner Model Updates** (`Libs/Data/Models/Scanner.cs`):
```csharp
// Added scanner profile relationship
public Guid? ProfileId { get; set; }
public ScannerProfile? Profile { get; set; }

// Added per-scanner delay configuration (Phase 3)
public int? AutoProcessDelaySeconds { get; set; }
```

#### Database Context Changes

**ScanLabContext** (`Libs/Data/Context/ScanLabContext.cs`):
```csharp
public DbSet<ScannerProfile> ScannerProfiles { get; set; }
public DbSet<ProfileConfiguration> ProfileConfigurations { get; set; }

// Relationships configured in OnModelCreating:
// - Scanner -> Profile (SetNull on delete)
// - Profile -> Configurations (Cascade on delete)
```

#### Profile Seeding

**ProfileSeeder** (`Libs/Data/SeedData/ProfileSeeder.cs`):

Seeds three default profiles on application startup:

1. **Noritsu Controller Auto**
   - Strategy: `NoritsuControllerStrategy`
   - For: HS-1800, LS-600, and other Noritsu Controller-based scanners
   - Auto-processing with daily folder structure
   - Default configurations:
     - `CompletionDelaySeconds`: 25
     - `DirectoryPattern`: `{WatchedDir}/{YYYYMMDD}/*`

2. **SP-500 Manual**
   - Strategy: `SP500Strategy`
   - For: Fujifilm Frontier SP-500 scanners
   - Manual processing only

3. **SP-3000 Manual**
   - Strategy: `SP3000Strategy`
   - For: Fujifilm Frontier SP-3000 scanners
   - Manual processing only

**API Startup Integration** (`API/Program.cs`):
```csharp
// Seed scanner profiles on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ScanLabContext>();
    await Libs.Data.SeedData.ProfileSeeder.SeedProfiles(context);
}
```

#### Migrations
- `20251219165108_AddScannerProfiles` - Added ScannerProfiles and ProfileConfigurations tables
- `20251223212012_AddAutoProcessDelayToScanner` - Added AutoProcessDelaySeconds to Scanners table

---

### Phase 2: Strategy Pattern Implementation

**Implemented:** Scanner-specific behavior strategies with factory pattern

#### Completion Detection Modes

**CompletionDetectionMode** (`Libs/Enums/CompletionDetectionMode.cs`):
```csharp
public enum CompletionDetectionMode
{
    Manual,           // SP-500/SP-3000 - Tech manually marks complete
    TimeBasedDelay,   // Noritsu scanners - Wait X seconds after last file
    ExitFile          // Future - Watch for specific completion file
}
```

#### Strategy Interface

**IScannerStrategy** (`Libs/Services/ScannerStrategies/IScannerStrategy.cs`):
```csharp
public interface IScannerStrategy
{
    // Path resolution for file watching
    string ResolveWatchPath(Scanner scanner);

    // Should watcher watch subdirectories?
    bool IsRecursive { get; }

    // How to detect completion
    CompletionDetectionMode CompletionMode { get; }

    // Optional delay in seconds (null = use scanner instance value)
    int? CompletionDelaySeconds { get; }

    // Get latest roll directory (strategy-specific logic)
    Task<string?> GetLatestRollDirectory(Scanner scanner);

    // Determine if auto-processing should trigger
    Task<bool> ShouldAutoProcess(Scanner scanner, string directoryPath);
}
```

#### Strategy Implementations

**NoritsuControllerStrategy** (`Libs/Services/ScannerStrategies/NoritsuControllerStrategy.cs`):
- **ResolveWatchPath**: Creates daily folder `{WatchedDir}/YYYYMMDD`
- **IsRecursive**: `true` - watches subdirectories within daily folder
- **CompletionMode**: `TimeBasedDelay`
- **Behavior**: Smart file watching - timer resets on each new file, processes when timer expires (no new files for X seconds)
- **Use Case**: HS-1800, LS-600 scanners with Noritsu Controller software

**SP500Strategy** (`Libs/Services/ScannerStrategies/SP500Strategy.cs`):
- **ResolveWatchPath**: Returns `scanner.WatchedDir` directly
- **IsRecursive**: `false` - flat directory structure
- **CompletionMode**: `Manual`
- **Behavior**: No auto-processing, tech manually exports and marks complete
- **Use Case**: Fujifilm Frontier SP-500 scanners

**SP3000Strategy** (`Libs/Services/ScannerStrategies/SP3000Strategy.cs`):
- Inherits all behavior from `SP500Strategy`
- Identical manual processing workflow
- **Use Case**: Fujifilm Frontier SP-3000 scanners

#### Strategy Factory

**ScannerStrategyFactory** (`Libs/Services/ScannerStrategies/ScannerStrategyFactory.cs`):

Hardcoded registry mapping strategy class names to types:

```csharp
public static readonly Dictionary<string, Type> StrategyRegistry = new()
{
    { "NoritsuControllerStrategy", typeof(NoritsuControllerStrategy) },
    { "SP500Strategy", typeof(SP500Strategy) },
    { "SP3000Strategy", typeof(SP3000Strategy) },
    { "SP500AutoStrategy", typeof(SP500AutoStrategy) }
};

public static IScannerStrategy? CreateStrategy(Scanner scanner);
public static IScannerStrategy? CreateStrategy(string strategyClassName);
public static bool IsValidStrategy(string className);
public static List<string> GetAvailableStrategies();
```

**How Strategy Selection Works:**
1. Scanner has `ProfileId` → lookup `ScannerProfile`
2. `ScannerProfile.StrategyClassName` (string) → lookup in `StrategyRegistry`
3. `Activator.CreateInstance()` → instantiate strategy type
4. Return as `IScannerStrategy` interface

**Adding New Strategies:**
1. Create new class implementing `IScannerStrategy`
2. Add to `StrategyRegistry` dictionary
3. Create corresponding `ScannerProfile` in database
4. No interface changes required!

#### Repository Integration

**RollRepository** (`Libs/Repositories/RollRepository.cs`):

**Before (Hardcoded):**
```csharp
var rollDirsSorted = Directory.GetDirectories(roll.Order.Scanner.WatchedDir)
    .Select(dir => new { Path = dir, WriteTime = Directory.GetLastWriteTimeUtc(dir) })
    .OrderByDescending(dir => dir.WriteTime)
    .ToList();
```

**After (Strategy Pattern):**
```csharp
var strategy = ScannerStrategyFactory.CreateStrategy(roll.Order.Scanner);
if (strategy == null)
    return new SystemResponse { IsSuccess = false, Message = "Scanner profile not configured" };

var latestRollDir = await strategy.GetLatestRollDirectory(roll.Order.Scanner);
if (string.IsNullOrEmpty(latestRollDir))
    return new SystemResponse { IsSuccess = false, Message = "No roll directories found" };
```

---

### Phase 3: Enhanced Watcher Service & Null Safety

**Implemented:** Session-based file watching with smart completion detection and comprehensive null safety

#### WatcherSession Model

**WatcherSession** (`Libs/Services/WatcherSession.cs`):

Represents an active file system watcher session for a specific roll:

```csharp
public class WatcherSession
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public Guid RollId { get; set; }
    public required Roll Roll { get; set; }
    public required Scanner Scanner { get; set; }
    public required IScannerStrategy Strategy { get; set; }
    public required FileSystemWatcher Watcher { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public CancellationTokenSource? CancellationToken { get; set; }

    // For Noritsu scanners: nested file watcher and timer
    public FileSystemWatcher? InnerFileWatcher { get; set; }
    public System.Timers.Timer? CompletionTimer { get; set; }
}
```

#### FileSystemWatcherService Rewrite

**Complete Rewrite** (`Libs/Services/FileSystemWatcherService.cs`):

**Key Features:**
- **Session-based management** using `ConcurrentDictionary<Guid, WatcherSession>`
- **Scanner conflict prevention** - Only one watcher per scanner at a time
- **Smart file watching** for Noritsu scanners:
  - Outer watcher monitors for directory creation
  - Inner watcher monitors files within directory
  - Timer resets on each new file creation
  - Auto-processes when timer expires (no new files for X seconds)
- **Proper disposal** of nested watchers and timers
- **Error handling** - Stops watcher on error, requires manual completion

**Architecture:**

```csharp
public class FileSystemWatcherService : IDisposable
{
    private readonly ConcurrentDictionary<Guid, WatcherSession> _activeSessions = new();

    public delegate Task ProcessRollDelegate(Guid rollId, Guid? staffId);
    public ProcessRollDelegate? OnAutoProcessRoll { get; set; }

    // Start watcher for specific roll
    public Guid? StartWatcherForRoll(Roll roll, Guid? staffId);

    // Stop watcher by roll ID or session ID
    public bool StopWatcherForRoll(Guid rollId);
    public bool StopWatcher(Guid sessionId);

    // Query active sessions
    public IEnumerable<WatcherSession> GetActiveSessions();
    public bool IsWatcherActive(Guid rollId);
}
```

**Smart File Watching for Noritsu Scanners:**

```csharp
private async Task HandleNoritsuDirectory(WatcherSession session, string directoryPath, Guid? staffId)
{
    var delay = session.Scanner.AutoProcessDelaySeconds ?? 25;

    // Create inner file watcher for files inside directory
    var fileWatcher = new FileSystemWatcher(directoryPath)
    {
        NotifyFilter = NotifyFilters.FileName,
        IncludeSubdirectories = false
    };

    // Create timer (resets on each new file)
    var timer = new System.Timers.Timer(delay * 1000);

    // Store in session for cleanup
    session.InnerFileWatcher = fileWatcher;
    session.CompletionTimer = timer;

    // Reset timer on each new file
    fileWatcher.Created += (sender, e) =>
    {
        fileCount++;
        timer.Stop();
        timer.Start();
    };

    // When timer expires (no new files for X seconds)
    timer.Elapsed += async (sender, e) =>
    {
        timer.Stop();
        fileWatcher.EnableRaisingEvents = false;

        if (Directory.Exists(directoryPath) && Directory.GetFiles(directoryPath).Length > 0)
        {
            if (OnAutoProcessRoll != null)
                await OnAutoProcessRoll(session.RollId, staffId);
        }

        StopWatcher(session.SessionId);
    };

    fileWatcher.EnableRaisingEvents = true;
    timer.Start();
}
```

**Example Workflow:**
1. Directory `20260106/12345/` created by Noritsu scanner
2. Outer watcher detects directory → calls `HandleNoritsuDirectory()`
3. Inner file watcher created, monitoring `20260106/12345/`
4. Timer starts (25 seconds)
5. File `IMG001.jpg` created → timer resets to 25 seconds
6. File `IMG002.jpg` created → timer resets again
7. ... (continues for each file)
8. No new files for 25 seconds → timer expires
9. Auto-process triggered, watcher stopped

#### Service Registration Change

**API/Program.cs:**
```csharp
// Changed from Singleton to Scoped
builder.Services.AddScoped<FileSystemWatcherService>();
```

**Reasoning:** Scoped lifetime is more appropriate for session-based architecture and prevents state sharing issues between requests.

#### Null Safety Improvements

**All Nullable Reference Warnings Fixed:**

Applied `required` modifier to non-nullable model properties:
- `WatcherSession`: Roll, Scanner, Strategy, Watcher
- `ScannerProfile`: ProfileName, StrategyClassName
- `ProfileConfiguration`: ConfigKey, ConfigValue
- `Order`: OrderId
- `Customer`: FirstName, LastName
- `EmailConfig`: Email, Password
- `AnalyticsBaseDTO`: Id, Name

Made nullable fields explicitly nullable:
- `GmailService`: `_fromEmail`, `_appPassword`

**Build Result:** 0 Errors, 0 Warnings

---

### Testing (Phase 8)

**42 tests** across 4 test fixtures, all passing:

- **SmokeTests** (5 tests) — DB connection, EF Core CRUD, file system, TestDataBuilder
- **StrategyFactoryTests** (14 tests) — `IsValidStrategy`, `GetAvailableStrategies`, `CreateStrategy` by name/scanner
- **StrategyPropertyTests** (10 tests) — CompletionMode, IsRecursive, ShouldAutoProcess for all 4 strategies
- **StrategyFileSystemTests** (7 tests) — ResolveWatchPath, GetLatestRollDirectory with temp directories
- **ProfileRepositoryTests** (5 tests) — Profile/config CRUD, scanner relationship, cascade delete

**Test Categories:** `[Category("Smoke")]`, `[Category("Unit")]`, `[Category("FileSystem")]`, `[Category("Integration")]`

**Test Infrastructure** (`Libs.Tests/Helpers/`):

- `TestBase` / `DatabaseTestBase` / `FileSystemTestBase` — base classes for different test needs
- `TestDatabaseFixture` — creates isolated PostgreSQL test database per test
- `FileSystemTestHelper` — temp directory creation, test files, HS1800 structure helpers
- `TestDataBuilder` — factory methods for Scanner, Customer, Order, Roll, ScannerProfile, ProfileConfiguration

---

### Phase 3.1: SP-500 Auto-Export Integration

**Implemented:** Ported SP500Exporter functionality into Scan-Lab as a scanner profile strategy.

**SP500AutoStrategy** (`Libs/Services/ScannerStrategies/SP500AutoStrategy.cs`):

- Extends `SP500Strategy`, overrides `CompletionMode` to `ExitFile`
- Auto-processing handled by `SP500ExporterService`, not FileSystemWatcherService

**SP500ExporterService** (`Libs/Services/SP500ExporterService.cs`):

- Polling-based monitoring (3-second intervals) of Frontier Software exports
- Roll detection via `{roll}-1-4`, `{roll}-Ac_ImgConv`, `{roll}-1-1` directories
- Exit file: `CdOrder.INF` in `-1-4` subdirectory signals completion
- Copies `H*.jpg` from `-Ac_ImgConv` subdirs to target directory
- 90-minute timeout, session-based (one export at a time)

**Profile Seeding** — adds "SP-500 Auto" profile with `SP500AutoStrategy`

**Client Dashboard Integration:**

- Start/Stop Export buttons bound to `Scanner.Profile.StrategyClassName == "SP500AutoStrategy"`
- `RollActionVisibilityMultiConverter`: hides manual buttons for SP500Auto scanners, shows export buttons
- Active Exports panel with real-time status polling

---

### Phase 5: API Layer Updates

**ScannerProfileController** (`API/Controllers/ScannerProfileController.cs`):

- `GET /api/ScannerProfile/profiles` — list all active profiles
- `GET /api/ScannerProfile/profile/{id}` — get single profile
- `POST /api/ScannerProfile/add` — create profile (validates strategy class name)
- `PUT /api/ScannerProfile/update` — update profile
- `DELETE /api/ScannerProfile/delete/{id}` — soft-delete (blocked if scanners use it)
- `GET /api/ScannerProfile/strategies` — list available strategy class names
- `GET /api/ScannerProfile/profile/{id}/configurations` — get profile configs

**ProfileRepository** (`Libs/Repositories/ProfileRepository.cs`):

- Full CRUD with strategy validation via `ScannerStrategyFactory.IsValidStrategy()`
- Soft delete (sets `IsActive = false`) with in-use protection

**RollController** — automation lifecycle endpoints:

- `POST /api/Roll/startExport` — starts SP500 auto-export for a roll
- `POST /api/Roll/stopExport` — stops active export
- `GET /api/Roll/activeExports` — lists active export sessions

---

### Phase 6: Admin App Updates

**Scanner Profiles Management** (`Admin/Views/Scanner Profiles/ScannerProfiles.axaml`):

- Tab-based CRUD: DataGrid list + Add/Edit form
- Profile Name, Strategy (dropdown from API), Description fields
- Edit/Delete buttons in DataGrid

**Navigation** — added "Scanner Profiles" button to sidebar in `Admin/Views/MainWindow.axaml`

---

### Phase 7: Client App Updates

**Settings** — read-only "Scanner Profile" field in Scanner Configuration expander showing assigned profile name (with "No profile assigned" fallback)

**Dashboard** — SP500 auto-export UI (completed in Phase 3.1)

---

### Strategy Pattern Usage Examples

#### Example 1: Get Latest Roll Directory

```csharp
var scanner = await context.Scanners
    .Include(s => s.Profile)
    .FirstOrDefaultAsync(s => s.Id == scannerId);

var strategy = ScannerStrategyFactory.CreateStrategy(scanner);
if (strategy != null)
{
    string? latestRollDir = await strategy.GetLatestRollDirectory(scanner);
    // latestRollDir = "/path/to/20260106/12345/" for Noritsu
    // latestRollDir = "/path/to/IMG_EXPORT/" for SP-500
}
```

#### Example 2: Check If Should Auto-Process

```csharp
var strategy = ScannerStrategyFactory.CreateStrategy(scanner);
if (strategy != null && strategy.CompletionMode == CompletionDetectionMode.TimeBasedDelay)
{
    bool shouldProcess = await strategy.ShouldAutoProcess(scanner, directoryPath);
    if (shouldProcess)
    {
        // Trigger auto-processing
    }
}
```

#### Example 3: Start File Watcher

```csharp
var sessionId = fileSystemWatcherService.StartWatcherForRoll(roll, staffId);
if (sessionId.HasValue)
{
    // Watcher started successfully
    Console.WriteLine($"Watcher started for roll {roll.RollId}: {sessionId}");
}
else
{
    // Failed to start (scanner already has watcher, or other error)
}
```

#### Example 4: Stop File Watcher

```csharp
bool stopped = fileSystemWatcherService.StopWatcherForRoll(rollId);
if (stopped)
{
    // Watcher stopped successfully
}
```

---

### Adding a New Scanner Type

**Scenario:** Adding support for Pakon F-135 scanners

**Step 1:** Create Strategy Class

```csharp
// Libs/Services/ScannerStrategies/F135Strategy.cs
public class F135Strategy : IScannerStrategy
{
    public string ResolveWatchPath(Scanner scanner)
    {
        // F-135 specific path resolution
        return Path.Combine(scanner.WatchedDir, "F135_OUTPUT");
    }

    public bool IsRecursive => true;

    public CompletionDetectionMode CompletionMode => CompletionDetectionMode.ExitFile;

    public int? CompletionDelaySeconds => null;

    public async Task<string?> GetLatestRollDirectory(Scanner scanner)
    {
        // F-135 specific logic
        var watchPath = ResolveWatchPath(scanner);
        // ... implementation
    }

    public async Task<bool> ShouldAutoProcess(Scanner scanner, string directoryPath)
    {
        // Check for F-135 completion file
        return File.Exists(Path.Combine(directoryPath, "COMPLETE.TXT"));
    }
}
```

**Step 2:** Register in Factory

```csharp
// Libs/Services/ScannerStrategies/ScannerStrategyFactory.cs
public static readonly Dictionary<string, Type> StrategyRegistry = new()
{
    { "NoritsuControllerStrategy", typeof(NoritsuControllerStrategy) },
    { "SP500Strategy", typeof(SP500Strategy) },
    { "SP3000Strategy", typeof(SP3000Strategy) },
    { "F135Strategy", typeof(F135Strategy) }  // ← Add new strategy
};
```

**Step 3:** Create Scanner Profile

```csharp
// Add to ProfileSeeder or via Admin UI
new ScannerProfile
{
    ProfileName = "Pakon F-135 Auto",
    StrategyClassName = "F135Strategy",
    Description = "Automatic processing for Pakon F-135 scanners with exit file detection"
}
```

**Step 4:** Assign to Scanner

```csharp
scanner.ProfileId = f135Profile.Id;
await context.SaveChangesAsync();
```

**Done!** The scanner now uses F-135 strategy automatically throughout the system.

---

### Troubleshooting Scanner Profiles

#### Issue: "Scanner profile not configured"
**Cause:** Scanner has no ProfileId or ProfileId points to non-existent profile
**Solution:**
1. Check scanner configuration: `SELECT * FROM "Scanners" WHERE "ProfileId" IS NULL`
2. Assign profile via Admin UI or SQL:
   ```sql
   UPDATE "Scanners" SET "ProfileId" = '<profile-guid>' WHERE "Id" = '<scanner-guid>'
   ```

#### Issue: "Invalid strategy for scanner"
**Cause:** StrategyClassName doesn't exist in StrategyRegistry
**Solution:**
1. Verify StrategyClassName: `SELECT "StrategyClassName" FROM "ScannerProfiles"`
2. Check StrategyRegistry in `ScannerStrategyFactory.cs`
3. Ensure strategy class is compiled and registered

#### Issue: File watcher not starting
**Cause:** Scanner already has active watcher
**Solution:**
1. Check active sessions: `fileSystemWatcherService.GetActiveSessions()`
2. Stop existing watcher: `fileSystemWatcherService.StopWatcherForRoll(rollId)`
3. Or wait for current roll to complete

#### Issue: Smart file watching not triggering
**Cause:** Timer configuration or file permission issues
**Solution:**
1. Check `Scanner.AutoProcessDelaySeconds` value
2. Verify directory permissions for file monitoring
3. Check logs for file creation events
4. Ensure Noritsu scanner is creating files in expected location

---

### Performance Considerations

**Database Queries:**
- Scanner profile loaded via `Include(s => s.Profile)` - single query with join
- Profile configurations lazy-loaded as needed
- Strategy instantiation uses `Activator.CreateInstance()` - minimal overhead

**File Watching:**
- One FileSystemWatcher per active session (not per scanner)
- Noritsu scanners use nested watchers (outer + inner)
- ConcurrentDictionary provides thread-safe session management
- All watchers properly disposed to prevent memory leaks

**Recommended Limits:**
- Maximum concurrent active sessions: Unlimited (but one per scanner in practice)
- File watcher timeout: 90 minutes default (configurable per scanner)
- Poll interval: 3 seconds (Noritsu inner watcher)

---

## Current Development Status

All phases of the Scanner Profile Migration are complete.

- **Phase 1:** Database foundation (ScannerProfile, ProfileConfiguration models, seeding)
- **Phase 2:** Strategy pattern (IScannerStrategy, factory, 4 strategy implementations)
- **Phase 3:** Enhanced watcher service (session-based, smart file watching, null safety)
- **Phase 3.1:** SP-500 auto-export (SP500ExporterService, SP500AutoStrategy, Client dashboard UI)
- **Phase 4:** Repository updates (ProfileRepository, RollRepository strategy integration)
- **Phase 5:** API layer (ScannerProfileController CRUD, RollController export lifecycle)
- **Phase 6:** Admin app (Scanner Profiles management page, sidebar navigation)
- **Phase 7:** Client app (Settings profile display, Dashboard export buttons)
- **Phase 8:** Testing (42 tests: unit, file system, DB integration)
- **Phase 9:** Documentation and cleanup
