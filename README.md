# Relay

Relay is a Windows macro recorder and player with an autoclicker, editable hotkeys, and a WebView interface.

## Download

Tagged releases (`v*`) are built by GitHub Actions and attached to the repository's Releases page. The `publish` folder also contains two single-file executables:

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

Or run `.\build.ps1` from PowerShell to build both executables into `dist\`.

## Project layout

| Path | Contents |
| --- | --- |
| `src/` | Application source, one file per class (`MainForm`, `MacroPlayer`, `MacroRecorder`, `NativeMethods`, ...). |
| `GlobalUsings.cs` | Shared `using` directives for the project. |
| `ui/index.html` | WebView page, embedded as a resource. |
| `.github/workflows/build.yml` | Builds on every push and pull request; publishes both executables to a GitHub release when a `v*` tag is pushed. |
