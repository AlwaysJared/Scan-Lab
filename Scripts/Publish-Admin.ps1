# Set variables
$projectPath = ".\..\Admin\Admin.csproj" # Adjust this path
$outputDir = ".\..\Admin\Publish"    # Base publish output folder
$configuration = "Release"

# Windows publish
dotnet publish $projectPath `
    -c $configuration `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    -o "$outputDir\windows"

# Linux publish
dotnet publish $projectPath `
    -c $configuration `
    -r linux-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    -o "$outputDir\linux"

Write-Host "Publish completed for Windows and Linux."
