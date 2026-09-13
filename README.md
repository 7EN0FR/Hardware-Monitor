# XENO Hardware Monitor

A modern, dark-themed hardware monitoring application built with WPF and C# (.NET 8). Inspired by the MSI Afterburner aesthetic with cyan, orange, and green neon accents.

## Screenshot
![Hardware Monitor UI](screenshot.png)

## Features
- Real-time CPU usage, temperature, and clock speed monitoring
- GPU usage, temperature, and VRAM utilization
- Memory usage and percentage
- Disk activity, network speed, and system uptime
- Custom borderless window with dark theme
- 1-second polling interval for smooth updates
- Graceful fallback values if hardware sensors are inaccessible

## Prerequisites
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) or newer
- Windows 10/11 (64-bit)

## Building and Running

### Option 1: Using .NET CLI (Recommended)
1. Open terminal in the project directory
2. Restore dependencies:
   ```bash
   dotnet restore
   ```
3. Build the project:
   ```bash
   dotnet build
   ```
4. Run the application:
   ```bash
   dotnet run --project XenoHardwareMonitor.csproj
   ```

### Option 2: Using Visual Studio Code
1. Install the C# extension for VS Code
2. Press F5 to start debugging (or Ctrl+F5 for without debugging)
3. Alternatively, run the build task via Terminal > Run Task

## Project Structure
- `App.xaml` - Application resources and theme definitions
- `MainWindow.xaml` - UI layout with dark theme and telemetry panels
- `MainWindow.xaml.cs` - Hardware monitoring logic using PerformanceCounter and WMI
- `XenoHardwareMonitor.csproj` - Project file with NuGet references

## Design Notes
- Dark background: `#121214`
- Accent colors: Cyan `#00E5FF`, Orange `#FF9100`, Green `#00E676`
- Borderless window with custom minimize/close buttons
- Uniform grids for consistent layout
- Fallback values (-- or simulated) prevent crashes when sensors are blocked
- DispatcherTimer updates all metrics every 1000ms

## Customization
- Adjust polling interval in `MainWindow.xaml.cs` (currently 1000ms)
- Modify colors in `App.xaml`
- Change fallback values in telemetry update methods
- Add actual GPU monitoring by integrating LibreHardwareMonitorLib NuGet package
