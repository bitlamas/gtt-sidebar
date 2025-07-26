using gtt_sidebar.Core.Interfaces;
using gtt_sidebar.Core.Managers;
using gtt_sidebar.Core.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace gtt_sidebar.Widgets.SystemMonitor
{
    public partial class SystemMonitorWidget : UserControl, IWidget
    {
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _ramCounter;
        private DispatcherTimer _timer;
        private SystemMonitorSettings _settings;

        // current data values
        private double _currentCpu = 0.0;
        private double _currentRam = 0.0;
        private double _currentPing = 0.0;
        private double _totalRamGB = 0.0;

        // historical data for averages (2-second intervals)
        private Queue<double> _cpuHistory = new Queue<double>();
        private Queue<double> _ramHistory = new Queue<double>();
        private Queue<double> _pingHistory = new Queue<double>();

        // average calculation constants
        private const int FIVE_MINUTE_SAMPLES = 150;  // 5 minutes ÷ 2 seconds = 150 samples
        private const int ONE_HOUR_SAMPLES = 1800;    // 60 minutes ÷ 2 seconds = 1800 samples

        public string Name => "System Monitor";

        public SystemMonitorWidget()
        {
            InitializeComponent();
        }

        public UserControl GetControl()
        {
            return this;
        }

        public async Task InitializeAsync()
        {
            LoadSettings();
            SharedResourceManager.Instance.SettingsChanged += OnSettingsChanged;
            // initialize CPU performance counter
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue(); // first call returns 0, so initialize
                System.Diagnostics.Debug.WriteLine("SystemMonitor: CPU counter initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SystemMonitor: Failed to initialize CPU counter: {ex.Message}");
                _cpuCounter = null;
            }

            // initialize RAM performance counter
            try
            {
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                _totalRamGB = computerInfo.TotalPhysicalMemory / (1024.0 * 1024.0 * 1024.0);
                System.Diagnostics.Debug.WriteLine($"SystemMonitor: RAM counter initialized. Total RAM: {_totalRamGB:F1} GB");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SystemMonitor: Failed to initialize RAM counter: {ex.Message}");
                _ramCounter = null;
            }

            // create timer with settings-based frequency
            UpdateTimerFrequency();

            await Task.CompletedTask;
        }

        private void OnSettingsChanged(SettingsData newSettings)
        {
            _settings = newSettings.SystemMonitor;
            UpdateTimerFrequency(); // restart timer with new frequency
            UpdateDisplay(); // apply new color thresholds immediately
            System.Diagnostics.Debug.WriteLine($"SystemMonitor: Settings changed via SharedResourceManager");
        }
        private void UpdateTimerFrequency()
        {
            // stop existing timer
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }

            // create new timer with current settings frequency
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(_settings.UpdateFrequencySeconds);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            System.Diagnostics.Debug.WriteLine($"SystemMonitor: Timer frequency updated to {_settings.UpdateFrequencySeconds} seconds");
        }

        public void RefreshSettings()
        {
            LoadSettings();
            UpdateTimerFrequency(); // restart timer with new frequency
            UpdateDisplay(); // apply new color thresholds immediately

            System.Diagnostics.Debug.WriteLine($"SystemMonitor: Settings refreshed - CPU: {_settings.CpuThreshold}%, RAM: {_settings.RamThreshold}%, Ping: {_settings.PingThreshold}ms");
        }
        private void LoadSettings()
        {
            try
            {
                // Use cached settings instead of file access
                _settings = SharedResourceManager.Instance.Settings.SystemMonitor;
                System.Diagnostics.Debug.WriteLine($"SystemMonitor: Loaded cached settings - CPU: {_settings.CpuThreshold}%, RAM: {_settings.RamThreshold}%, Ping: {_settings.PingThreshold}ms, Freq: {_settings.UpdateFrequencySeconds}s");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SystemMonitor: Error loading cached settings, using defaults: {ex.Message}");
                _settings = new SystemMonitorSettings(); // use defaults
            }
        }


        private async void Timer_Tick(object sender, EventArgs e)
        {
            // get real data from all sources
            GetRealCpuData();
            GetRealRamData();
            await GetRealPingDataAsync();

            // store data for averages (only store valid readings)
            StoreHistoricalData();

            UpdateDisplay();
        }

        private void GetRealCpuData()
        {
            try
            {
                if (_cpuCounter != null)
                {
                    _currentCpu = _cpuCounter.NextValue();
                }
                else
                {
                    _currentCpu = -1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SystemMonitor: Error reading CPU: {ex.Message}");
                _currentCpu = -1;
            }
        }

        private void GetRealRamData()
        {
            try
            {
                if (_ramCounter != null && _totalRamGB > 0)
                {
                    var availableRamMB = _ramCounter.NextValue();
                    var availableRamGB = availableRamMB / 1024.0;
                    var usedRamGB = _totalRamGB - availableRamGB;
                    _currentRam = (usedRamGB / _totalRamGB) * 100.0;
                }
                else
                {
                    _currentRam = -1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SystemMonitor: Error reading RAM: {ex.Message}");
                _currentRam = -1;
            }
        }

        private async Task GetRealPingDataAsync()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync("8.8.8.8", 1000); // 1 second timeout

                    if (reply.Status == IPStatus.Success)
                    {
                        _currentPing = reply.RoundtripTime;
                    }
                    else
                    {
                        _currentPing = -1; // will display as N/A
                        System.Diagnostics.Debug.WriteLine($"SystemMonitor: Ping failed: {reply.Status}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SystemMonitor: Error pinging: {ex.Message}");
                _currentPing = -1; // will display as N/A
            }
        }

        private void StoreHistoricalData()
        {
            // only store valid readings (>= 0)
            if (_currentCpu >= 0)
            {
                _cpuHistory.Enqueue(_currentCpu);
                // keep only the last hour of data
                while (_cpuHistory.Count > ONE_HOUR_SAMPLES)
                {
                    _cpuHistory.Dequeue();
                }
            }

            if (_currentRam >= 0)
            {
                _ramHistory.Enqueue(_currentRam);
                while (_ramHistory.Count > ONE_HOUR_SAMPLES)
                {
                    _ramHistory.Dequeue();
                }
            }

            if (_currentPing >= 0)
            {
                _pingHistory.Enqueue(_currentPing);
                while (_pingHistory.Count > ONE_HOUR_SAMPLES)
                {
                    _pingHistory.Dequeue();
                }
            }
        }

        private double CalculateAverage(Queue<double> data, int sampleCount)
        {
            if (data.Count == 0) return -1; // no data available

            // take the last N samples (or all if we have fewer)
            var samplesToUse = Math.Min(sampleCount, data.Count);
            var recentSamples = data.Skip(data.Count - samplesToUse);

            return recentSamples.Average();
        }

        private void UpdateDisplay()
        {
            try
            {
                // update CPU usage (%)
                if (_currentCpu >= 0)
                {
                    CpuValue.Text = $"{_currentCpu:F0}%";
                }
                else
                {
                    CpuValue.Text = "N/A";
                }

                // update RAM usage (%)
                if (_currentRam >= 0)
                {
                    RamValue.Text = $"{_currentRam:F0}%";
                }
                else
                {
                    RamValue.Text = "N/A";
                }

                // update Ping latency (ms)
                if (_currentPing >= 0)
                {
                    PingValue.Text = $"{_currentPing:F0}ms";
                }
                else
                {
                    PingValue.Text = "N/A";
                }

                // update tooltips with real averages
                if (_currentCpu >= 0)
                {
                    var cpu5Min = CalculateAverage(_cpuHistory, FIVE_MINUTE_SAMPLES);
                    var cpu1Hour = CalculateAverage(_cpuHistory, ONE_HOUR_SAMPLES);

                    if (cpu5Min >= 0 && cpu1Hour >= 0)
                    {
                        CpuValue.ToolTip = $"Current: {_currentCpu:F1}%\nAvg 5m: {cpu5Min:F1}%\nAvg 1h: {cpu1Hour:F1}%";
                    }
                    else if (cpu5Min >= 0)
                    {
                        CpuValue.ToolTip = $"Current: {_currentCpu:F1}%\nAvg 5m: {cpu5Min:F1}%\nAvg 1h: (collecting data...)";
                    }
                    else
                    {
                        CpuValue.ToolTip = $"Current: {_currentCpu:F1}%\nAvg 5m: (collecting data...)\nAvg 1h: (collecting data...)";
                    }
                }
                else
                {
                    CpuValue.ToolTip = "CPU monitoring unavailable";
                }

                if (_currentRam >= 0)
                {
                    var ram5Min = CalculateAverage(_ramHistory, FIVE_MINUTE_SAMPLES);
                    var ram1Hour = CalculateAverage(_ramHistory, ONE_HOUR_SAMPLES);

                    if (ram5Min >= 0 && ram1Hour >= 0)
                    {
                        RamValue.ToolTip = $"Current: {_currentRam:F1}%\nAvg 5m: {ram5Min:F1}%\nAvg 1h: {ram1Hour:F1}%";
                    }
                    else if (ram5Min >= 0)
                    {
                        RamValue.ToolTip = $"Current: {_currentRam:F1}%\nAvg 5m: {ram5Min:F1}%\nAvg 1h: (collecting data...)";
                    }
                    else
                    {
                        RamValue.ToolTip = $"Current: {_currentRam:F1}%\nAvg 5m: (collecting data...)\nAvg 1h: (collecting data...)";
                    }
                }
                else
                {
                    RamValue.ToolTip = "RAM monitoring unavailable";
                }

                if (_currentPing >= 0)
                {
                    var ping5Min = CalculateAverage(_pingHistory, FIVE_MINUTE_SAMPLES);
                    var ping1Hour = CalculateAverage(_pingHistory, ONE_HOUR_SAMPLES);

                    if (ping5Min >= 0 && ping1Hour >= 0)
                    {
                        PingValue.ToolTip = $"Current: {_currentPing:F1}ms\nAvg 5m: {ping5Min:F1}ms\nAvg 1h: {ping1Hour:F1}ms";
                    }
                    else if (ping5Min >= 0)
                    {
                        PingValue.ToolTip = $"Current: {_currentPing:F1}ms\nAvg 5m: {ping5Min:F1}ms\nAvg 1h: (collecting data...)";
                    }
                    else
                    {
                        PingValue.ToolTip = $"Current: {_currentPing:F1}ms\nAvg 5m: (collecting data...)\nAvg 1h: (collecting data...)";
                    }
                }
                else
                {
                    PingValue.ToolTip = "Network ping unavailable";
                }

                // color coding using settings-based thresholds
                if (_currentCpu >= 0)
                {
                    CpuValue.Foreground = _currentCpu > _settings.CpuThreshold ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Black;
                }
                else
                {
                    CpuValue.Foreground = System.Windows.Media.Brushes.Gray;
                }

                if (_currentRam >= 0)
                {
                    RamValue.Foreground = _currentRam > _settings.RamThreshold ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Black;
                }
                else
                {
                    RamValue.Foreground = System.Windows.Media.Brushes.Gray;
                }

                if (_currentPing >= 0)
                {
                    PingValue.Foreground = _currentPing > _settings.PingThreshold ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Black;
                }
                else
                {
                    PingValue.Foreground = System.Windows.Media.Brushes.Gray;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SystemMonitorWidget UpdateDisplay error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            SharedResourceManager.Instance.SettingsChanged -= OnSettingsChanged;

            // clean up timer
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }

            // clean up the performance counters
            if (_cpuCounter != null)
            {
                _cpuCounter.Dispose();
                _cpuCounter = null;
            }

            if (_ramCounter != null)
            {
                _ramCounter.Dispose();
                _ramCounter = null;
            }
        }
    }
}