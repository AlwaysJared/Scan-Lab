# Scan Lab - Windows 10 Deployment Guide

This guide walks through deploying the entire Scan Lab system on a Windows 10 machine from scratch. By the end, you will have the PostgreSQL database, the API server, the Admin desktop app, and the Client desktop app all running.

---

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Install PostgreSQL](#2-install-postgresql)
3. [Set Up the Databases](#3-set-up-the-databases)
4. [Install .NET SDK](#4-install-net-sdk)
5. [Clone the Repository](#5-clone-the-repository)
6. [Configure the API](#6-configure-the-api)
7. [Run Database Migrations](#7-run-database-migrations)
8. [Build and Publish the API](#8-build-and-publish-the-api)
9. [Run the API](#9-run-the-api)
10. [Build and Publish the Admin App](#10-build-and-publish-the-admin-app)
11. [Build and Publish the Client App](#11-build-and-publish-the-client-app)
12. [Configure the Admin App](#12-configure-the-admin-app)
13. [Configure the Client App](#13-configure-the-client-app)
14. [Create the First Staff Account](#14-create-the-first-staff-account)
15. [Firewall Configuration](#15-firewall-configuration)
16. [Run the API as a Windows Service (Optional)](#16-run-the-api-as-a-windows-service-optional)
17. [Troubleshooting](#17-troubleshooting)

---

## 1. Prerequisites

Before starting, ensure you have:

- Windows 10 (64-bit)
- Administrator access
- Internet connection (for downloading installers)
- At least 2 GB of free disk space

### Software you will install

| Software | Version | Purpose |
|----------|---------|---------|
| PostgreSQL | 15 or newer | Database server |
| .NET SDK | 9.0 | Building and publishing all projects |
| EF Core CLI | Latest | Running database migrations |
| Git | Latest | Cloning the repository (optional if you copy files manually) |

---

## 2. Install PostgreSQL

### Download

1. Go to https://www.postgresql.org/download/windows/
2. Click **Download the installer** (from EDB)
3. Download the installer for **PostgreSQL 15** or newer (Windows x86-64)

### Install

1. Run the installer as Administrator
2. Click through the setup wizard:
   - **Installation Directory**: Leave as default (`C:\Program Files\PostgreSQL\15`)
   - **Select Components**: Keep all selected (PostgreSQL Server, pgAdmin 4, Stack Builder, Command Line Tools)
   - **Data Directory**: Leave as default
   - **Password**: Set a strong password for the `postgres` superuser. **Remember this password** - you will need it for the API configuration
   - **Port**: Leave as default `5432`
   - **Locale**: Leave as default
3. Click **Next** through remaining screens and **Finish**

### Add PostgreSQL to System PATH

The `psql` command-line tool is used throughout this guide. To avoid typing the full path every time, add PostgreSQL's `bin` directory to your system PATH:

1. Open the Start menu and search for **"Environment Variables"**
2. Click **"Edit the system environment variables"**
3. In the System Properties dialog, click **Environment Variables...**
4. In the **System variables** section (bottom half), scroll down and find the variable named **`Path`** — select it, then click **Edit...**
   - **Important:** Do NOT create a new variable. You must edit the existing `Path` variable
5. In the Edit Environment Variable dialog, click **New** and type: `C:\Program Files\PostgreSQL\17\bin`
   - Adjust the `17` to match your installed PostgreSQL version
6. Click **OK** on all three dialogs to save
7. **Close and reopen** any Command Prompt or PowerShell windows for the change to take effect

### Verify Installation

Open a **new** Command Prompt and run:

```cmd
psql -U postgres -c "SELECT version();"
```

Enter the password you set during installation. You should see the PostgreSQL version printed.

> **If you skip adding to PATH:** You can still use the full path for all `psql` commands in this guide, e.g.:
>
> ```cmd
> "C:\Program Files\PostgreSQL\15\bin\psql.exe" -U postgres -c "SELECT version();"
> ```

---

## 3. Set Up the Databases

Scan Lab uses two PostgreSQL databases:

- **ScanLab** - Main application database (orders, rolls, scanners, profiles, staff)
- **ScanLab_Logs** - Logging database (Serilog structured logs)

### Create the databases

Open a Command Prompt and run:

```cmd
psql -U postgres
```

Enter your postgres password, then run these SQL commands:

```sql
CREATE DATABASE "ScanLab";
CREATE DATABASE "ScanLab_Logs";
\q
```

> **Note:** The database names are case-sensitive. Use the exact casing shown above with double quotes.

---

## 4. Install .NET SDK

The API targets .NET 9 and the desktop apps target .NET 9. You need the .NET 9 SDK.

### Download

1. Go to https://dotnet.microsoft.com/download/dotnet/9.0
2. Download the **.NET SDK 9.0** installer for **Windows x64**

### Install

1. Run the installer
2. Click **Install** and follow the prompts
3. Restart any open Command Prompt / PowerShell windows

### Verify Installation

```cmd
dotnet --version
```

Should show `9.0.x`.

### Install EF Core CLI Tool

The Entity Framework Core CLI tool is required for running database migrations:

```cmd
dotnet tool install --global dotnet-ef
```

Verify:

```cmd
dotnet ef --version
```

---

## 5. Clone the Repository

If you have Git installed:

```cmd
cd C:\
git clone <your-repository-url> "Scan-Lab"
cd Scan-Lab
```

Otherwise, copy the project folder to `C:\Scan-Lab` (or any directory of your choice).

### Verify Project Structure

You should see these directories:

```
C:\Scan-Lab\
  API\
  Admin\
  Client\
  Libs\
  Libs.Tests\
  Scripts\
  Scan Lab.sln
```

---

## 6. Configure the API

### Edit Connection Strings

Open `API\appsettings.json` in a text editor and update the connection strings with your PostgreSQL password:

```json
{
  "ConnectionStrings": {
    "ScanLabDBConnection": "Host=localhost;Database=ScanLab;Username=postgres;Password=YOUR_PASSWORD_HERE",
    "ScanLab_LogsDBConnection": "Host=localhost;Database=ScanLab_Logs;Username=postgres;Password=YOUR_PASSWORD_HERE"
  }
}
```

Replace `YOUR_PASSWORD_HERE` with the password you set during PostgreSQL installation.

### Configure JWT Secret Key

In the same `appsettings.json` file, update the JWT key to a secure random string (at least 32 characters):

```json
{
  "Jwt": {
    "Key": "REPLACE_WITH_A_SECURE_RANDOM_STRING_AT_LEAST_32_CHARS",
    "Issuer": "ScanLabAPI",
    "Audience": "ScanLabClient",
    "ExpiresInMinutes": 480
  }
}
```

> **Important:** The JWT key must be at least 32 characters long. Use a mix of letters, numbers, and symbols. This key is used to sign authentication tokens.

### API Port

The API listens on port `3624` by default. This is configured in `API\Program.cs` line 21:

```csharp
builder.WebHost.UseUrls("http://0.0.0.0:3624");
```

If you need to change the port, update this line before building. The `0.0.0.0` binding means the API accepts connections from any network interface (not just localhost), which is necessary for Client/Admin apps on other machines to connect.

---

## 7. Run Database Migrations

From the root of the project directory (`C:\Scan-Lab`), run:

```cmd
dotnet ef database update -c ScanLabContext --project Libs --startup-project API
```

```cmd
dotnet ef database update -c ScanLab_LogContext --project Libs --startup-project API
```

This creates all the required tables in both databases. You should see output ending with `Done.` for each command.

### Verify Tables Were Created

```cmd
psql -U postgres -d ScanLab -c "\dt"
```

You should see tables including: `Scanners`, `Orders`, `Rolls`, `ScannerProfiles`, `ProfileConfigurations`, `AspNetUsers`, `AspNetRoles`, etc.

---

## 8. Build and Publish the API

### Option A: Using the Publish Script (Recommended)

Open PowerShell and run:

```powershell
cd C:\Scan-Lab\Scripts
.\Publish-API.ps1
```

This builds the API in Release mode, publishes as self-contained, and runs migrations. The output goes to `API\Publish\API\`.

### Option B: Manual Publish

```cmd
cd C:\Scan-Lab
dotnet publish API\API.csproj -c Release --self-contained true -r win-x64 /p:IncludeNativeLibrariesForSelfExtract=true -o API\Publish\API
```

### Published Output

The published API is at `C:\Scan-Lab\API\Publish\API\`. This folder contains everything needed to run the API, including the .NET runtime (self-contained). Key files:

- `API.exe` - The executable to run the API
- `appsettings.json` - Configuration (already has your connection strings)

### Copy to Deployment Location (Optional)

If deploying to a different machine or directory:

```cmd
xcopy /E /I "C:\Scan-Lab\API\Publish\API" "C:\ScanLab-Deploy\API"
```

> **Important:** If you copy to a different location, make sure the `appsettings.json` in the deployed folder has the correct connection strings and JWT key.

---

## 9. Run the API

### Test Run

Navigate to the published API directory and run:

```cmd
cd C:\Scan-Lab\API\Publish\API
API.exe
```

You should see output like:

```
[INF] Now listening on: http://0.0.0.0:3624
[INF] Application started.
```

### Verify the API is Running

Open a web browser and go to:

```
http://localhost:3624/swagger
```

If running in Development mode, you will see the Swagger UI. In Production mode, Swagger is disabled but the API is still running.

You can also test with:

```cmd
curl http://localhost:3624/api/ScannerProfile/strategies
```

> **Note:** This endpoint requires authentication, so you will get a 401 response until you log in. That is expected and confirms the API is running.

Press `Ctrl+C` in the terminal to stop the API.

---

## 10. Build and Publish the Admin App

### Option A: Using the Publish Script

```powershell
cd C:\Scan-Lab\Scripts
.\Publish-Admin.ps1
```

This publishes for both Windows and Linux. The Windows output is at `Admin\Publish\windows\`.

### Option B: Manual Publish (Windows Only)

```cmd
cd C:\Scan-Lab
dotnet publish Admin\Admin.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o Admin\Publish\windows
```

### Published Output

The Admin app is a single self-contained executable:

- `C:\Scan-Lab\Admin\Publish\windows\Admin.exe`

Copy this file (and any accompanying files in the folder) to wherever you want to run it from, for example:

```cmd
xcopy /E /I "C:\Scan-Lab\Admin\Publish\windows" "C:\ScanLab-Deploy\Admin"
```

---

## 11. Build and Publish the Client App

### Option A: Using the Publish Script

```powershell
cd C:\Scan-Lab\Scripts
.\Publish-Client.ps1
```

The Windows output is at `Client\Publish\windows\`.

### Option B: Manual Publish (Windows Only)

```cmd
cd C:\Scan-Lab
dotnet publish Client\Client.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o Client\Publish\windows
```

### Published Output

The Client app is a single self-contained executable:

- `C:\Scan-Lab\Client\Publish\windows\Client.exe`

Copy this file (and any accompanying files in the folder) to wherever you want to run it from:

```cmd
xcopy /E /I "C:\Scan-Lab\Client\Publish\windows" "C:\ScanLab-Deploy\Client"
```

---

## 12. Configure the Admin App

### First Launch

1. Make sure the API is running (see [Step 9](#9-run-the-api))
2. Launch `Admin.exe`
3. The app will open. Navigate to the **Settings** page

### Set the API Address

In the Settings page, set the **API Address** to:

```
http://localhost:3624
```

If the API is running on a different machine, use that machine's IP address:

```
http://192.168.1.100:3624
```

Click the **Test API** button to verify the connection. You should get a success response.

> **How it works:** The Admin app saves the API address to `%APPDATA%\ScanLab\api_config.json`. This persists across app restarts. You only need to set it once.

---

## 13. Configure the Client App

### First Launch

1. Make sure the API is running (see [Step 9](#9-run-the-api))
2. Launch `Client.exe`
3. Navigate to the **Settings** page

### Set the API Address

Same as the Admin app - set the API address in Settings:

```
http://localhost:3624
```

Or the machine's IP address if the API is on a different computer.

Click **Test API** to verify the connection.

> **How it works:** The Client app also saves to `%APPDATA%\ScanLab\api_config.json`. Both apps share the same config file, so if you set the address in one, the other will pick it up automatically.

---

## 14. Create the First Staff Account

The Client app requires authentication. You need to register the first staff account.

### Using the Admin App

1. Open the Admin app
2. Navigate to the Staff management section
3. Register a new staff member with a username, email, and password

### Using the API Directly

If the Admin app doesn't have a registration UI exposed, you can register via the API endpoint directly:

```cmd
curl -X POST http://localhost:3624/api/auth/register ^
  -H "Content-Type: application/json" ^
  -d "{\"userName\": \"admin\", \"email\": \"admin@example.com\", \"password\": \"YourPassword123\"}"
```

**Password requirements:**
- Minimum 8 characters
- Must contain at least one digit
- Uppercase and special characters are not required

### Log In via the Client App

1. Open the Client app
2. Enter your username and password on the login screen
3. You should be logged in and see the Dashboard

---

## 15. Firewall Configuration

If the Client or Admin apps need to connect to the API from other machines on the network, you must allow inbound connections on port `3624`.

### Open Port in Windows Firewall

1. Open **Windows Defender Firewall with Advanced Security** (search "firewall" in the Start menu and select "Windows Defender Firewall with Advanced Security")
2. Click **Inbound Rules** in the left panel
3. Click **New Rule...** in the right panel
4. Select **Port** and click **Next**
5. Select **TCP** and enter `3624` in "Specific local ports"
6. Select **Allow the connection** and click **Next**
7. Check all profiles (Domain, Private, Public) or just the ones applicable to your network
8. Name the rule: `Scan Lab API` and click **Finish**

### Alternative: PowerShell Command

Run PowerShell as Administrator:

```powershell
New-NetFirewallRule -DisplayName "Scan Lab API" -Direction Inbound -Protocol TCP -LocalPort 3624 -Action Allow
```

### Test from Another Machine

From another machine on the same network:

```cmd
curl http://<api-machine-ip>:3624/api/ScannerProfile/strategies
```

---

## 16. Run the API as a Windows Service (Optional)

To keep the API running in the background and start it automatically on boot, you can register it as a Windows Service.

### Using NSSM (Non-Sucking Service Manager)

1. Download NSSM from https://nssm.cc/download
2. Extract to a folder like `C:\Tools\nssm\`
3. Open Command Prompt as Administrator:

```cmd
C:\Tools\nssm\win64\nssm.exe install ScanLabAPI
```

4. In the dialog that appears:
   - **Path**: `C:\ScanLab-Deploy\API\API.exe` (path to your published API executable)
   - **Startup directory**: `C:\ScanLab-Deploy\API\`
   - **Service name**: `ScanLabAPI`
5. Click **Install service**

### Manage the Service

```cmd
:: Start the service
net start ScanLabAPI

:: Stop the service
net stop ScanLabAPI

:: Check status
sc query ScanLabAPI

:: Remove the service (if needed)
C:\Tools\nssm\win64\nssm.exe remove ScanLabAPI confirm
```

The service will now start automatically when Windows boots.

### Alternative: Using sc.exe (Built-in)

If you prefer not to install NSSM, you can use the built-in Windows service manager, but this requires the application to support the Windows Service hosting model. You would need to add the `Microsoft.Extensions.Hosting.WindowsServices` NuGet package to the API project and modify `Program.cs`:

```csharp
builder.Host.UseWindowsService();
```

Then register it:

```cmd
sc create ScanLabAPI binPath= "C:\ScanLab-Deploy\API\API.exe" start= auto
```

> **Note:** This requires code changes. The NSSM approach works without any modifications.

---

## 17. Troubleshooting

### API won't start - "Connection refused" to PostgreSQL

**Symptoms:** API crashes on startup with a database connection error.

**Fix:**
1. Verify PostgreSQL is running:
   ```cmd
   sc query postgresql-x64-15
   ```
   If stopped, start it:
   ```cmd
   net start postgresql-x64-15
   ```
2. Verify connection strings in `appsettings.json` have the correct password
3. Verify the databases exist:
   ```cmd
   psql -U postgres -l
   ```

### API starts but Client/Admin can't connect

**Symptoms:** "Test API" fails in the desktop apps.

**Fix:**
1. Verify the API is running and listening:
   ```cmd
   netstat -an | findstr 3624
   ```
   Should show `LISTENING` on port 3624.
2. Verify the API address in the desktop app Settings is correct
3. If connecting from another machine, check the [firewall configuration](#15-firewall-configuration)
4. Try pinging the API machine from the Client machine

### "Invalid JWT" or authentication errors

**Symptoms:** Login fails or API returns 401 for authenticated requests.

**Fix:**
1. Verify the JWT Key in `appsettings.json` is at least 32 characters
2. If you changed the JWT Key after creating accounts, existing tokens are invalidated. Users must log in again
3. Check that the API's system clock is correct (JWT tokens are time-sensitive)

### Database migration fails

**Symptoms:** `dotnet ef database update` returns errors.

**Fix:**
1. Ensure the databases `ScanLab` and `ScanLab_Logs` exist (see [Step 3](#3-set-up-the-databases))
2. Ensure connection strings in `appsettings.json` are correct
3. Run from the root project directory (where `Scan Lab.sln` is located)
4. Verify EF Core tools are installed: `dotnet ef --version`

### Desktop apps crash on launch

**Symptoms:** Admin.exe or Client.exe closes immediately.

**Fix:**
1. Check if the app was published as self-contained. If not, install the .NET 9 Desktop Runtime from https://dotnet.microsoft.com/download/dotnet/9.0
2. Run from Command Prompt to see error output:
   ```cmd
   cd C:\ScanLab-Deploy\Client
   Client.exe
   ```
3. Check Windows Event Viewer for application errors

### Scanner export directories not found

**Symptoms:** "Scanner's export directory not found" when processing rolls.

**Fix:**
1. Verify the scanner's Watched Directory and Destination Directory exist and are accessible from the machine running the API
2. If using network shares, ensure they are mapped or accessible via UNC path
3. Check directory permissions - the account running the API must have read/write access

### Port 3624 already in use

**Symptoms:** API fails to start with "address already in use" error.

**Fix:**
1. Find what's using the port:
   ```cmd
   netstat -ano | findstr 3624
   ```
2. Kill the process if it's a stale API instance:
   ```cmd
   taskkill /PID <pid> /F
   ```
3. Or change the API port in `Program.cs` line 21

---

## Quick Reference

| Component | Default Location | Port | Config File |
|-----------|-----------------|------|-------------|
| PostgreSQL | `C:\Program Files\PostgreSQL\15\` | 5432 | `pg_hba.conf` |
| API | `API\Publish\API\` | 3624 | `appsettings.json` |
| Admin App | `Admin\Publish\windows\` | N/A | `%APPDATA%\ScanLab\api_config.json` |
| Client App | `Client\Publish\windows\` | N/A | `%APPDATA%\ScanLab\api_config.json` |

### Useful Commands

```cmd
:: Check if PostgreSQL is running
sc query postgresql-x64-15

:: Check if API port is in use
netstat -an | findstr 3624

:: Run API in development mode (with Swagger)
cd C:\Scan-Lab\API
set ASPNETCORE_ENVIRONMENT=Development
dotnet run

:: Run database migrations
cd C:\Scan-Lab
dotnet ef database update -c ScanLabContext --project Libs --startup-project API
dotnet ef database update -c ScanLab_LogContext --project Libs --startup-project API

:: Build entire solution
dotnet build "Scan Lab.sln"
```
