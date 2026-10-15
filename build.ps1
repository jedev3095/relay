# Builds both Relay executables into .\dist (Windows, x64).
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host 'The .NET 8 SDK is required. Install it with:'
    Write-Host '  winget install Microsoft.DotNet.SDK.8'
    Write-Host 'or download it from https://dotnet.microsoft.com/download/dotnet/8.0'
    exit 1
}

dotnet publish relay.csproj -p:PublishProfile=SingleFile -o out/self-contained
dotnet publish relay.csproj -p:PublishProfile=SingleFileRequiresDotNet -o out/requires-dotnet

New-Item -ItemType Directory -Force dist | Out-Null
Copy-Item out/self-contained/Relay.exe dist/Relay.exe -Force
Copy-Item out/requires-dotnet/Relay.exe dist/Relay-requires-dotnet.exe -Force
Write-Host 'Done: dist\Relay.exe and dist\Relay-requires-dotnet.exe'
