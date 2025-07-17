using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;
using gtt_sidebar.Interfaces;

namespace gtt_sidebar.Widgets
{
    // Define StockItem class first
    public class StockItem
    {
        public string Symbol { get; set; }
        public string DisplayName { get; set; }
        public string ApiSource { get; set; }
        public string PairName { get; set; }
        public string HistoricalPairName { get; set; } // Separate name for historical data
        public bool RequiresFxConversion { get; set; }
        public TextBlock PriceTextBlock { get; set; }
        public TextBlock ChangeTextBlock { get; set; }
        public TextBlock ArrowTextBlock { get; set; }
        public Canvas ChartCanvas { get; set; }
    }

    public partial class StockWidget : UserControl, IWidget
    {
        private readonly HttpClient _httpClient;
        private DispatcherTimer _timer;
        private List<StockItem> _stocks;
        private double _usdToCadRate = 1.35; // Default, will be updated

        public string Name => "Stocks";

        public StockWidget()
        {
            InitializeComponent();
            _httpClient = new HttpClient();

            // Add user agent for Yahoo Finance
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            InitializeStocks();
        }

        private void InitializeStocks()
        {
            _stocks = new List<StockItem>
            {
                new StockItem {
                    Symbol = "BTC",
                    ApiSource = "Kraken",
                    PairName = "XBTCAD",
                    HistoricalPairName = "XXBTZCAD", // Kraken uses different names for historical
                    DisplayName = "BTC",
                    RequiresFxConversion = false
                },
                new StockItem {
                    Symbol = "XMR",
                    ApiSource = "Yahoo",
                    PairName = "XMR-USD",
                    DisplayName = "XMR",
                    RequiresFxConversion = true
                },
                new StockItem {
                    Symbol = "SPY",
                    ApiSource = "Yahoo",
                    PairName = "SPY",
                    DisplayName = "S&P 500",
                    RequiresFxConversion = true
                },
                new StockItem {
                    Symbol = "GOLD",
                    ApiSource = "Yahoo",
                    PairName = "GC=F",
                    DisplayName = "Gold/oz",
                    RequiresFxConversion = true
                }
            };

            CreateStockViews();
        }

        private void CreateStockViews()
        {
            StockContainer.Children.Clear();

            foreach (var stock in _stocks)
            {
                var stockView = CreateStockView(stock);
                StockContainer.Children.Add(stockView);
            }
        }

        private Border CreateStockView(StockItem stock)
        {
            var border = new Border
            {
                Margin = new Thickness(0, 0, 0, 3),
                Padding = new Thickness(4, 3, 4, 3),
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                CornerRadius = new CornerRadius(2),
                BorderBrush = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                BorderThickness = new Thickness(1)
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });

            // Top row - Stock info
            var topGrid = new Grid();
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left side - Symbol and current price
            var leftStack = new StackPanel();

            var symbolText = new TextBlock
            {
                Text = stock.DisplayName,
                FontSize = 8, // SMALLER ticker name
                FontWeight = FontWeights.Normal,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102))
            };

            var priceText = new TextBlock
            {
                Text = "Loading...",
                FontSize = 9, // BIGGER price value
                //FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                Margin = new Thickness(0, 1, 0, 0)
            };

            leftStack.Children.Add(symbolText);
            leftStack.Children.Add(priceText);

            // Right side - 7-day change
            var rightStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };

            var changeText = new TextBlock
            {
                Text = "--",
                FontSize = 9, // BIGGER percentage text
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var arrowText = new TextBlock
            {
                Text = "→",
                FontSize = 8,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 1, 0, 0)
            };

            rightStack.Children.Add(changeText);
            rightStack.Children.Add(arrowText);

            Grid.SetColumn(leftStack, 0);
            Grid.SetColumn(rightStack, 1);
            topGrid.Children.Add(leftStack);
            topGrid.Children.Add(rightStack);

            // Bar chart canvas
            var chartCanvas = new Canvas
            {
                Height = 14,
                Margin = new Thickness(0, 2, 0, 0)
            };

            Grid.SetRow(topGrid, 0);
            Grid.SetRow(chartCanvas, 1);
            mainGrid.Children.Add(topGrid);
            mainGrid.Children.Add(chartCanvas);

            border.Child = mainGrid;

            // Store references
            stock.PriceTextBlock = priceText;
            stock.ChangeTextBlock = changeText;
            stock.ArrowTextBlock = arrowText;
            stock.ChartCanvas = chartCanvas;

            return border;
        }

        public UserControl GetControl() => this;

        public async Task InitializeAsync()
        {
            // Update every 10 minutes since we have unlimited calls
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMinutes(10);
            _timer.Tick += async (s, e) => await UpdateAllStocksAsync();
            _timer.Start();

            await UpdateAllStocksAsync();
        }

        private async Task UpdateAllStocksAsync()
        {
            // First get USD/CAD rate for conversions
            await UpdateExchangeRate();

            var tasks = new List<Task>();

            foreach (var stock in _stocks)
            {
                tasks.Add(UpdateStockAsync(stock));
            }

            await Task.WhenAll(tasks);

            Dispatcher.Invoke(() =>
            {
                LastUpdateText.Text = $"Updated: {DateTime.Now:HH:mm}";
            });
        }

        private async Task UpdateExchangeRate()
        {
            try
            {
                var url = "https://query1.finance.yahoo.com/v8/finance/chart/USDCAD=X?period1=1640995200&period2=9999999999&interval=1d";
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                var chart = data["chart"]["result"][0];
                var meta = chart["meta"];
                var currentPrice = (double)meta["regularMarketPrice"];

                _usdToCadRate = currentPrice;
                System.Diagnostics.Debug.WriteLine($"USD/CAD rate updated: {_usdToCadRate:F4}");
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Failed to update USD/CAD rate, using default");
            }
        }

        private async Task UpdateStockAsync(StockItem stock)
        {
            try
            {
                if (stock.ApiSource == "Kraken")
                {
                    await UpdateFromKraken(stock);
                }
                else if (stock.ApiSource == "Yahoo")
                {
                    await UpdateFromYahoo(stock);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating {stock.Symbol}: {ex.Message}");
                Dispatcher.Invoke(() =>
                {
                    stock.PriceTextBlock.Text = "Error";
                    stock.ChangeTextBlock.Text = "--";
                    stock.ArrowTextBlock.Text = "⚠️";
                });
            }
        }

        private async Task UpdateFromKraken(StockItem stock)
        {
            try
            {
                // Get current price
                var url = $"https://api.kraken.com/0/public/Ticker?pair={stock.PairName}";
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                if (data["error"] != null && ((JArray)data["error"]).Count > 0)
                {
                    var errorMsg = (string)((JArray)data["error"])[0];
                    throw new Exception($"Kraken API error: {errorMsg}");
                }

                var result = data["result"];
                if (result == null)
                {
                    throw new Exception("No result data from Kraken API");
                }

                var firstPair = result.First;
                var pairData = firstPair.First;
                var currentPrice = (double)pairData["c"][0];

                // Try to get historical data with the alternative pair name
                var historicalPairName = stock.HistoricalPairName ?? stock.PairName;
                var historicalUrl = $"https://api.kraken.com/0/public/OHLC?pair={historicalPairName}&interval=1440";

                try
                {
                    var historicalResponse = await _httpClient.GetStringAsync(historicalUrl);
                    var historicalData = JObject.Parse(historicalResponse);

                    if (historicalData["error"] != null && ((JArray)historicalData["error"]).Count > 0)
                    {
                        throw new Exception("Historical data not available");
                    }

                    var historicalResult = historicalData["result"];
                    if (historicalResult != null)
                    {
                        // Find the correct key in the result
                        JArray ohlcData = null;
                        foreach (var property in historicalResult.Children<JProperty>())
                        {
                            if (property.Name != "last")
                            {
                                ohlcData = (JArray)property.Value;
                                break;
                            }
                        }

                        if (ohlcData != null && ohlcData.Count > 0)
                        {
                            var priceData = new List<(DateTime date, double price)>();

                            var startIndex = Math.Max(0, ohlcData.Count - 8);
                            for (int i = startIndex; i < ohlcData.Count; i++)
                            {
                                var day = ohlcData[i];
                                var timestamp = (long)day[0];
                                var closePrice = (double)day[4];
                                var date = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;

                                priceData.Add((date, closePrice));
                            }

                            if (priceData.Count >= 2)
                            {
                                var weekAgoPrice = priceData[0].price;
                                var weekChange = ((currentPrice - weekAgoPrice) / weekAgoPrice) * 100;

                                Dispatcher.Invoke(() =>
                                {
                                    stock.PriceTextBlock.Text = FormatCurrency(currentPrice);
                                    stock.ChangeTextBlock.Text = $"{weekChange:+0.0;-0.0}%";

                                    UpdateTrendVisuals(stock, weekChange);
                                    DrawBarChartWithDates(stock, priceData);
                                });
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Historical data failed: {ex.Message}");
                }

                // Fallback - just current price without chart
                Dispatcher.Invoke(() =>
                {
                    stock.PriceTextBlock.Text = FormatCurrency(currentPrice);
                    stock.ChangeTextBlock.Text = "--";
                    stock.ArrowTextBlock.Text = "→";
                    stock.ChartCanvas.Children.Clear();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Kraken error for {stock.PairName}: {ex.Message}");
                throw;
            }
        }

        private async Task UpdateFromYahoo(StockItem stock)
        {
            try
            {
                var period1 = DateTimeOffset.UtcNow.AddDays(-14).ToUnixTimeSeconds();
                var period2 = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{stock.PairName}?period1={period1}&period2={period2}&interval=1d";

                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                var chart = data["chart"]["result"][0];
                var timestamps = (JArray)chart["timestamp"];
                var quotes = chart["indicators"]["quote"][0];
                var closes = (JArray)quotes["close"];

                var priceData = new List<(DateTime date, double price)>();

                var startIndex = Math.Max(0, closes.Count - 8);
                for (int i = startIndex; i < closes.Count; i++)
                {
                    if (closes[i].Type != JTokenType.Null && timestamps[i].Type != JTokenType.Null)
                    {
                        var price = (double)closes[i];
                        var timestamp = (long)timestamps[i];
                        var date = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;

                        // Convert to CAD if needed
                        if (stock.RequiresFxConversion)
                        {
                            price *= _usdToCadRate;
                        }
                        priceData.Add((date, price));
                    }
                }

                if (priceData.Count >= 2)
                {
                    var currentPrice = priceData[priceData.Count - 1].price;
                    var weekAgoPrice = priceData[0].price;
                    var weekChange = ((currentPrice - weekAgoPrice) / weekAgoPrice) * 100;

                    Dispatcher.Invoke(() =>
                    {
                        stock.PriceTextBlock.Text = FormatCurrency(currentPrice);
                        stock.ChangeTextBlock.Text = $"{weekChange:+0.0;-0.0}%";

                        UpdateTrendVisuals(stock, weekChange);
                        DrawBarChartWithDates(stock, priceData);
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Yahoo error for {stock.PairName}: {ex.Message}");
                throw;
            }
        }

        private string FormatCurrency(double amount)
        {
            if (amount >= 1000)
            {
                return amount.ToString("$#,##0", CultureInfo.CreateSpecificCulture("en-CA"));
            }
            else
            {
                return amount.ToString("$0.00", CultureInfo.CreateSpecificCulture("en-CA"));
            }
        }

        private void UpdateTrendVisuals(StockItem stock, double change)
        {
            if (change > 0)
            {
                stock.ArrowTextBlock.Text = "↗️";
                stock.ChangeTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(34, 139, 34));
            }
            else if (change < 0)
            {
                stock.ArrowTextBlock.Text = "↘️";
                stock.ChangeTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(220, 20, 60));
            }
            else
            {
                stock.ArrowTextBlock.Text = "→";
                stock.ChangeTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
            }
        }

        private void DrawBarChart(StockItem stock, List<double> prices)
        {
            if (prices.Count < 2) return;

            stock.ChartCanvas.Children.Clear();

            var width = 108.0;
            var height = 12.0;

            var minPrice = prices.Min();
            var maxPrice = prices.Max();
            var priceRange = maxPrice - minPrice;

            if (priceRange == 0) return;

            var barWidth = width / prices.Count;

            for (int i = 0; i < prices.Count; i++)
            {
                var barHeight = ((prices[i] - minPrice) / priceRange * height);
                var x = i * barWidth;
                var y = height - barHeight;

                // Calculate the date for this bar (going backwards from today)
                var daysBack = prices.Count - 1 - i;
                var barDate = DateTime.Now.AddDays(-daysBack);

                var rect = new Rectangle
                {
                    Width = barWidth - 1,
                    Height = Math.Max(barHeight, 1), // Minimum height so it's visible
                    Fill = new SolidColorBrush(Color.FromArgb(150, 100, 149, 237)),
                    Stroke = new SolidColorBrush(Color.FromRgb(100, 149, 237)),
                    StrokeThickness = 0.5,
                    // Add tooltip with date and price
                    ToolTip = $"{barDate:MMM dd}: {FormatCurrency(prices[i])}"
                };

                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);

                stock.ChartCanvas.Children.Add(rect);
            }
        }

        private void DrawBarChartWithDates(StockItem stock, List<(DateTime date, double price)> priceData)
        {
            if (priceData.Count < 2) return;

            stock.ChartCanvas.Children.Clear();

            var width = 108.0;
            var height = 12.0;

            var prices = priceData.Select(x => x.price).ToList();
            var minPrice = prices.Min();
            var maxPrice = prices.Max();
            var priceRange = maxPrice - minPrice;

            if (priceRange == 0) return;

            var barWidth = width / priceData.Count;

            for (int i = 0; i < priceData.Count; i++)
            {
                var barHeight = ((priceData[i].price - minPrice) / priceRange * height);
                var x = i * barWidth;
                var y = height - barHeight;

                var rect = new Rectangle
                {
                    Width = barWidth - 1,
                    Height = Math.Max(barHeight, 1), // Minimum height so it's visible
                    Fill = new SolidColorBrush(Color.FromArgb(150, 100, 149, 237)),
                    Stroke = new SolidColorBrush(Color.FromRgb(100, 149, 237)),
                    StrokeThickness = 0.5,
                    // Tooltip shows the actual date and price for this bar
                    ToolTip = $"{priceData[i].date:MMM dd}: {FormatCurrency(priceData[i].price)}"
                };

                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);

                stock.ChartCanvas.Children.Add(rect);
            }
        }

        void IWidget.Dispose()
        {
            _timer?.Stop();
            _httpClient?.Dispose();
        }
    }
}