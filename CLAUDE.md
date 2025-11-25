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
- FileSystemWatcherService uses concurrent dictionary to manage multiple watchers by GUID
