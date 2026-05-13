# Scan Lab System Documentation

## 1. System Overview

Scan Lab is a film scanning management system for Coastal Film Lab consisting of four interconnected .NET applications:

- **API** (.NET 8): REST API backend serving order, roll, and scanner management endpoints
- **Libs** (.NET 8): Shared library containing data models, Entity Framework Core context, repositories, helpers, and services
- **Client** (.NET 9 Avalonia): Desktop application for scan technicians to process orders and manage scanning workflow
- **Admin** (.NET 9 Avalonia): Administrative desktop application for system configuration

The system tracks film scanning orders through a workflow where orders contain rolls, and scan technicians process rolls using designated scanners. The API uses PostgreSQL for data persistence and manages file system monitoring for scanned image collection.

## 2. Projects

### 2.1 API (Backend)

**Location**: `API/`
**Target Framework**: .NET 8
**Key Features**:
- RESTful ASP.NET Core API
- Entity Framework Core 9 with Npgsql provider for PostgreSQL
- JWT-based authentication
- Serilog for logging (with PostgreSQL sinks)
- Swagger/OpenAPI documentation
- Controllers for Orders, Rolls, Scanners, Staff, Authentication, Logs, and Analytics

**Important Files**:
- `Program.cs`: Application setup, middleware, services configuration, and database contexts
- `Controllers/`: REST API endpoints
  - `OrderController.cs`: Order CRUD and processing
  - `RollController.cs`: Roll management and status updates
  - `ScannerController.cs`: Scanner configuration
  - `AuthController.cs`: Authentication endpoints
  - `StaffController.cs`: Staff management
  - `LogController.cs`: Log retrieval
  - `AnalyticsController.ts`: Analytics data
- `API.csproj`: Project file showing dependencies on Libs and various NuGet packages

### 2.2 Libs (Shared Library)

**Location**: `Libs/`
**Target Framework**: .NET 8
**Purpose**: Contains shared code used by both the API and client applications.

**Key Components**:

**Data Models** (`Libs/Data/Models/`):
- `Order.cs`: Represents a scanning order (OrderId as primary key, Customer, Scanner, list of Rolls, Status)
- `Roll.cs`: Belongs to an Order via OrderId FK, has RollNumber, RollStatus, FilmType
- `Scanner.cs`: Represents a physical film scanner with export directory paths (WatchedDir, DestinationDir, ArchiveDir)
- `Customer.cs`: Basic customer information
- `Staff.cs`: Extends IdentityUser for authentication (used by API)
- `LogEntry.cs`: For application logging
- `ConfigSetting.cs`: For system configuration

**Repositories** (`Libs/Repositories/`):
- Implement data access patterns using Entity Framework Core
- `OrderRepository.cs`: CRUD operations for orders, order processing logic
- `RollRepository.cs`: Roll management including status updates and file processing
- `ScannerRepository.cs`: Scanner configuration and retrieval
- `StaffRepository.cs`: Staff management
- `LogRepository.cs`: Log retrieval
- `AuthRepository.cs`: Authentication helpers
- `AnalyticsRepository.cs`: Analytics queries

**Helpers** (`Libs/Helpers/`):
- `IOHelpers.NetworkPathConverter`: Converts between Linux GVFS paths (AFP/SMB) and Windows UNC paths for cross-platform network directory access
- `ImageFileHelpers`: Image processing utilities using ImageSharp (BMP to TIFF conversion, EXIF data handling)
- `DateTimeHelpers`: Timestamp utilities (e.g., getting Monday of a week)

**Services** (`Libs/Services/`):
- `FileSystemWatcherService`: Singleton service managing file watchers for scanner export directories (uses ConcurrentDictionary)

**Interfaces** (`Libs/Interfaces/`):
- Define contracts for repositories (e.g., `IOrderRepository`, `IRollRepository`)

**Enums** (`Libs/Enums/`):
- `OrderStatus`: Created, Processing, InProgress, Completed, Cancelled
- `RollStatus`: Created, ScanningInProgress, ScanningPaused, Processed
- `FilmType`: Various film types (e.g., 8mm, 16mm, 35mm)
- `LogArea`, `LogLevel`: For logging categorization
- `IntervalType`: For analytics intervals

### 2.3 Client (Scan Technician Application)

**Location**: `Client/`
**Target Framework**: .NET 9
**UI Framework**: Avalonia 11.2.1 with Fluent theme
**Runtime Identifiers**: win-x64 and linux-x64 (self-contained publishes)
**Key Features**:
- MVVM pattern using CommunityToolkit.Mvvm and ReactiveUI
- Authentication flow (login/jwt token management)
- Dashboard for viewing and managing orders
- Order form for creating new orders
- Settings for configuring API address and default scanner
- Real-time updates of order/roll status
- Integration with scanner hardware via directory monitoring (handled by API)

**Key Files**:
- `App.axaml.cs`: Application startup
- `Services/`: 
  - `ApiService.cs`: Handles API communication and address persistence
  - `AuthService.cs`: Handles user authentication (login)
  - `TokenService.cs`: Manages JWT token storage and validation
  - `ScannerService.cs`: Manages selected scanner persistence
- `ViewModels/`:
  - `MainWindowViewModel.cs`: Controls navigation between views (Login, Dashboard, OrderForm, Settings)
  - `DashboardViewModel.cs`: Main interface for technicians - displays orders, allows roll status updates, scanning controls
  - `OrderFormViewModel.cs`: Form for creating new orders with scanner selection
  - `SettingsViewModel.cs`: Configures API address and scanner preferences
  - `LoginViewModel.cs`: Handles user authentication
- `Views/` (AXAML with code-behind):
  - Dashboard view with order list and roll controls
  - Order form for creating orders
  - Settings view
  - Login view
- `Converters/`: Value converters for UI (boolean visibility, datetime formatting, etc.)

### 2.4 Admin (Administrative Application)

**Location**: `Admin/`
**Target Framework**: .NET 9
**UI Framework**: Avalonia 11.2.1 with Fluent theme
**Runtime Identifiers**: win-x64 and linux-x64 (self-contained publishes)
**Key Features**:
- Dashboard with analytics charts (orders per staff, rolls per staff, etc.)
- Staff management
- Scanner management
- Activity log viewing
- Settings for API address

**Key Files**:
- Similar structure to Client but focused on administrative tasks
- `ViewModels/`:
  - `MainWindowViewModel.cs`: Navigation between Dashboard, Orders, Scanners, Settings, ActivityLog, StaffManagement
  - `DashboardViewModel.cs`: Analytics dashboard with filtering capabilities
  - `OrdersViewModel.cs`: View and manage orders
  - `ScannersViewModel.cs`: Configure scanners
  - `SettingsViewModel.cs`: API address configuration
  - `ActivityLogViewModel.cs`: View system logs
  - `StaffManagementViewModel.cs`: Manage staff users
- `Services/ApiService.cs`: Similar to Client's ApiService but without token handling (admin may use different auth or none)

### 2.5 Scripts

**Location**: `Scripts/`
**Purpose**: PowerShell scripts for publishing applications
- `Publish-API.ps1`: Publishes the API (self-contained) and runs database migrations
- `Publish-Client.ps1`: Publishes Client for Windows and Linux (self-contained single files)
- `Publish-Admin.ps1`: Publishes Admin for Windows and Linux (self-contained single files)

## 3. Data Models (Core Entities)

### 3.1 Order
- `OrderId` (string, unique, primary key)
- `Customer` (navigation property to Customer)
- `CustomerInitials` (string)
- `Scanner` (navigation property to Scanner)
- `Rolls` (list of Roll objects)
- `Status` (OrderStatus enum)
- `DateCreated` (DateTime)
- `DateUpdated` (DateTime)
- `CreatedBy` (Guid, references Staff)
- `UpdatedBy` (Guid, references Staff)

### 3.2 Roll
- `RollId` (Guid, primary key)
- `RollNumber` (long, NOT unique globally - can repeat across orders)
- `ImageCount` (int?)
- `FilmType` (FilmType enum?)
- `RollNotes` (list of strings)
- `OrderId` (string, foreign key to Order)
- `Order` (navigation property to Order)
- `Status` (RollStatus enum)
- `DateCreated` (DateTime)
- `DateUpdated` (DateTime)
- `CreatedBy` (Guid, references Staff)
- `UpdatedBy` (Guid, references Staff)

### 3.3 Scanner
- `Id` (Guid, primary key)
- `ScannerName` (string, required)
- `Make` (string)
- `Model` (string)
- `ArtistName` (string)
- `WatchedDir` (string, required - directory to monitor for new scan files)
- `DestinationDir` (string, required - directory where processed files are moved)
- `ArchiveDir` (string, required - directory for archived files)
- Default constructor and copy constructor

### 3.4 Customer
- `Id` (Guid, primary key)
- `FirstName` (string)
- `LastName` (string)
- `Orders` (navigation property to list of Orders)

### 3.5 Staff (for authentication)
- Inherits from `IdentityUser<Guid>`
- `FirstName` (string?)
- `LastName` (string?)
- `CreatedAt` (DateTime)

## 4. Client-API Interaction

The Avalonia clients (Client and Admin) interact with the API through HTTP REST calls using the `ApiService` and related services.

### 4.1 API Service (`ApiService.cs`)
- Manages the API address (loaded/saved from `%APPDATA%\ScanLab\api_config.json`)
- Provides an `HttpClient` instance
- Handles adding authentication headers (JWT token) via `TokenService` (in Client)
- Exposes `ApiAddress` property with change notifications

### 4.2 Authentication Flow (Client only)
1. User enters credentials in Login view
2. `AuthService.AuthenticateAsync()` sends POST to `/api/auth/login` with username/password
3. API validates credentials and returns JWT token
4. `TokenService` stores the token
5. `ApiService` automatically adds `Authorization: Bearer <token>` header to subsequent requests
6. Token validity is checked on each request; if expired, user is redirected to login

### 4.3 Example API Calls from Client
- **DashboardViewModel.LoadOrdersAsync()**:
  - POST to `/api/Order/orders` with JSON body containing search filters, status, scannerId, and fetchCompletedOrders flag
  - Returns JSON array of Order objects (with Rolls and related data)
- **OrderFormViewModel.SubmitOrderAsync()**:
  - POST to `/api/Order/submit` with new Order object (including Scanner, Customer, Rolls)
  - Returns success/failure response
- **DashboardViewModel.CompleteRoll()**:
  - POST to `/api/Roll/complete` with RollId
  - Triggers roll processing logic in API (file renaming, EXIF updates, directory moves)
- **DashboardViewModel.StartResumeScanningRoll()**:
  - PUT to `/api/Roll/UpdateStatus` with RollId and Status=ScanningInProgress
  - Updates roll status and parent order status

### 4.4 Admin API Interaction
- Similar to Client but without authentication token handling (Admin may operate without strict auth or use different mechanism)
- Calls to analytics endpoints (`/api/Analytics/*`) for dashboard charts
- CRUD operations for scanners and staff via respective controllers

## 5. System Requirements

### 5.1 Software
- **.NET SDK 9.0** (for building Client and Admin)
- **.NET SDK 8.0** (for building API and Libs)
- **PostgreSQL** database (version compatible with Npgsql 9.0.4)
- **Windows 10/11** or **Linux** (for running published applications)
- **Scanner hardware** with accessible export directories (SMB/AFP or local paths)

### 5.2 Dependencies (from project files)
**API/Libs**:
- Microsoft.EntityFrameworkCore 9.0.8
- Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4
- Serilog.AspNetCore 9.0.0 + various sinks
- Swashbuckle.AspNetCore 6.4.0
- System.Text.Json 9.0.7 (explicitly referenced to avoid IIS issues)
- Microsoft.AspNetCore.Authentication.JwtBearer 9.0.8
- SixLabors.ImageSharp 3.1.6
- MailKit 4.13.0 (for email functionality in Libs)

**Client/Admin**:
- Avalonia 11.2.1
- Avalonia.Controls.DataGrid 11.2.1
- Avalonia.Themes.Fluent 11.2.1
- Avalonia.Fonts.Inter 11.2.1
- CommunityToolkit.Mvvm 8.4.0
- ReactiveUI 20.2.45 (Client only)
- System.IdentityModel.Tokens.Jwt 8.13.1 (Client only)

### 5.3 Environment Variables
- ASPNETCORE_ENVIRONMENT (set during publish, e.g., "Production")
- Database connection strings in `appsettings.json` (API):
  - `ScanLabDBConnection`: Main PostgreSQL connection
  - `ScanLab_LogsDBConnection`: Separate database for Serilog logs
- JWT settings in `appsettings.json`:
  - `Jwt:Key`: Secret key for token signing
  - `Jwt:Issuer`: Token issuer
  - `Jwt:Audience`: Token audience

## 6. General Instructions

### 6.1 Building the Solution

From the root directory:
```bash
dotnet build "Scan Lab.sln"
```

Individual projects:
```bash
dotnet build API/API.csproj
dotnet build Libs/Libs.csproj
dotnet build Client/Client.csproj
dotnet build Admin/Admin.csproj
```

### 6.2 Running the API (Development)

```bash
cd API
dotnet run
```
The API will be available at `http://0.0.0.0:3624` (configured in `API/Program.cs:20`).

### 6.3 Running Desktop Applications (Development)

**Client**:
```bash
cd Client
dotnet run
```

**Admin**:
```bash
cd Admin
dotnet run
```

### 6.4 Database Migrations

Run migrations from the root directory:
```bash
# Update database to latest migration
dotnet ef database update --project Libs --startup-project API

# Create new migration
dotnet ef migrations add MigrationName --project Libs --startup-project API
```

Note: There are two database contexts:
- `ScanLabContext` (main application data)
- `ScanLab_LogContext` (Serilog logs)

Both are updated by the publish script.

### 6.5 Publishing Applications

Use the PowerShell scripts in the `Scripts` directory:

**API** (self-contained with database migration):
```powershell
cd Scripts
.\Publish-API.ps1 -environment Production
```

**Client** (builds for Windows and Linux):
```powershell
cd Scripts
.\Publish-Client.ps1
```

**Admin**:
```powershell
cd Scripts
.\Publish-Admin.ps1
```

Published output locations:
- API: `API/Publish/API/`
- Client: `Client/Publish/windows/` and `Client/Publish/linux/`
- Admin: `Admin/Publish/windows/` and `Admin/Publish/linux/`

### 6.6 Important Notes from Codebase

1. **Roll Numbers**: Not unique globally (unique index removed in migration 20250719125701) - they can repeat across different orders
2. **Order IDs**: Must be unique (primary key)
3. **Network Path Handling**: Spaces in directory names are handled via URL decoding in `IOHelpers.NetworkPathConverter.ResolvePath`
4. **File Processing**: When completing a roll, the API:
   - Monitors the scanner's WatchedDir for new files
   - Renames image files to format: `{OrderId}-{RollNumber}-{ImageCount}{extension}`
   - Converts BMP to TIFF using ImageSharp (if apllicable, otherwise image files remain .jpeg) 
   - Updates EXIF data with scanner information (ArtistName, Make, Model)
   - Moves files to DestinationDir with optional weekly subfolder and "Rescans" folder for redos
   - Deletes source files from WatchedDir after processing
5. **Authentication**: API uses JWT tokens; clients store tokens in memory (TokenService) and send with requests
6. **Self-contained Publishing**: All projects are configured for self-contained deployment (includes .NET runtime)
7. **Logging**: Serilog configured to write to PostgreSQL logs table with rich context (timestamp, level, message, properties, etc.). System logs can be viewed in the Admin app.

## 7. Order Processing Workflow (from notes.txt)

1. Order created through Client containing Scanner (selected in "settings"), Customer, Order number, and Rolls
2. Order added to queue on Dashboard
3. Scan tech marks roll as "scanning in progress" (sets parent order to "in progress")
4. Tech scans roll and exports to scanner's directory
5. Tech marks roll as "scanning complete"
6. API service handles file renaming and moving, then marks the individual roll as "processed"
7. The backend then checks if any all the rolls in the associated order are "processed". If they are, backend marks the parent order as "complete".

### 8. API Endpoints Summary

**OrderController** (`/api/Order`):
- POST `/submit` - Create new order
- POST `/complete` - Mark order as complete (when all rolls done)
- DELETE `/cancel/{id}` - Cancel order (not implemented)
- POST `/delete` - Delete order
- POST `/orders` - Get orders with filters

**RollController** (`/api/Roll`):
- POST `/add` - Add roll to order
- POST `/complete` - Process roll (file handling, status update)
- PUT `/updateStatus` - Update roll status (created, scanning in progress, scanning paused, processed)
- POST `/delete` - Delete roll

**ScannerController** (`/api/Scanner`):
- POST `/add` - Add new scanner
- DELETE `/delete/{id}` - Delete scanner
- GET `/scanners` - Get all scanners
- POST `/update` - Update scanner configuration

**AuthController** (`/api/auth`):
- POST `/login` - Authenticate user and return JWT token

**StaffController** (`/api/Staff`):
- GET `/Staff` - Get staff members (with options)

**LogController** (`/api/log`):
- GET `/Get` - Get logs with filtering

**AnalyticsController** (`/api/Analytics`):
- Various endpoints for analytics data (orders per staff, rolls per scanner, etc.)