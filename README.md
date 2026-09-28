# Relay

Relay is a Windows macro recorder and player with an autoclicker, editable hotkeys, and a WebView interface.

## Download

The `publish` folder contains two single-file executables:

| File | Requirements |
| --- | --- |
| `Relay.exe` | Includes the .NET runtime. |
| `Relay-requires-dotnet.exe` | Smaller download; requires the .NET 8 Desktop Runtime (x64). |

Both use Microsoft Edge WebView2 Runtime for the interface. Windows 11 normally includes it. Settings are saved under `%APPDATA%\Relay\settings.json`; macro files are saved wherever you choose.

## Build from source

Use the .NET 8 SDK on Windows. The project includes all application source, the embedded WebView page, icon, and publish profiles. NuGet restores the WebView2 package when building.

```powershell
dotnet build relay.csproj -c Release
dotnet publish relay.csproj -p:PublishProfile=SingleFile
dotnet publish relay.csproj -p:PublishProfile=SingleFileRequiresDotNet
```

The two publish profiles create single-file Windows x64 builds. Save each output separately because both builds use the filename `Relay.exe` by default.
