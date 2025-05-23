# Define the path to the project and the publish profile
$projectPath = ".\..\API\API.csproj"
$publishProfile = "Production"
$outputDir = ".\..\API\Publish\API"
$dbCopyDir = ".\..\API\Publish\DB\"

# Run the publish command
dotnet publish $projectPath `
    -c Release `
    -p:PublishProfile=$publishProfile `
    --self-contained true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    -o $outputDir

Remove-Item ".\..\DB\ScanLab.*"

Push-Location ".."
dotnet ef database update --project Libs --startup-project API
Pop-Location   
 
New-Item -Path $dbCopyDir -ItemType Directory
Copy-Item ".\..\DB\ScanLab.db" -Destination $dbCopyDir

Write-Host "Publish completed successfully."
