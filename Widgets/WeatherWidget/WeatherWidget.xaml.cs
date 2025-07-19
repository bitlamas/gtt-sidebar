using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;
using gtt_sidebar.Core.Interfaces;

namespace gtt_sidebar.Widgets.WeatherWidget
{
    public partial class WeatherWidget : UserControl, IWidget
    {
        private HttpClient _httpClient;
        private DispatcherTimer _timer;
        private string _currentCity = "Saint-Joseph-de-Beauce,QC,CA";
        private int _retryCount = 0;
        private const int MAX_IMMEDIATE_RETRIES = 10;
        private const int RETRY_DELAY_SECONDS = 10;
        private const int FALLBACK_RETRY_MINUTES = 5;
        private DispatcherTimer _retryTimer;
        private bool _isRetrying = false;

        // Free OpenWeatherMap API key - you'll need to get your own
        private const string API_KEY = "c9b2bd0c1ea061353ec9b02831f77c57"; // Get from openweathermap.org

        public new string Name => "Weather";  // WeatherWidget

        public WeatherWidget()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
        }

        public UserControl GetControl() => this;

        public async Task InitializeAsync()
        {
            // Update every 30 minutes
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMinutes(30);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            await UpdateWeatherAsync();
        }

        private async Task UpdateWeatherAsync()
        {
            try
            {
                // Current weather
                var currentUrl = $"https://api.openweathermap.org/data/2.5/weather?q={_currentCity}&appid={API_KEY}&units=metric";
                var currentResponse = await _httpClient.GetStringAsync(currentUrl);
                var currentData = JObject.Parse(currentResponse);

                // Forecast
                var forecastUrl = $"https://api.openweathermap.org/data/2.5/forecast?q={_currentCity}&appid={API_KEY}&units=metric";
                var forecastResponse = await _httpClient.GetStringAsync(forecastUrl);
                var forecastData = JObject.Parse(forecastResponse);

                UpdateCurrentWeather(currentData);
                UpdateForecast(forecastData);
                _retryCount = 0;
                _isRetrying = false;
                StopRetryTimer();

                System.Diagnostics.Debug.WriteLine("Weather updated successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Weather update error (attempt {_retryCount + 1}): {ex.Message}");
                await HandleWeatherError();
            }
        }
        private async Task HandleWeatherError()
        {
            _retryCount++;

            // Show error state
            WeatherDescription.Text = $"Error loading (retry {_retryCount})";
            Temperature.Text = "--°C";

            // Clear forecast on error
            Day1Icon.Text = "?";
            Day1Temp.Text = "--°";
            Day2Icon.Text = "?";
            Day2Temp.Text = "--°";
            Day3Icon.Text = "?";
            Day3Temp.Text = "--°";

            if (_retryCount <= MAX_IMMEDIATE_RETRIES)
            {
                // Immediate retries: wait 10 seconds
                System.Diagnostics.Debug.WriteLine($"Scheduling immediate retry {_retryCount}/{MAX_IMMEDIATE_RETRIES} in {RETRY_DELAY_SECONDS} seconds");

                _isRetrying = true;
                StartRetryTimer(TimeSpan.FromSeconds(RETRY_DELAY_SECONDS));
            }
            else
            {
                // Fallback retries: wait 5 minutes
                System.Diagnostics.Debug.WriteLine($"Switching to fallback retry mode - will retry every {FALLBACK_RETRY_MINUTES} minutes");

                _isRetrying = true;
                StartRetryTimer(TimeSpan.FromMinutes(FALLBACK_RETRY_MINUTES));

                WeatherDescription.Text = "Connection issues";
            }
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            // Don't interfere if we're in retry mode
            if (!_isRetrying)
            {
                _retryCount = 0; // Reset retry count for new update cycle
                await UpdateWeatherAsync();
            }
        }

        private void UpdateCurrentWeather(JObject data)
        {
            var temp = Math.Round((double)data["main"]["temp"]);
            var description = (string)data["weather"][0]["description"];
            var weatherId = (int)data["weather"][0]["id"];

            Temperature.Text = $"{temp}°C";
            WeatherDescription.Text = CapitalizeFirst(description);
            WeatherIcon.Text = GetWeatherIcon(weatherId);
        }

        private void UpdateForecast(JObject data)
        {
            var forecasts = (JArray)data["list"];
            var dailyTemps = new double[3];
            var dailyIcons = new string[3];

            // Get next 3 days (simplified - every 8th entry represents ~24 hours)
            for (int i = 0; i < 3 && i * 8 < forecasts.Count; i++)
            {
                var forecast = forecasts[i * 8]; // Every 8th entry (24 hours)
                dailyTemps[i] = Math.Round((double)forecast["main"]["temp"]);
                dailyIcons[i] = GetWeatherIcon((int)forecast["weather"][0]["id"]);
            }

            Day1Icon.Text = dailyIcons[0];
            Day1Temp.Text = $"{dailyTemps[0]}°";
            Day2Icon.Text = dailyIcons[1];
            Day2Temp.Text = $"{dailyTemps[1]}°";
            Day3Icon.Text = dailyIcons[2];
            Day3Temp.Text = $"{dailyTemps[2]}°";
        }

        private string GetWeatherIcon(int weatherId)
        {
            // Using traditional if-else instead of switch expressions for C# 7.3 compatibility
            if (weatherId >= 200 && weatherId <= 232)
                return "⛈️"; // Thunderstorm
            else if (weatherId >= 300 && weatherId <= 321)
                return "🌦️"; // Drizzle
            else if (weatherId >= 500 && weatherId <= 531)
                return "🌧️"; // Rain
            else if (weatherId >= 600 && weatherId <= 622)
                return "❄️"; // Snow
            else if (weatherId >= 701 && weatherId <= 781)
                return "🌫️"; // Atmosphere
            else if (weatherId == 800)
                return "☀️"; // Clear
            else if (weatherId == 801)
                return "🌤️"; // Few clouds
            else if (weatherId == 802)
                return "⛅"; // Scattered clouds
            else if (weatherId >= 803 && weatherId <= 804)
                return "☁️"; // Broken/overcast clouds
            else
                return "🌡️"; // Default
        }

        private string CapitalizeFirst(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToUpper(input[0]) + input.Substring(1);
        }

        private void LocationText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Simple input dialog for changing city
            var newCity = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter city name (e.g., 'Montreal,QC,CA'):",
                "Change Location",
                _currentCity);

            if (!string.IsNullOrWhiteSpace(newCity))
            {
                _currentCity = newCity;
                LocationText.Text = newCity.Split(',')[0]; // Show just city name
                _ = UpdateWeatherAsync();
            }
        }

        private void StartRetryTimer(TimeSpan interval)
        {
            StopRetryTimer();

            _retryTimer = new DispatcherTimer();
            _retryTimer.Interval = interval;
            _retryTimer.Tick += RetryTimer_Tick;
            _retryTimer.Start();
        }

        private void StopRetryTimer()
        {
            if (_retryTimer != null)
            {
                _retryTimer.Stop();
                _retryTimer.Tick -= RetryTimer_Tick;
                _retryTimer = null;
            }
        }

        private async void RetryTimer_Tick(object sender, EventArgs e)
        {
            if (_isRetrying)
            {
                StopRetryTimer();
                await UpdateWeatherAsync();
            }
        }
        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }
            StopRetryTimer();

            _httpClient?.Dispose();
            _httpClient = null;
        }
    }
}