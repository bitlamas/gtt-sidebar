using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Threading;
using gtt_sidebar.Core.Settings;
using gtt_sidebar.Core.Interfaces;

namespace gtt_sidebar.Core.Managers
{
    /// <summary>
    /// Centralized resource manager for HTTP clients, timers, and settings
    /// </summary>
    public sealed class SharedResourceManager : IDisposable
    {
        private static readonly Lazy<SharedResourceManager> _instance = new Lazy<SharedResourceManager>(() => new SharedResourceManager());
        public static SharedResourceManager Instance => _instance.Value;

        private HttpClient _httpClient;
        private DispatcherTimer _masterTimer;
        private SettingsData _cachedSettings;
        private DateTime _lastSettingsLoad = DateTime.MinValue;
        private readonly List<ITimerSubscriber> _timerSubscribers = new List<ITimerSubscriber>();
        private readonly Dictionary<ITimerSubscriber, DateTime> _lastUpdateTimes = new Dictionary<ITimerSubscriber, DateTime>();
        private bool _disposed = false;

        // Events
        public event Action<SettingsData> SettingsChanged;
        public event Action MasterTimerTick;

        private SharedResourceManager()
        {
            InitializeMasterTimer();
        }

        /// <summary>
        /// Shared HttpClient with proper configuration
        /// </summary>
        public HttpClient HttpClient
        {
            get
            {
                if (_httpClient == null)
                {
                    _httpClient = CreateHttpClient();
                }
                return _httpClient;
            }
        }

        /// <summary>
        /// Cached settings with automatic refresh
        /// </summary>
        public SettingsData Settings
        {
            get
            {
                // Refresh settings every 30 seconds max
                if (_cachedSettings == null || (DateTime.Now - _lastSettingsLoad).TotalSeconds > 30)
                {
                    RefreshSettings();
                }
                return _cachedSettings;
            }
        }

        /// <summary>
        /// Subscribe to timer events with custom intervals
        /// </summary>
        public void Subscribe(ITimerSubscriber subscriber)
        {
            if (!_timerSubscribers.Contains(subscriber))
            {
                _timerSubscribers.Add(subscriber);
                _lastUpdateTimes[subscriber] = DateTime.Now;
                System.Diagnostics.Debug.WriteLine($"SharedResourceManager: Subscribed {subscriber.GetType().Name} with {subscriber.UpdateInterval} interval");
            }
        }

        /// <summary>
        /// Unsubscribe from timer events
        /// </summary>
        public void Unsubscribe(ITimerSubscriber subscriber)
        {
            _timerSubscribers.Remove(subscriber);
            _lastUpdateTimes.Remove(subscriber);
            System.Diagnostics.Debug.WriteLine($"SharedResourceManager: Unsubscribed {subscriber.GetType().Name}");
        }

        /// <summary>
        /// Force refresh settings from storage
        /// </summary>
        public void RefreshSettings()
        {
            try
            {
                var newSettings = SettingsStorage.LoadSettings();
                var settingsChanged = _cachedSettings == null ||
                                    !AreSettingsEqual(_cachedSettings, newSettings);

                _cachedSettings = newSettings;
                _lastSettingsLoad = DateTime.Now;

                if (settingsChanged)
                {
                    System.Diagnostics.Debug.WriteLine("SharedResourceManager: Settings refreshed and changed");
                    SettingsChanged?.Invoke(_cachedSettings);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SharedResourceManager: Error refreshing settings: {ex.Message}");
                // Keep existing cached settings if reload fails
            }
        }

        /// <summary>
        /// Update settings and notify subscribers
        /// </summary>
        public void UpdateSettings(SettingsData newSettings)
        {
            _cachedSettings = newSettings;
            _lastSettingsLoad = DateTime.Now;
            SettingsStorage.SaveSettings(newSettings);
            SettingsChanged?.Invoke(_cachedSettings);
            System.Diagnostics.Debug.WriteLine("SharedResourceManager: Settings updated");
        }

        private HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            client.Timeout = TimeSpan.FromSeconds(30);

            System.Diagnostics.Debug.WriteLine("SharedResourceManager: Created shared HttpClient");
            return client;
        }

        private void InitializeMasterTimer()
        {
            _masterTimer = new DispatcherTimer();
            _masterTimer.Interval = TimeSpan.FromSeconds(1); // Check every second
            _masterTimer.Tick += MasterTimer_Tick;
            _masterTimer.Start();
            System.Diagnostics.Debug.WriteLine("SharedResourceManager: Master timer initialized");
        }

        private async void MasterTimer_Tick(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            var subscribersToUpdate = new List<ITimerSubscriber>();

            // Check which subscribers need updates
            foreach (var subscriber in _timerSubscribers.ToList()) // ToList to avoid modification during iteration
            {
                if (_lastUpdateTimes.ContainsKey(subscriber))
                {
                    var timeSinceLastUpdate = now - _lastUpdateTimes[subscriber];
                    if (timeSinceLastUpdate >= subscriber.UpdateInterval)
                    {
                        subscribersToUpdate.Add(subscriber);
                        _lastUpdateTimes[subscriber] = now;
                    }
                }
            }

            // Update subscribers asynchronously
            var updateTasks = subscribersToUpdate.Select(async subscriber =>
            {
                try
                {
                    await subscriber.OnTimerTickAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SharedResourceManager: Error updating {subscriber.GetType().Name}: {ex.Message}");
                }
            });

            if (updateTasks.Any())
            {
                await Task.WhenAll(updateTasks);
            }

            // Notify master timer tick for other components
            MasterTimerTick?.Invoke();
        }

        private bool AreSettingsEqual(SettingsData settings1, SettingsData settings2)
        {
            if (settings1 == null || settings2 == null) return false;

            // Quick comparison of key settings that might affect widgets
            return settings1.Window.Position == settings2.Window.Position &&
                   Math.Abs(settings1.Window.Width - settings2.Window.Width) < 0.1 &&
                   settings1.SystemMonitor.CpuThreshold == settings2.SystemMonitor.CpuThreshold &&
                   settings1.SystemMonitor.RamThreshold == settings2.SystemMonitor.RamThreshold &&
                   settings1.SystemMonitor.PingThreshold == settings2.SystemMonitor.PingThreshold &&
                   settings1.SystemMonitor.UpdateFrequencySeconds == settings2.SystemMonitor.UpdateFrequencySeconds;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                System.Diagnostics.Debug.WriteLine("SharedResourceManager: Disposing resources");

                _masterTimer?.Stop();
                _masterTimer = null;

                _httpClient?.Dispose();
                _httpClient = null;

                _timerSubscribers.Clear();
                _lastUpdateTimes.Clear();

                _disposed = true;
            }
        }
    }
}