# Define the path to the project and the publish profile
$projectPath = ".\..\API\API.csproj"
$publishProfile = "Production"
$outputDir = ".\..\API\Publish"

# Run the publish command
dotnet publish $projectPath `
    -c Release `
    -p:PublishProfile=$publishProfile `
    --self-contained true `
    -o $outputDir

Write-Host "Publish completed successfully."
