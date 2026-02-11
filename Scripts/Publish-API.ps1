param(
    [string]$environment
)

Write-Host "Publishing API for environment: '$environment'"

# Define the path to the project and the publish profile
$projectPath = ".\..\API\API.csproj"
$publishProfile = "Production"
$outputDir = ".\..\API\Publish\API"
# $dbCopyDir = ".\..\API\Publish\DB\"

# Run the publish command
dotnet publish $projectPath `
    -c Release `
    -p:PublishProfile=$publishProfile `
    --self-contained true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    -o $outputDir

# Remove-Item ".\..\DB\ScanLab.*"

$env:ASPNETCORE_ENVIRONMENT = "$environment"

Write-Host "Updating databases using Entity Framework Core migrations..."
Push-Location ".."
dotnet ef database update -c ScanLabContext --project Libs --startup-project API
dotnet ef database update -c ScanLab_LogContext --project Libs --startup-project API
Pop-Location   
 
# New-Item -Path $dbCopyDir -ItemType Directory
# Copy-Item ".\..\DB\ScanLab.db" -Destination $dbCopyDir

Write-Host "Publish completed successfully."
