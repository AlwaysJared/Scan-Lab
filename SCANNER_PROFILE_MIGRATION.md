# Scanner Profile System - Migration Plan

## Overview
This document outlines the complete migration plan for implementing a scanner profile system in Scan-Lab. The system will support different scanner models (HS-1800, SP-500, SP-3000) with unique file organization behaviors through a flexible strategy pattern architecture.

---

## Current Code Structure Analysis

### Database Layer (Libs/Data)

**Models:**
- `Scanner` (Libs/Data/Models/Scanner.cs)
  - Properties: Id, ScannerName, Make, Model, WatchedDir, DestinationDir, ArchiveDir, ArtistName
  - **No profile relationship currently exists**

- `Roll` (Libs/Data/Models/Roll.cs)
  - Properties: RollId, RollNumber, ImageCount, FilmType, RollNotes, OrderId, Order, Status, DateCreated, DateUpdated, CreatedBy, UpdatedBy
  - Status enum: Created, ScanningInProgress, ScanningPaused, ScanningCompleted, Processing, Processed

- `Order` (Libs/Data/Models/Order.cs)
  - Relationships: Has many Rolls (cascade delete), references Scanner and Customer

**Context:**
- `ScanLabContext` (Libs/Data/Context/ScanLabContext.cs)
  - Inherits from IdentityDbContext for staff authentication
  - DbSets: Orders, Scanners, Rolls, Customers, Staff, ConfigSettings

### Repository Layer (Libs/Repositories)

**RollRepository** (Libs/Repositories/RollRepository.cs):
- `ProcessRoll` (lines 74-283): Core file processing logic
  - **Currently hardcoded directory discovery** (lines 112-122):
    - Gets all directories in WatchedDir
    - Sorts by LastWriteTimeUtc descending
    - Selects the newest directory
  - Creates destination folder structure with weekly intervals
  - Renames files: `{CustomerInitials}-{OrderId}-{RollNumber}-{SequenceNumber}.ext`
  - Handles .bmp to .tiff conversion
  - Updates EXIF data with scanner info
  - Moves files and deletes source directory

**ScannerRepository** (Libs/Repositories/ScannerRepository.cs):
- `UpdateScanner` (lines 88-148):
  - Uses `IOHelpers.NetworkPathConverter.ResolvePath()` for cross-platform path validation
  - Validates WatchedDir, DestinationDir, ArchiveDir exist before saving

### Services Layer (Libs/Services)

**FileSystemWatcherService** (Libs/Services/FileSystemWatcherService.cs):
- Current implementation: Simple watcher creation/disposal
- Stores watchers in `ConcurrentDictionary<Guid, FileSystemWatcher>`
- **No roll/scanner association tracking**
- **No strategy-based behavior**
- Events: OnChanged, OnRenamed (just console logging)

### API Layer (API)

**Controllers:**
- `RollController` (API/Controllers/RollController.cs):
  - `POST /roll/complete` → calls `RollRepository.ProcessRoll()`
  - `PUT /roll/updateStatus` → updates roll status via repository
  - Currently no watcher management logic

- `ScannerController` (API/Controllers/ScannerController.cs):
  - CRUD operations for scanners
  - **No profile assignment yet**

**Models:**
- Request/Response DTOs in API/Models/RequestsResponses/

### Client Layer (Client)

**Services:**
- `ApiService` (Client/Services/ApiService.cs): HTTP client wrapper
- `ScannerService` (Client/Services/ScannerService.cs): Scanner selection state management
- `AuthService` (Client/Services/AuthService.cs): Authentication
- `TokenService` (Client/Services/TokenService.cs): JWT token management

**ViewModels:**
- `DashboardViewModel` (Client/ViewModels/DashboardViewModel.cs):
  - `CompleteRoll` method (line 391): Calls `/roll/complete` endpoint
  - **No watcher start/stop logic currently**

### Existing Patterns & Conventions

**Good patterns to maintain:**
1. `SystemResponse` class for repository return values (IsSuccess, Message, ReturnObject)
2. Network path conversion via `IOHelpers.NetworkPathConverter.ResolvePath()`
3. Async/await throughout
4. MVVM pattern in Avalonia clients
5. EF Core with migrations
6. JWT authentication with ASP.NET Identity

**Challenges identified:**
1. FileSystemWatcherService is too simple for scanner-specific behaviors
2. ProcessRoll has hardcoded directory discovery logic (needs strategy injection)
3. No relationship between Scanner and profiles in database
4. Client doesn't differentiate between manual vs automatic scanner workflows

---

## Migration Path

### **PHASE 1: Database Foundation**
*Estimated Time: 2-3 hours*
*Dependencies: None*

#### 1.1 Create ScannerProfile Model
**File:** `Libs/Data/Models/ScannerProfile.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace Libs.Data.Models
{
    public class ScannerProfile
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string ProfileName { get; set; } // "HS-1800", "SP-500 Manual", etc.

        [Required]
        public string StrategyClassName { get; set; } // "HS1800Strategy", "SP500Strategy"

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true; // Soft delete capability

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public DateTime? DateUpdated { get; set; }
    }
}
```

#### 1.2 Create ProfileConfiguration Model
**File:** `Libs/Data/Models/ProfileConfiguration.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace Libs.Data.Models
{
    public class ProfileConfiguration
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProfileId { get; set; }

        public ScannerProfile? Profile { get; set; }

        [Required]
        public string ConfigKey { get; set; } // "CompletionDelaySeconds", "DirectoryPattern", etc.

        [Required]
        public string ConfigValue { get; set; } // "25", "{WatchedDir}/{YYYYMMDD}/*", etc.

        public string? Description { get; set; }
    }
}
```

#### 1.3 Update Scanner Model
**File:** `Libs/Data/Models/Scanner.cs`

**Add these properties:**
```csharp
public Guid? ProfileId { get; set; } // Nullable for backward compatibility

public ScannerProfile? Profile { get; set; }
```

**Update constructor to include ProfileId:**
```csharp
public Scanner(Scanner? scnr)
{
    // ... existing properties ...
    ProfileId = scnr?.ProfileId;
}
```

#### 1.4 Update ScanLabContext
**File:** `Libs/Data/Context/ScanLabContext.cs`

**Add DbSets:**
```csharp
public DbSet<ScannerProfile> ScannerProfiles { get; set; }
public DbSet<ProfileConfiguration> ProfileConfigurations { get; set; }
```

**Add relationships in OnModelCreating:**
```csharp
// Scanner -> Profile relationship
modelBuilder.Entity<Scanner>()
    .HasOne(s => s.Profile)
    .WithMany()
    .HasForeignKey(s => s.ProfileId)
    .OnDelete(DeleteBehavior.SetNull);

// Profile -> Configurations relationship
modelBuilder.Entity<ScannerProfile>()
    .HasMany<ProfileConfiguration>()
    .WithOne(pc => pc.Profile)
    .HasForeignKey(pc => pc.ProfileId)
    .OnDelete(DeleteBehavior.Cascade);
```

#### 1.5 Create Migration
**Terminal Command:**
```bash
cd Scan-Lab
dotnet ef migrations add AddScannerProfiles --project Libs --startup-project API
```

#### 1.6 Seed Initial Profiles
**File:** `Libs/Data/SeedData/ProfileSeeder.cs` (new file)

```csharp
namespace Libs.Data.SeedData
{
    public static class ProfileSeeder
    {
        public static async Task SeedProfiles(ScanLabContext context)
        {
            if (context.ScannerProfiles.Any())
                return; // Already seeded

            var profiles = new List<ScannerProfile>
            {
                new ScannerProfile
                {
                    ProfileName = "HS-1800 Auto",
                    StrategyClassName = "HS1800Strategy",
                    Description = "Automatic processing for HS-1800 scanners with daily folder structure"
                },
                new ScannerProfile
                {
                    ProfileName = "SP-500 Manual",
                    StrategyClassName = "SP500Strategy",
                    Description = "Manual processing for SP-500 scanners"
                },
                new ScannerProfile
                {
                    ProfileName = "SP-3000 Manual",
                    StrategyClassName = "SP3000Strategy",
                    Description = "Manual processing for SP-3000 scanners"
                }
            };

            context.ScannerProfiles.AddRange(profiles);
            await context.SaveChangesAsync();

            // Add default configurations for HS-1800
            var hs1800Profile = profiles.First(p => p.StrategyClassName == "HS1800Strategy");
            var hs1800Configs = new List<ProfileConfiguration>
            {
                new ProfileConfiguration
                {
                    ProfileId = hs1800Profile.Id,
                    ConfigKey = "CompletionDelaySeconds",
                    ConfigValue = "25",
                    Description = "Time to wait after directory creation before processing"
                },
                new ProfileConfiguration
                {
                    ProfileId = hs1800Profile.Id,
                    ConfigKey = "DirectoryPattern",
                    ConfigValue = "{WatchedDir}/{YYYYMMDD}/*",
                    Description = "Expected directory structure pattern"
                }
            };

            context.ProfileConfigurations.AddRange(hs1800Configs);
            await context.SaveChangesAsync();
        }
    }
}
```

**Call seeder in API startup:**
**File:** `API/Program.cs`

**Add after app.Build():**
```csharp
// Seed profiles on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ScanLabContext>();
    await Libs.Data.SeedData.ProfileSeeder.SeedProfiles(context);
}
```

#### 1.7 Run Migration
**Terminal Command:**
```bash
cd Scripts
.\Publish-API.ps1  # This runs the migration automatically
```

**Or manually:**
```bash
cd Scan-Lab
dotnet ef database update --project Libs --startup-project API
```

---

### **PHASE 2: Strategy Pattern Implementation**
*Estimated Time: 3-4 hours*
*Dependencies: Phase 1 complete*

#### 2.1 Create CompletionDetectionMode Enum
**File:** `Libs/Enums/CompletionDetectionMode.cs` (new file)

```csharp
namespace Libs.Enums
{
    public enum CompletionDetectionMode
    {
        Manual,           // SP-500/SP-3000 - scan tech clicks Complete Roll
        TimeBasedDelay,   // HS-1800 - automatic after delay
        ExitFile          // Future - watch for specific completion file
    }
}
```

#### 2.2 Create IScannerStrategy Interface
**File:** `Libs/Services/ScannerStrategies/IScannerStrategy.cs` (new directory and file)

```csharp
using Libs.Data.Models;
using Libs.Enums;

namespace Libs.Services.ScannerStrategies
{
    public interface IScannerStrategy
    {
        /// <summary>
        /// Returns the actual directory path to watch based on scanner's WatchedDir.
        /// Example: For HS-1800, appends daily folder like "20251125"
        /// </summary>
        string ResolveWatchPath(Scanner scanner);

        /// <summary>
        /// Should the FileSystemWatcher be configured with recursive = true?
        /// </summary>
        bool IsRecursive { get; }

        /// <summary>
        /// How completion is detected for this scanner model
        /// </summary>
        CompletionDetectionMode CompletionMode { get; }

        /// <summary>
        /// Delay in seconds for time-based completion (null if not applicable)
        /// </summary>
        int? CompletionDelaySeconds { get; }

        /// <summary>
        /// Given scanner's WatchedDir, find the newest roll directory.
        /// This replaces the hardcoded logic in RollRepository.ProcessRoll lines 112-122.
        /// </summary>
        Task<string?> GetLatestRollDirectory(Scanner scanner);

        /// <summary>
        /// Called when a new directory is detected (for auto-processing profiles).
        /// Returns true if processing should be triggered automatically.
        /// </summary>
        Task<bool> ShouldAutoProcess(Scanner scanner, string directoryPath);
    }
}
```

#### 2.3 Implement HS1800Strategy
**File:** `Libs/Services/ScannerStrategies/HS1800Strategy.cs` (new file)

```csharp
using Libs.Data.Models;
using Libs.Enums;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Services.ScannerStrategies
{
    public class HS1800Strategy : IScannerStrategy
    {
        public string ResolveWatchPath(Scanner scanner)
        {
            // Create daily folder path: {WatchedDir}/YYYYMMDD
            var today = DateTime.Now.ToString("yyyyMMdd");
            var dailyPath = Path.Combine(scanner.WatchedDir, today);

            // Ensure daily folder exists
            if (!Directory.Exists(dailyPath))
            {
                Directory.CreateDirectory(dailyPath);
            }

            return dailyPath;
        }

        public bool IsRecursive => true; // Watch subdirectories within daily folder

        public CompletionDetectionMode CompletionMode => CompletionDetectionMode.TimeBasedDelay;

        public int? CompletionDelaySeconds => 25; // 25 second delay like FileMover app

        public async Task<string?> GetLatestRollDirectory(Scanner scanner)
        {
            var watchPath = ResolveWatchPath(scanner);

            if (!Directory.Exists(watchPath))
                return null;

            var rollDirs = Directory.GetDirectories(watchPath)
                .Select(dir => new
                {
                    Path = dir,
                    WriteTime = Directory.GetLastWriteTimeUtc(dir)
                })
                .OrderByDescending(dir => dir.WriteTime)
                .ToList();

            return await Task.FromResult(rollDirs.FirstOrDefault()?.Path);
        }

        public async Task<bool> ShouldAutoProcess(Scanner scanner, string directoryPath)
        {
            // Wait for completion delay
            if (CompletionDelaySeconds.HasValue)
            {
                await Task.Delay(TimeSpan.FromSeconds(CompletionDelaySeconds.Value));
            }

            // Verify directory still exists and has files
            if (!Directory.Exists(directoryPath))
                return false;

            var files = Directory.GetFiles(directoryPath);
            return files.Length > 0;
        }
    }
}
```

#### 2.4 Implement SP500Strategy
**File:** `Libs/Services/ScannerStrategies/SP500Strategy.cs` (new file)

```csharp
using Libs.Data.Models;
using Libs.Enums;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Services.ScannerStrategies
{
    public class SP500Strategy : IScannerStrategy
    {
        public string ResolveWatchPath(Scanner scanner)
        {
            // SP-500 doesn't use daily folders - watch directory directly
            return scanner.WatchedDir;
        }

        public bool IsRecursive => false; // Only watch top level

        public CompletionDetectionMode CompletionMode => CompletionDetectionMode.Manual;

        public int? CompletionDelaySeconds => null; // Manual triggering only

        public async Task<string?> GetLatestRollDirectory(Scanner scanner)
        {
            if (!Directory.Exists(scanner.WatchedDir))
                return null;

            var rollDirs = Directory.GetDirectories(scanner.WatchedDir)
                .Select(dir => new
                {
                    Path = dir,
                    WriteTime = Directory.GetLastWriteTimeUtc(dir)
                })
                .OrderByDescending(dir => dir.WriteTime)
                .ToList();

            return await Task.FromResult(rollDirs.FirstOrDefault()?.Path);
        }

        public async Task<bool> ShouldAutoProcess(Scanner scanner, string directoryPath)
        {
            // Manual strategy never auto-processes
            return await Task.FromResult(false);
        }
    }
}
```

#### 2.5 Implement SP3000Strategy
**File:** `Libs/Services/ScannerStrategies/SP3000Strategy.cs` (new file)

```csharp
using Libs.Data.Models;
using Libs.Enums;

namespace Libs.Services.ScannerStrategies
{
    /// <summary>
    /// SP-3000 strategy - currently identical to SP-500 (manual processing)
    /// Separated for future customization if needed
    /// </summary>
    public class SP3000Strategy : SP500Strategy
    {
        // Inherits all behavior from SP500Strategy
        // Can override specific methods if SP-3000 differs in the future
    }
}
```

#### 2.6 Create Strategy Factory
**File:** `Libs/Services/ScannerStrategies/ScannerStrategyFactory.cs` (new file)

```csharp
using Libs.Data.Models;
using System;
using System.Collections.Generic;

namespace Libs.Services.ScannerStrategies
{
    public static class ScannerStrategyFactory
    {
        /// <summary>
        /// Registry of available strategy classes.
        /// IMPORTANT: When adding new strategies, register them here.
        /// </summary>
        public static readonly Dictionary<string, Type> StrategyRegistry = new()
        {
            { "HS1800Strategy", typeof(HS1800Strategy) },
            { "SP500Strategy", typeof(SP500Strategy) },
            { "SP3000Strategy", typeof(SP3000Strategy) }
        };

        /// <summary>
        /// Validates if a strategy class name is registered
        /// </summary>
        public static bool IsValidStrategy(string className)
        {
            return StrategyRegistry.ContainsKey(className);
        }

        /// <summary>
        /// Gets list of all available strategy class names (for Admin UI)
        /// </summary>
        public static List<string> GetAvailableStrategies()
        {
            return new List<string>(StrategyRegistry.Keys);
        }

        /// <summary>
        /// Creates a strategy instance from a scanner's profile
        /// </summary>
        public static IScannerStrategy? CreateStrategy(Scanner scanner)
        {
            if (scanner.Profile == null)
                return null;

            return CreateStrategy(scanner.Profile.StrategyClassName);
        }

        /// <summary>
        /// Creates a strategy instance from a class name
        /// </summary>
        public static IScannerStrategy? CreateStrategy(string strategyClassName)
        {
            if (!StrategyRegistry.TryGetValue(strategyClassName, out var strategyType))
                return null;

            return (IScannerStrategy?)Activator.CreateInstance(strategyType);
        }
    }
}
```

#### 2.7 Update RollRepository to Use Strategy
**File:** `Libs/Repositories/RollRepository.cs`

**Find the hardcoded directory discovery logic (lines 112-122):**
```csharp
var rollDirsSorted = Directory.GetDirectories(roll.Order.Scanner.WatchedDir).Select(dir => new
{
    Path = dir,
    CreationDate = Directory.GetCreationTime(dir),
    WriteTime = Directory.GetLastWriteTimeUtc(dir)
})
.OrderByDescending(dir => dir.WriteTime)
.ToList();

var latestRollDir = rollDirsSorted.Select(dir => dir.Path).ToList()[0];
```

**Replace with strategy-based logic:**
```csharp
// Load scanner strategy
var strategy = ScannerStrategyFactory.CreateStrategy(roll.Order.Scanner);

if (strategy == null)
    return new SystemResponse
    {
        IsSuccess = false,
        Message = $"Scanner profile not configured or invalid strategy class"
    };

// Use strategy to find latest roll directory
var latestRollDir = await strategy.GetLatestRollDirectory(roll.Order.Scanner);

if (string.IsNullOrEmpty(latestRollDir))
    return new SystemResponse
    {
        IsSuccess = false,
        Message = $"No roll directories found in scanner's watched directory"
    };
```

**Add using statement at top:**
```csharp
using Libs.Services.ScannerStrategies;
```

---

### **PHASE 3: Enhanced Watcher Service**
*Estimated Time: 4-5 hours*
*Dependencies: Phase 2 complete*

#### 3.1 Create WatcherSession Class
**File:** `Libs/Services/WatcherSession.cs` (new file)

```csharp
using Libs.Data.Models;
using Libs.Services.ScannerStrategies;

namespace Libs.Services
{
    /// <summary>
    /// Represents an active file system watcher session for a specific roll
    /// </summary>
    public class WatcherSession
    {
        public Guid SessionId { get; set; } = Guid.NewGuid();

        public Guid RollId { get; set; }

        public Roll Roll { get; set; }

        public Scanner Scanner { get; set; }

        public IScannerStrategy Strategy { get; set; }

        public FileSystemWatcher Watcher { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public CancellationTokenSource? CancellationToken { get; set; }
    }
}
```

#### 3.2 Rewrite FileSystemWatcherService
**File:** `Libs/Services/FileSystemWatcherService.cs`

**Replace entire contents with:**
```csharp
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Libs.Data.Models;
using Libs.Repositories;
using Libs.Services.ScannerStrategies;
using Microsoft.Extensions.Logging;

namespace Libs.Services
{
    public class FileSystemWatcherService : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, WatcherSession> _activeSessions = new();
        private readonly ILogger<FileSystemWatcherService>? _logger;

        // Delegate for auto-processing callback
        public delegate Task ProcessRollDelegate(Guid rollId, Guid? staffId);
        public ProcessRollDelegate? OnAutoProcessRoll { get; set; }

        public FileSystemWatcherService(ILogger<FileSystemWatcherService>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Starts a file system watcher for a specific roll based on its scanner's profile
        /// </summary>
        public Guid? StartWatcherForRoll(Roll roll, Guid? staffId)
        {
            if (roll.Order?.Scanner?.Profile == null)
            {
                _logger?.LogError($"Cannot start watcher: Roll {roll.RollId} has no scanner profile");
                return null;
            }

            var strategy = ScannerStrategyFactory.CreateStrategy(roll.Order.Scanner);
            if (strategy == null)
            {
                _logger?.LogError($"Cannot start watcher: Invalid strategy for scanner {roll.Order.Scanner.ScannerName}");
                return null;
            }

            // Check if watcher already exists for this roll
            var existingSession = _activeSessions.Values.FirstOrDefault(s => s.RollId == roll.RollId);
            if (existingSession != null)
            {
                _logger?.LogWarning($"Watcher already active for roll {roll.RollId}");
                return existingSession.SessionId;
            }

            try
            {
                var watchPath = strategy.ResolveWatchPath(roll.Order.Scanner);

                if (!Directory.Exists(watchPath))
                {
                    _logger?.LogError($"Watch path does not exist: {watchPath}");
                    return null;
                }

                var watcher = new FileSystemWatcher(watchPath)
                {
                    NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                    IncludeSubdirectories = strategy.IsRecursive
                };

                var session = new WatcherSession
                {
                    RollId = roll.RollId,
                    Roll = roll,
                    Scanner = roll.Order.Scanner,
                    Strategy = strategy,
                    Watcher = watcher,
                    CancellationToken = new CancellationTokenSource()
                };

                // Attach event handlers
                watcher.Created += async (sender, e) => await OnDirectoryCreated(session, e, staffId);

                watcher.EnableRaisingEvents = true;

                _activeSessions[session.SessionId] = session;

                _logger?.LogInformation($"Started watcher for roll {roll.RollId} (Session: {session.SessionId})");

                return session.SessionId;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to start watcher for roll {roll.RollId}");
                return null;
            }
        }

        /// <summary>
        /// Stops the watcher for a specific roll
        /// </summary>
        public bool StopWatcherForRoll(Guid rollId)
        {
            var session = _activeSessions.Values.FirstOrDefault(s => s.RollId == rollId);

            if (session == null)
            {
                _logger?.LogWarning($"No active watcher found for roll {rollId}");
                return false;
            }

            return StopWatcher(session.SessionId);
        }

        /// <summary>
        /// Stops a watcher by session ID
        /// </summary>
        public bool StopWatcher(Guid sessionId)
        {
            if (!_activeSessions.TryRemove(sessionId, out var session))
            {
                _logger?.LogWarning($"Session {sessionId} not found");
                return false;
            }

            try
            {
                session.CancellationToken?.Cancel();
                session.Watcher.EnableRaisingEvents = false;
                session.Watcher.Dispose();
                session.CancellationToken?.Dispose();

                _logger?.LogInformation($"Stopped watcher session {sessionId} for roll {session.RollId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error stopping watcher session {sessionId}");
                return false;
            }
        }

        /// <summary>
        /// Gets all active watcher sessions
        /// </summary>
        public IEnumerable<WatcherSession> GetActiveSessions()
        {
            return _activeSessions.Values;
        }

        /// <summary>
        /// Checks if a watcher is active for a specific roll
        /// </summary>
        public bool IsWatcherActive(Guid rollId)
        {
            return _activeSessions.Values.Any(s => s.RollId == rollId);
        }

        /// <summary>
        /// Event handler when a directory is created
        /// </summary>
        private async Task OnDirectoryCreated(WatcherSession session, FileSystemEventArgs e, Guid? staffId)
        {
            if (!e.ChangeType.HasFlag(WatcherChangeTypes.Created) || !Directory.Exists(e.FullPath))
                return;

            _logger?.LogInformation($"Directory created: {e.FullPath} (Session: {session.SessionId})");

            try
            {
                // Check if this strategy should auto-process
                var shouldProcess = await session.Strategy.ShouldAutoProcess(session.Scanner, e.FullPath);

                if (!shouldProcess)
                {
                    _logger?.LogInformation($"Strategy determined not to auto-process: {e.FullPath}");
                    return;
                }

                // Trigger auto-processing via callback
                if (OnAutoProcessRoll != null)
                {
                    _logger?.LogInformation($"Triggering auto-process for roll {session.RollId}");

                    await OnAutoProcessRoll(session.RollId, staffId);

                    // Stop watcher after successful processing
                    StopWatcher(session.SessionId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error in OnDirectoryCreated handler for session {session.SessionId}");
            }
        }

        /// <summary>
        /// Dispose all active watchers
        /// </summary>
        public void Dispose()
        {
            foreach (var session in _activeSessions.Values)
            {
                session.CancellationToken?.Cancel();
                session.Watcher.EnableRaisingEvents = false;
                session.Watcher.Dispose();
                session.CancellationToken?.Dispose();
            }

            _activeSessions.Clear();

            _logger?.LogInformation("FileSystemWatcherService disposed");
        }
    }
}
```

#### 3.3 Register Watcher Service in API
**File:** `API/Program.cs`

**Find the service registration section and update:**
```csharp
// Change from Singleton to Scoped to allow dependency injection
builder.Services.AddScoped<FileSystemWatcherService>();
```

**Add using statement:**
```csharp
using Libs.Services;
```

---

### **PHASE 4: Repository & Interface Updates**
*Estimated Time: 2-3 hours*
*Dependencies: Phase 3 complete*

#### 4.1 Create Profile Repository Interface
**File:** `Libs/Interfaces/IProfileRepository.cs` (new file)

```csharp
using Libs.Classes;
using Libs.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Libs.Interfaces
{
    public interface IProfileRepository
    {
        Task<List<ScannerProfile>> GetProfiles();
        Task<ScannerProfile?> GetProfile(Guid id);
        Task<SystemResponse> AddProfile(ScannerProfile profile);
        Task<SystemResponse> UpdateProfile(ScannerProfile profile);
        Task<SystemResponse> DeleteProfile(Guid id);
        Task<List<ProfileConfiguration>> GetProfileConfigurations(Guid profileId);
        Task<SystemResponse> UpdateProfileConfiguration(ProfileConfiguration config);
    }
}
```

#### 4.2 Implement Profile Repository
**File:** `Libs/Repositories/ProfileRepository.cs` (new file)

```csharp
using Libs.Classes;
using Libs.Data.Context;
using Libs.Data.Models;
using Libs.Interfaces;
using Libs.Services.ScannerStrategies;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Repositories
{
    public class ProfileRepository : IProfileRepository, IDisposable
    {
        private readonly ScanLabContext _context;

        public ProfileRepository(ScanLabContext context)
        {
            _context = context;
        }

        public async Task<List<ScannerProfile>> GetProfiles()
        {
            return await _context.ScannerProfiles
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProfileName)
                .ToListAsync();
        }

        public async Task<ScannerProfile?> GetProfile(Guid id)
        {
            return await _context.ScannerProfiles
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<SystemResponse> AddProfile(ScannerProfile profile)
        {
            try
            {
                // Validate strategy class name
                if (!ScannerStrategyFactory.IsValidStrategy(profile.StrategyClassName))
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = $"Invalid strategy class name: {profile.StrategyClassName}"
                    };
                }

                // Check for duplicate profile name
                var duplicate = await _context.ScannerProfiles
                    .FirstOrDefaultAsync(p => p.ProfileName == profile.ProfileName);

                if (duplicate != null)
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = $"Profile with name '{profile.ProfileName}' already exists"
                    };
                }

                profile.DateCreated = DateTime.UtcNow;

                _context.ScannerProfiles.Add(profile);
                await _context.SaveChangesAsync();

                return new SystemResponse
                {
                    IsSuccess = true,
                    ReturnObject = profile
                };
            }
            catch (Exception ex)
            {
                return new SystemResponse
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<SystemResponse> UpdateProfile(ScannerProfile profile)
        {
            try
            {
                var dbProfile = await _context.ScannerProfiles
                    .FirstOrDefaultAsync(p => p.Id == profile.Id);

                if (dbProfile == null)
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = "Profile not found"
                    };
                }

                // Validate strategy class name
                if (!ScannerStrategyFactory.IsValidStrategy(profile.StrategyClassName))
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = $"Invalid strategy class name: {profile.StrategyClassName}"
                    };
                }

                dbProfile.ProfileName = profile.ProfileName;
                dbProfile.StrategyClassName = profile.StrategyClassName;
                dbProfile.Description = profile.Description;
                dbProfile.DateUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new SystemResponse { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return new SystemResponse
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<SystemResponse> DeleteProfile(Guid id)
        {
            try
            {
                var profile = await _context.ScannerProfiles
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (profile == null)
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = "Profile not found"
                    };
                }

                // Check if any scanners are using this profile
                var scannersUsingProfile = await _context.Scanners
                    .Where(s => s.ProfileId == id)
                    .CountAsync();

                if (scannersUsingProfile > 0)
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = $"Cannot delete profile: {scannersUsingProfile} scanner(s) are using this profile"
                    };
                }

                // Soft delete
                profile.IsActive = false;
                profile.DateUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new SystemResponse { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return new SystemResponse
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<List<ProfileConfiguration>> GetProfileConfigurations(Guid profileId)
        {
            return await _context.ProfileConfigurations
                .Where(pc => pc.ProfileId == profileId)
                .OrderBy(pc => pc.ConfigKey)
                .ToListAsync();
        }

        public async Task<SystemResponse> UpdateProfileConfiguration(ProfileConfiguration config)
        {
            try
            {
                var dbConfig = await _context.ProfileConfigurations
                    .FirstOrDefaultAsync(pc => pc.Id == config.Id);

                if (dbConfig == null)
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = "Configuration not found"
                    };
                }

                dbConfig.ConfigValue = config.ConfigValue;
                dbConfig.Description = config.Description;

                await _context.SaveChangesAsync();

                return new SystemResponse { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return new SystemResponse
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
```

#### 4.3 Update ScannerRepository
**File:** `Libs/Repositories/ScannerRepository.cs`

**Add ProfileId to AddScanner method (line 18-39):**
```csharp
public async Task<SystemResponse> AddScanner(Scanner scnr)
{
    try
    {
        // Validate profile if provided
        if (scnr.ProfileId.HasValue)
        {
            var profile = await _context.ScannerProfiles
                .FirstOrDefaultAsync(p => p.Id == scnr.ProfileId.Value);

            if (profile == null)
            {
                return new SystemResponse
                {
                    IsSuccess = false,
                    Message = "Selected profile not found"
                };
            }
        }

        _context.Scanners.Add(scnr);
        await _context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        return new SystemResponse { IsSuccess = false, Message = ex.Message };
    }

    return new SystemResponse { IsSuccess = true };
}
```

**Update UpdateScanner to handle ProfileId (around line 137):**
```csharp
dbScnr.ScannerName = scanner.ScannerName;
dbScnr.Make = scanner.Make;
dbScnr.Model = scanner.Model;
dbScnr.WatchedDir = compatibleWatchedDir;
dbScnr.DestinationDir = compatibleDestDir;
dbScnr.ArchiveDir = compatibleArchiveDir;
dbScnr.ArtistName = scanner.ArtistName;
dbScnr.ProfileId = scanner.ProfileId; // Add this line
```

**Update GetScanners to include profiles:**
```csharp
public async Task<List<Scanner>> GetScanners()
{
    return await _context.Scanners
        .Include(s => s.Profile)
        .ToListAsync();
}
```

---

### **PHASE 5: API Layer Updates**
*Estimated Time: 3-4 hours*
*Dependencies: Phase 4 complete*

#### 5.1 Create Profile Request/Response Models
**File:** `API/Models/RequestsResponses/ProfileRequestResponse.cs` (new file)

```csharp
using Libs.Data.Models;

namespace API.Models.RequestsResponses
{
    public class GetProfilesResponse
    {
        public List<ScannerProfile>? Profiles { get; set; }
    }

    public class AddProfileRequest
    {
        public string ProfileName { get; set; }
        public string StrategyClassName { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateProfileRequest
    {
        public Guid Id { get; set; }
        public string ProfileName { get; set; }
        public string StrategyClassName { get; set; }
        public string? Description { get; set; }
    }

    public class DeleteProfileRequest
    {
        public Guid Id { get; set; }
    }

    public class GetStrategiesResponse
    {
        public List<string> Strategies { get; set; }
    }
}
```

#### 5.2 Create ScannerProfileController
**File:** `API/Controllers/ScannerProfileController.cs` (new file)

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Models.RequestsResponses;
using Libs.Repositories;
using Libs.Data.Models;
using Libs.Services.ScannerStrategies;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ScannerProfileController : ControllerBase
    {
        private readonly ProfileRepository _profileRepository;

        public ScannerProfileController(ProfileRepository profileRepository)
        {
            _profileRepository = profileRepository;
        }

        [HttpGet("profiles")]
        public async Task<IActionResult> GetProfiles()
        {
            try
            {
                var profiles = await _profileRepository.GetProfiles();
                return Ok(new GetProfilesResponse { Profiles = profiles });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("profile/{id}")]
        public async Task<IActionResult> GetProfile(Guid id)
        {
            try
            {
                var profile = await _profileRepository.GetProfile(id);

                if (profile == null)
                    return NotFound("Profile not found");

                return Ok(profile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddProfile(AddProfileRequest req)
        {
            try
            {
                var profile = new ScannerProfile
                {
                    ProfileName = req.ProfileName,
                    StrategyClassName = req.StrategyClassName,
                    Description = req.Description
                };

                var resp = await _profileRepository.AddProfile(profile);

                if (!resp.IsSuccess)
                    return BadRequest(resp.Message);

                return Ok(resp.ReturnObject);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileRequest req)
        {
            try
            {
                var profile = new ScannerProfile
                {
                    Id = req.Id,
                    ProfileName = req.ProfileName,
                    StrategyClassName = req.StrategyClassName,
                    Description = req.Description
                };

                var resp = await _profileRepository.UpdateProfile(profile);

                if (!resp.IsSuccess)
                    return BadRequest(resp.Message);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteProfile(Guid id)
        {
            try
            {
                var resp = await _profileRepository.DeleteProfile(id);

                if (!resp.IsSuccess)
                    return BadRequest(resp.Message);

                return Ok("Profile deleted successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("strategies")]
        public IActionResult GetAvailableStrategies()
        {
            try
            {
                var strategies = ScannerStrategyFactory.GetAvailableStrategies();
                return Ok(new GetStrategiesResponse { Strategies = strategies });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("profile/{id}/configurations")]
        public async Task<IActionResult> GetProfileConfigurations(Guid id)
        {
            try
            {
                var configs = await _profileRepository.GetProfileConfigurations(id);
                return Ok(configs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
```

#### 5.3 Update RollController for Watcher Integration
**File:** `API/Controllers/RollController.cs`

**Add dependency injection for FileSystemWatcherService:**
```csharp
private readonly RollRepository _rollRepository;
private readonly OrderRepository _orderRepository;
private readonly FileSystemWatcherService _watcherService; // Add this

public RollController(
    RollRepository rollRepository,
    OrderRepository orderRepository,
    FileSystemWatcherService watcherService) // Add this parameter
{
    _rollRepository = rollRepository;
    _orderRepository = orderRepository;
    _watcherService = watcherService;

    // Set up auto-processing callback
    _watcherService.OnAutoProcessRoll = async (rollId, staffId) =>
    {
        await _rollRepository.ProcessRoll(rollId, staffId);
    };
}
```

**Update the updateStatus endpoint (around line 92):**
```csharp
[HttpPut("updateStatus")]
public async Task<IActionResult> UpdateRollStatus(UpdateRollRequest req)
{
    try
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userIdClaim, out var staffId);

        var roll = await _rollRepository.GetRoll(req.RollId);

        if (roll == null)
            return BadRequest(new UpdateRollResponse
            {
                Success = false,
                Message = "Error retrieving roll"
            });

        var resp = await _rollRepository.UpdateRollStatus(roll, req.Status, staffId);

        if (!resp.IsSuccess)
            return BadRequest(resp.Message);

        // Handle watcher lifecycle based on status and scanner profile
        if (req.Status == Libs.Enums.RollStatus.ScanningInProgress)
        {
            // Check if scanner has auto-processing profile
            if (roll.Order?.Scanner?.Profile != null)
            {
                var strategy = Libs.Services.ScannerStrategies.ScannerStrategyFactory
                    .CreateStrategy(roll.Order.Scanner);

                if (strategy?.CompletionMode == Libs.Enums.CompletionDetectionMode.TimeBasedDelay)
                {
                    // Start watcher for auto-processing
                    var sessionId = _watcherService.StartWatcherForRoll(roll, staffId);

                    if (sessionId == null)
                    {
                        return BadRequest("Failed to start file watcher");
                    }
                }
            }
        }
        else if (req.Status == Libs.Enums.RollStatus.ScanningPaused ||
                 req.Status == Libs.Enums.RollStatus.Created)
        {
            // Stop watcher if active
            _watcherService.StopWatcherForRoll(req.RollId);
        }

        return Ok("Roll status successfully updated");
    }
    catch (ArgumentException ex)
    {
        return BadRequest(ex.Message);
    }
}
```

**Add using statements:**
```csharp
using Libs.Services;
using System.Security.Claims;
```

#### 5.4 Update ScannerController
**File:** `API/Controllers/ScannerController.cs`

**Update AddScanner to include ProfileId (line 30-40):**
```csharp
var newScnr = new Scanner
{
    Id = Guid.NewGuid(),
    ScannerName = req.ScannerName,
    Make = req.Make,
    Model = req.Model,
    WatchedDir = req.WatchedDir,
    DestinationDir = req.DestinationDir,
    ArchiveDir = req.ArchiveDir,
    ArtistName = req.ArtistName,
    ProfileId = req.ProfileId // Add this line
};
```

**Update request model:**
**File:** `API/Models/RequestsResponses/ScannersRequestResponse.cs`

**Add ProfileId to AddScannerRequest:**
```csharp
public class AddScannerRequest
{
    public string ScannerName { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string WatchedDir { get; set; }
    public string DestinationDir { get; set; }
    public string ArchiveDir { get; set; }
    public string? ArtistName { get; set; }
    public Guid? ProfileId { get; set; } // Add this line
}
```

**Add ProfileId to UpdateScannerRequest:**
```csharp
public class UpdateScannerRequest
{
    public Scanner Scnr { get; set; } // Scanner model already has ProfileId
}
```

#### 5.5 Register ProfileRepository in API
**File:** `API/Program.cs`

**Add to service registration:**
```csharp
builder.Services.AddScoped<ProfileRepository>();
```

---

### **PHASE 6: Admin App Updates**
*Estimated Time: 4-5 hours*
*Dependencies: Phase 5 complete*

#### 6.1 Create Profile Management View
**File:** `Admin/Views/ProfileManagement/ProfileManagement.axaml` (new directory and file)

```xml
<UserControl
    x:Class="Admin.Views.ProfileManagement"
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:viewModels="clr-namespace:Admin.ViewModels"
    x:DataType="viewModels:ProfileManagementViewModel">

    <Grid Margin="20" RowDefinitions="Auto,*">

        <!-- Header -->
        <StackPanel Grid.Row="0" Margin="0,0,0,20">
            <TextBlock Text="Scanner Profile Management"
                       FontSize="24"
                       FontWeight="Bold" />
            <Button Content="Create New Profile"
                    Command="{Binding CreateProfileCommand}"
                    Margin="0,10,0,0" />
        </StackPanel>

        <!-- Profiles DataGrid -->
        <DataGrid Grid.Row="1"
                  ItemsSource="{Binding Profiles}"
                  SelectedItem="{Binding SelectedProfile}"
                  AutoGenerateColumns="False"
                  IsReadOnly="True">
            <DataGrid.Columns>
                <DataGridTextColumn Header="Profile Name"
                                    Binding="{Binding ProfileName}"
                                    Width="200" />
                <DataGridTextColumn Header="Strategy Class"
                                    Binding="{Binding StrategyClassName}"
                                    Width="150" />
                <DataGridTextColumn Header="Description"
                                    Binding="{Binding Description}"
                                    Width="*" />
                <DataGridTemplateColumn Header="Actions" Width="200">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal" Spacing="5">
                                <Button Content="Edit"
                                        Command="{Binding $parent[UserControl].DataContext.EditProfileCommand}"
                                        CommandParameter="{Binding}" />
                                <Button Content="Delete"
                                        Command="{Binding $parent[UserControl].DataContext.DeleteProfileCommand}"
                                        CommandParameter="{Binding}" />
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>

    </Grid>
</UserControl>
```

**Code-behind file:**
**File:** `Admin/Views/ProfileManagement/ProfileManagement.axaml.cs` (new file)

```csharp
using Avalonia.Controls;

namespace Admin.Views
{
    public partial class ProfileManagement : UserControl
    {
        public ProfileManagement()
        {
            InitializeComponent();
        }
    }
}
```

#### 6.2 Create Profile Management ViewModel
**File:** `Admin/ViewModels/ProfileManagementViewModel.cs` (new file)

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Libs.Data.Models;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Admin.Services;

namespace Admin.ViewModels
{
    public partial class ProfileManagementViewModel : ViewModelBase
    {
        private readonly HttpClient _httpClient = new();
        private readonly ApiService _apiService;

        [ObservableProperty]
        private ObservableCollection<ScannerProfile> profiles = new();

        [ObservableProperty]
        private ScannerProfile? selectedProfile;

        public ProfileManagementViewModel() : this(App.ApiService) { }

        public ProfileManagementViewModel(ApiService apiService)
        {
            _apiService = apiService;
            LoadProfilesAsync();
        }

        private async Task LoadProfilesAsync()
        {
            try
            {
                var url = $"{_apiService.ApiAddress}/api/ScannerProfile/profiles";
                var response = await _httpClient.GetStringAsync(url);

                var result = JsonSerializer.Deserialize<GetProfilesResponse>(response,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Profiles != null)
                {
                    Profiles = new ObservableCollection<ScannerProfile>(result.Profiles);
                }
            }
            catch (Exception ex)
            {
                // Handle error - show message to user
                Console.WriteLine($"Error loading profiles: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task CreateProfile()
        {
            // TODO: Open dialog for creating new profile
            // For now, placeholder
            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task EditProfile(ScannerProfile profile)
        {
            // TODO: Open dialog for editing profile
            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task DeleteProfile(ScannerProfile profile)
        {
            // TODO: Confirm and delete profile
            await Task.CompletedTask;
        }
    }

    // Response model
    public class GetProfilesResponse
    {
        public List<ScannerProfile>? Profiles { get; set; }
    }
}
```

#### 6.3 Update Scanner Configuration View
**File:** `Admin/Views/Settings/Settings.axaml` (or wherever scanner configuration is)

**Add Profile selection ComboBox:**
```xml
<TextBlock FontWeight="Bold" Text="Scanner Profile:" />
<ComboBox
    HorizontalAlignment="Stretch"
    ItemsSource="{Binding AvailableProfiles}"
    SelectedItem="{Binding SelectedProfile}">
    <ComboBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding ProfileName}" />
        </DataTemplate>
    </ComboBox.ItemTemplate>
</ComboBox>
```

**Update ViewModel to load profiles:**
```csharp
[ObservableProperty]
private ObservableCollection<ScannerProfile> availableProfiles = new();

[ObservableProperty]
private ScannerProfile? selectedProfile;

private async Task LoadAvailableProfiles()
{
    try
    {
        var url = $"{_apiService.ApiAddress}/api/ScannerProfile/profiles";
        var response = await _httpClient.GetStringAsync(url);

        var result = JsonSerializer.Deserialize<GetProfilesResponse>(response,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result?.Profiles != null)
        {
            AvailableProfiles = new ObservableCollection<ScannerProfile>(result.Profiles);
        }
    }
    catch (Exception ex)
    {
        // Handle error
    }
}
```

---

### **PHASE 7: Client App Updates**
*Estimated Time: 3-4 hours*
*Dependencies: Phase 5 complete*

#### 7.1 Update DashboardViewModel
**File:** `Client/ViewModels/DashboardViewModel.cs`

**Find the CompleteRoll method (around line 391) and update:**

```csharp
[RelayCommand]
public async Task CompleteRoll(Roll? roll)
{
    if (roll == null) return;

    try
    {
        // Show loading
        IsProcessing = true;

        var url = $"{_apiService.ApiAddress}/api/Roll/complete";

        var request = new
        {
            RollId = roll.RollId
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Add authorization header
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization",
            $"Bearer {_authService.Token}");

        var response = await _httpClient.PostAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            await ShowMessageAsync("Success",
                "Roll completed successfully",
                MessageType.Success);

            // Refresh orders
            await LoadOrdersAsync();
        }
        else
        {
            var errorMsg = await response.Content.ReadAsStringAsync();
            await ShowMessageAsync("Error",
                $"Failed to complete roll: {errorMsg}",
                MessageType.Error);
        }
    }
    catch (Exception ex)
    {
        await ShowMessageAsync("Error",
            $"Error completing roll: {ex.Message}",
            MessageType.Error);
    }
    finally
    {
        IsProcessing = false;
    }
}
```

**Update the UpdateRollStatus method to handle watcher logic:**

```csharp
[RelayCommand]
private async Task UpdateRollStatus(object parameter)
{
    // ... existing code ...

    // After successful status update
    if (response.IsSuccessStatusCode)
    {
        // If starting scanning and scanner has auto-processing profile
        if (newStatus == RollStatus.ScanningInProgress)
        {
            // Check if this is an auto-processing scanner
            if (roll.Order?.Scanner?.Profile != null)
            {
                // TODO: Check profile's completion mode
                // For now, show message that watcher is active
                await ShowMessageAsync("Info",
                    "File watcher started for this roll",
                    MessageType.Info);
            }
        }

        await LoadOrdersAsync();
    }
}
```

#### 7.2 Update Order Loading to Include Scanner Profiles
**Verify that API response includes Scanner.Profile:**

The `GetScanners()` method in ScannerRepository already includes profiles (Phase 4.3).

**Client deserialization should automatically handle it** since Scanner model now has Profile property.

---

### **PHASE 8: Testing & Validation**
*Estimated Time: 4-6 hours*
*Dependencies: All previous phases complete*

#### 8.1 Unit Tests for Strategies
**File:** `Libs.Tests/Services/ScannerStrategies/StrategyTests.cs` (new test project if needed)

Create tests for:
- HS1800Strategy.ResolveWatchPath() creates daily folder correctly
- SP500Strategy.GetLatestRollDirectory() returns newest directory
- Strategy factory validates class names

#### 8.2 Integration Tests
**Create test scenarios:**

1. **HS-1800 Auto-Processing Flow:**
   - Create order with scanner that has HS-1800 profile
   - Add roll to order
   - Update roll status to ScanningInProgress
   - Verify watcher starts
   - Simulate directory creation in watched folder
   - Verify 25-second delay
   - Verify ProcessRoll is called automatically
   - Verify files are moved/renamed correctly

2. **SP-500 Manual Flow:**
   - Create order with scanner that has SP-500 profile
   - Add roll to order
   - Update roll status to ScanningInProgress
   - Verify NO watcher starts
   - Manually call CompleteRoll endpoint
   - Verify ProcessRoll executes
   - Verify files are moved/renamed correctly

3. **Profile Management:**
   - Create new profile via Admin app
   - Assign profile to scanner
   - Verify validation of strategy class name
   - Attempt to delete profile in use (should fail)

#### 8.3 Manual Testing Checklist

**Admin App:**
- [ ] View all scanner profiles
- [ ] Create new profile with valid strategy
- [ ] Create new profile with invalid strategy (should fail)
- [ ] Edit existing profile
- [ ] Delete unused profile (should succeed)
- [ ] Delete profile in use (should fail)
- [ ] Assign profile to scanner
- [ ] Update scanner with new profile

**Client App:**
- [ ] Create order with HS-1800 scanner
- [ ] Start scanning roll (verify watcher starts)
- [ ] Scanner exports files to daily folder
- [ ] Wait 25 seconds
- [ ] Verify auto-processing completes
- [ ] Verify files moved to destination
- [ ] Create order with SP-500 scanner
- [ ] Start scanning roll (verify no watcher)
- [ ] Click Complete Roll manually
- [ ] Verify processing completes

**API:**
- [ ] GET /api/ScannerProfile/profiles returns seeded profiles
- [ ] GET /api/ScannerProfile/strategies returns hardcoded list
- [ ] POST /api/ScannerProfile/add with valid data succeeds
- [ ] POST /api/ScannerProfile/add with invalid strategy fails
- [ ] PUT /api/Roll/updateStatus starts watcher for HS-1800
- [ ] PUT /api/Roll/updateStatus does NOT start watcher for SP-500

#### 8.4 Error Scenarios to Test

- [ ] API restart while watcher is active (should clear gracefully)
- [ ] Invalid directory path in scanner configuration
- [ ] Scanner profile missing when starting watcher
- [ ] Auto-processing fails (verify error logged and manual option available)
- [ ] Daily folder doesn't exist for HS-1800 (should create it)
- [ ] Multiple rolls trying to use same scanner simultaneously

---

### **PHASE 9: Documentation & Cleanup**
*Estimated Time: 2-3 hours*
*Dependencies: Phase 8 complete*

#### 9.1 Update CLAUDE.md
**File:** `CLAUDE.md`

**Add section about scanner profiles:**

```markdown
### Scanner Profile System

The system uses a strategy pattern to handle different scanner models with varying file organization behaviors.

**Profiles:**
- **HS-1800 Auto**: Automatic processing with 25-second delay, daily folder structure
- **SP-500 Manual**: Manual processing, flat directory structure
- **SP-3000 Manual**: Manual processing, flat directory structure

**Key Files:**
- `Libs/Services/ScannerStrategies/` - Strategy implementations
- `Libs/Services/FileSystemWatcherService.cs` - Multiplexed watcher service
- `API/Controllers/ScannerProfileController.cs` - Profile CRUD endpoints

**Adding New Scanner Profiles:**
1. Create strategy class implementing `IScannerStrategy`
2. Register in `ScannerStrategyFactory.StrategyRegistry`
3. Create profile via Admin app
4. Assign to scanner

**Watcher Lifecycle:**
- Started when roll status → `ScanningInProgress` (auto-processing profiles only)
- Stopped when roll status → `ScanningPaused` or `Created`
- Automatically stopped after successful auto-processing
- In-memory (cleared on API restart)
```

#### 9.2 Add Code Comments
**Review and add XML documentation comments to:**
- All public methods in strategy classes
- FileSystemWatcherService public API
- ProfileRepository methods

#### 9.3 Create Migration Notes
**File:** `MIGRATION_NOTES.md` (new file in project root)

Document:
- Breaking changes (Scanner model now requires Profile)
- Migration steps for existing scanners
- How to roll back if needed

#### 9.4 Remove Dead Code
- Remove old FileSystemWatcherService code (if any backup was made)
- Clean up commented-out code in RollRepository
- Verify no unused imports

---

## Post-Migration Tasks

### Immediate (Week 1)
- [ ] Monitor logs for watcher errors
- [ ] Verify all existing scanners have profiles assigned
- [ ] Train scan techs on new auto-processing workflow

### Short-term (Month 1)
- [ ] Gather feedback on HS-1800 auto-processing reliability
- [ ] Consider implementing retry logic for failed auto-processing
- [ ] Add metrics/analytics for watcher activity

### Long-term (Quarter 1)
- [ ] Implement SP-500 auto-processing strategy
- [ ] Add exit file completion detection mode
- [ ] Consider adding SignalR for real-time watcher status updates to Client

---

## Rollback Plan

If critical issues arise:

1. **Database rollback:**
   ```bash
   dotnet ef database update PreviousMigrationName --project Libs --startup-project API
   ```

2. **Code rollback:**
   ```bash
   git revert <commit-hash>
   git push origin dev
   ```

3. **Scanners continue working** with manual ProcessRoll even if profiles are broken

---

## Estimated Total Time
- **Phase 1:** 2-3 hours
- **Phase 2:** 3-4 hours
- **Phase 3:** 4-5 hours
- **Phase 4:** 2-3 hours
- **Phase 5:** 3-4 hours
- **Phase 6:** 4-5 hours
- **Phase 7:** 3-4 hours
- **Phase 8:** 4-6 hours
- **Phase 9:** 2-3 hours

**Total: 27-37 hours** (approximately 1-2 weeks for a single developer)

---

## Success Criteria

✅ **Phase 1:** Migration runs successfully, profiles seeded in database
✅ **Phase 2:** Strategy factory creates correct strategy instances
✅ **Phase 3:** Watchers start/stop based on roll status
✅ **Phase 4:** Repositories handle profiles correctly
✅ **Phase 5:** API endpoints functional and tested
✅ **Phase 6:** Admin app can manage profiles
✅ **Phase 7:** Client app differentiates manual vs auto workflows
✅ **Phase 8:** All test scenarios pass
✅ **Phase 9:** Documentation complete and accurate

**Final Goal:** HS-1800 scanners auto-process without manual "Complete Roll" clicks, while SP-500/SP-3000 continue with manual workflow.
