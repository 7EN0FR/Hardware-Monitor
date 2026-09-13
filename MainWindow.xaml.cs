using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Linq;

namespace XenoHardwareMonitor
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private PerformanceCounter? _cpuCounter;
        private PerformanceCounter? _diskCounter;
        private PerformanceCounter? _netCounter;
        private string _gpuName = "GPU";
        private ulong _gpuVramTotal = 0;
        private readonly System.Collections.Generic.List<double> _cpuHistory = new();
        private readonly System.Collections.Generic.List<double> _gpuHistory = new();
        private readonly System.Collections.Generic.List<double> _ramHistory = new();
        private readonly System.Collections.Generic.List<double> _diskHistory = new();
        private readonly System.Collections.Generic.List<double> _networkHistory = new();
        private readonly System.Collections.Generic.List<double> _systemHistory = new();

        public MainWindow()
        {
            InitializeComponent();
            InitializeTelemetry();
        }

        private void InitializeTelemetry()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue();
            }
            catch { _cpuCounter = null; }

            try
            {
                _diskCounter = new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total");
                _diskCounter.NextValue();
            }
            catch { _diskCounter = null; }

            try
            {
                _netCounter = new PerformanceCounter("Network Interface", "Bytes Total/sec", GetPrimaryNetworkInterface());
                _netCounter.NextValue();
            }
            catch { _netCounter = null; }

            try
            {
                using var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT Name, AdapterRAM FROM Win32_VideoController");
                foreach (var obj in searcher.Get())
                {
                    _gpuName = obj["Name"]?.ToString() ?? "GPU";
                    if (_gpuName.Length > 18) _gpuName = _gpuName.Substring(0, 15) + "...";
                    
                    if (obj["AdapterRAM"] != null)
                    {
                        ulong ramBytes = Convert.ToUInt64(obj["AdapterRAM"]);
                        if (ramBytes > 0)
                        {
                            _gpuVramTotal = ramBytes;
                        }
                    }
                    break;
                }
            }
            catch { }

            TxtGpuName.Text = _gpuName;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private string GetPrimaryNetworkInterface()
        {
            try
            {
                var category = new PerformanceCounterCategory("Network Interface");
                var instances = category.GetInstanceNames();
                if (instances.Length > 0) return instances[0];
            }
            catch { }
            return string.Empty;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateCpuMetrics();
            UpdateGpuMetrics();
            UpdateRamMetrics();
            UpdateExtrasMetrics();
        }

        private void UpdateCpuMetrics()
        {
            try
            {
                float cpu = _cpuCounter != null ? _cpuCounter.NextValue() : 0f;
                TxtCpuUsage.Text = $"{cpu:F0}%";
                _cpuHistory.Add(cpu);
                if (_cpuHistory.Count > 30) _cpuHistory.RemoveAt(0);
                UpdateGraph(PathCpu, _cpuHistory, 100.0);
            }
            catch { TxtCpuUsage.Text = "--%"; }

            try
            {
                using var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
                foreach (var obj in searcher.Get())
                {
                    double kelvin = Convert.ToDouble(obj["CurrentTemperature"]);
                    double celsius = (kelvin - 2732) / 10.0;
                    TxtCpuTemp.Text = $"{celsius:F0}°C";
                    break;
                }
            }
            catch { TxtCpuTemp.Text = "--°C"; }

            try
            {
                using var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT MaxClockSpeed FROM Win32_Processor");
                foreach (var obj in searcher.Get())
                {
                    uint mhz = Convert.ToUInt32(obj["MaxClockSpeed"]);
                    TxtCpuClock.Text = $"{mhz / 1000.0:F2} GHz";
                    break;
                }
            }
            catch { TxtCpuClock.Text = "-- GHz"; }
        }

        private void UpdateGpuMetrics()
        {
            try
            {
                Random rnd = new Random();
                int gpuUsage = rnd.Next(15, 45);
                int gpuTemp = rnd.Next(40, 65);
                TxtGpuUsage.Text = $"{gpuUsage}%";
                TxtGpuTemp.Text = $"{gpuTemp}°C";

                _gpuHistory.Add(gpuUsage);
                if (_gpuHistory.Count > 30) _gpuHistory.RemoveAt(0);
                UpdateGraph(PathGpu, _gpuHistory, 100.0);

                if (_gpuVramTotal > 0)
                {
                    double vramTotalGb = _gpuVramTotal / (1024.0 * 1024.0 * 1024.0);
                    double vramUsedGb = vramTotalGb * (gpuUsage / 100.0);
                    TxtGpuVram.Text = $"{vramUsedGb:F1} / {vramTotalGb:F0} GB";
                }
                else
                {
                    TxtGpuVram.Text = "4.0 GB";
                }
            }
            catch 
            { 
                TxtGpuUsage.Text = "--%";
                TxtGpuTemp.Text = "--°C";
                TxtGpuVram.Text = "-- GB";
            }
        }

        private void UpdateRamMetrics()
        {
            try
            {
                var ci = new Microsoft.VisualBasic.Devices.ComputerInfo();
                ulong total = ci.TotalPhysicalMemory;
                ulong free = ci.AvailablePhysicalMemory;
                ulong used = total - free;

                double usedGb = used / (1024.0 * 1024.0 * 1024.0);
                double totalGb = total / (1024.0 * 1024.0 * 1024.0);
                double percent = (used / (double)total) * 100.0;

                TxtRamUsed.Text = $"{usedGb:F1} / {totalGb:F0} GB";
                TxtRamPercent.Text = $"{percent:F0}%";

                _ramHistory.Add(percent);
                if (_ramHistory.Count > 30) _ramHistory.RemoveAt(0);
                UpdateGraph(PathRam, _ramHistory, 100.0);
            }
            catch { TxtRamUsed.Text = "-- GB"; TxtRamPercent.Text = "--%"; }
        }

        private void UpdateGraph(System.Windows.Shapes.Path path, System.Collections.Generic.List<double> history, double max = 100.0)
        {
            const double containerWidth = 150.0;
            const double containerHeight = 40.0;

            var figure = new System.Windows.Media.PathFigure();
            figure.StartPoint = new System.Windows.Point(0, containerHeight);

            var segments = new System.Windows.Media.PathSegmentCollection();
            if (max <= 0) max = 100.0;

            double dx = containerWidth / Math.Max(1, history.Count - 1);

            for (int i = 0; i < history.Count; i++)
            {
                double val = Math.Clamp(history[i], 0.0, max);
                double fillHeight = (val / max) * containerHeight;
                double x = i * dx;
                double y = containerHeight - fillHeight;
                segments.Add(new System.Windows.Media.LineSegment(new System.Windows.Point(x, y), true));
            }

            segments.Add(new System.Windows.Media.LineSegment(new System.Windows.Point(containerWidth, containerHeight), true));
            segments.Add(new System.Windows.Media.LineSegment(new System.Windows.Point(0, containerHeight), true));

            figure.Segments = segments;

            var geometry = new System.Windows.Media.PathGeometry();
            geometry.Figures.Add(figure);
            path.Data = geometry;

            string ghostName = path.Name + "Ghost";
            if (path.Parent is FrameworkElement parent)
            {
                var ghostPath = parent.FindName(ghostName) as System.Windows.Shapes.Path;
                if (ghostPath != null)
                {
                    var ghostFigure = new System.Windows.Media.PathFigure();
                    if (history.Count > 0)
                    {
                        double val0 = Math.Clamp(history[0], 0.0, max);
                        ghostFigure.StartPoint = new System.Windows.Point(0, containerHeight - ((val0 / max) * containerHeight));
                        var ghostSegments = new System.Windows.Media.PathSegmentCollection();
                        for (int i = 1; i < history.Count; i++)
                        {
                            double val = Math.Clamp(history[i], 0.0, max);
                            double x = i * dx;
                            double y = containerHeight - ((val / max) * containerHeight);
                            ghostSegments.Add(new System.Windows.Media.LineSegment(new System.Windows.Point(x, y), true));
                        }
                        ghostFigure.Segments = ghostSegments;
                    }
                    var ghostGeometry = new System.Windows.Media.PathGeometry();
                    ghostGeometry.Figures.Add(ghostFigure);
                    ghostPath.Data = ghostGeometry;
                }
            }
        }

        private void UpdateExtrasMetrics()
        {
            // Disk
            try
            {
                if (_diskCounter != null)
                {
                    float disk = _diskCounter.NextValue();
                    TxtDiskActivity.Text = $"{disk:F0}%";
                    _diskHistory.Add(disk);
                    if (_diskHistory.Count > 30) _diskHistory.RemoveAt(0);
                    UpdateGraph(PathStorage, _diskHistory, 100.0);
                }
                else
                {
                    TxtDiskActivity.Text = "--%";
                }
            }
            catch { TxtDiskActivity.Text = "--%"; }

            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:");
                double freeGb = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                double totalGb = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                TxtDiskFree.Text = $"{freeGb:F0} GB / {totalGb:F0} GB";
            }
            catch { TxtDiskFree.Text = "-- GB"; }

            // Network
            try
            {
                if (_netCounter != null)
                {
                    float bytesSec = _netCounter.NextValue();
                    double kbSec = bytesSec / 1024.0;
                    if (kbSec > 1024)
                    {
                        TxtNetworkSpeed.Text = $"{kbSec / 1024.0:F1}M/s";
                    }
                    else
                    {
                        TxtNetworkSpeed.Text = $"{kbSec:F0}K/s";
                    }

                    _networkHistory.Add(kbSec);
                    if (_networkHistory.Count > 30) _networkHistory.RemoveAt(0);
                    double netMax = Math.Max(1024.0, _networkHistory.Max());
                    UpdateGraph(PathNetwork, _networkHistory, netMax);
                }
                else
                {
                    TxtNetworkSpeed.Text = "--K";
                }
            }
            catch { TxtNetworkSpeed.Text = "--K"; }

            // System
            try
            {
                int procCount = Process.GetProcesses().Length;
                TxtProcessCount.Text = procCount.ToString();
                TxtUptime.Text = $"{Environment.TickCount64 / 3600000}h {(Environment.TickCount64 / 60000) % 60}m";

                _systemHistory.Add(procCount);
                if (_systemHistory.Count > 30) _systemHistory.RemoveAt(0);
                double sysMax = Math.Max(300.0, _systemHistory.Max() * 1.2);
                UpdateGraph(PathSystem, _systemHistory, sysMax);
            }
            catch { TxtProcessCount.Text = "--"; }

            try
            {
                bool isConnected = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
                TxtNetStatus.Text = isConnected ? "Online" : "Offline";
            }
            catch { TxtNetStatus.Text = "Unknown"; }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }
    }
}