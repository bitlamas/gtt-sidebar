# GTT Sidebar Development Guide - Complete Context & Code

## Project Overview
A lightweight Windows sidebar application built in C# WPF that displays real-time widgets in a fixed position on the right side of the screen. The application is designed to complement a specific browser setup without interfering with the main workspace.

## Hardware & Environment Specifications

### System Details
- **Computer**: Lenovo ThinkPad A485 (AMD Ryzen 5 PRO 2500U, 32GB RAM)
- **OS**: Windows 10 IoT LTSC Enterprise (performance-optimized, minimal bloat)
- **Screen Resolution**: 1920x1080 at 125% scaling
- **Browser**: LibreWolf (always opens at 1400x700 window size)
- **Development Environment**: Visual Studio Community 2022
- **Target Framework**: .NET Framework 4.7.2

### Location Context
- **User Location**: Saint-Joseph-de-Beauce, Quebec, Canada
- **Currency Preference**: CAD (Canadian Dollars)
- **Number Format**: Canadian formatting with comma thousands separators

## Window Specifications & Positioning

### Precise Dimensions
```csharp
var windowWidth = 122;      // pixels
var marginRight = 5;        // pixels from screen edge
var marginTop = 5;          // pixels from top
var marginBottom = 5;       // pixels from bottom
```

### Calculated Height
```csharp
this.Height = screenHeight - taskbarHeight - marginTop - marginBottom;
```

### Window Properties
- **Position**: Right-aligned with 5px margin from screen edge
- **Behavior**: Always on top, no taskbar icon, frameless
- **Resizing**: Disabled (fixed dimensions)
- **Background**: White theme with light gray borders

## Technical Architecture

### Project Structure
```
gtt-sidebar/
├── Interfaces/
│   └── IWidget.cs                 // Widget interface definition
├── Widgets/
│   ├── ClockWidget.xaml           // Time/date display
│   ├── ClockWidget.xaml.cs
│   ├── WeatherWidget.xaml         // Weather with 3-day forecast
│   ├── WeatherWidget.xaml.cs
│   ├── StockWidget.xaml           // 4-stock market panel
│   └── StockWidget.xaml.cs
├── MainWindow.xaml                // Main container
├── MainWindow.xaml.cs
├── App.xaml
└── App.xaml.cs
```

### Widget Interface
```csharp
public interface IWidget
{
    string Name { get; }
    UserControl GetControl();
    Task InitializeAsync();
    void Dispose();
}
```

## Widget Implementations

### 1. Clock Widget
- **Display**: Current time (HH:mm:ss) and date (MMMM dd, yyyy)
- **Update Frequency**: Every second
- **Styling**: White background with light gray border
- **Font Sizes**: Time 12px bold, Date 9px regular

### 2. Weather Widget
- **API**: OpenWeatherMap (requires free API key)
- **Current Weather**: Icon, description, temperature in Celsius
- **3-Day Forecast**: Mini icons with temperatures
- **Location**: Configurable via double-click (default: Saint-Joseph-de-Beauce,QC,CA)
- **Update Frequency**: Every 30 minutes
- **Features**: Weather icons using Unicode emojis, clickable location change

#### Weather Icons Mapping
```csharp
200-232: ⛈️ (Thunderstorm)
300-321: 🌦️ (Drizzle)  
500-531: 🌧️ (Rain)
600-622: ❄️ (Snow)
701-781: 🌫️ (Atmosphere)
800: ☀️ (Clear)
801: 🌤️ (Few clouds)
802: ⛅ (Scattered clouds)
803-804: ☁️ (Broken/overcast)
```

### 3. Stock Widget
- **Assets Tracked**: BTC, XMR, S&P 500, Gold (1oz)
- **Display Format**: Ticker, price in CAD, 7-day % change, trend arrow
- **Chart Type**: Mini bar charts showing 7-day price history
- **Update Frequency**: Every 10 minutes
- **Font Hierarchy**: Values emphasized over ticker names

#### API Sources & Endpoints
1. **Bitcoin (BTC/CAD)**: Kraken API
   - Current: `https://api.kraken.com/0/public/Ticker?pair=XBTCAD`
   - Historical: `https://api.kraken.com/0/public/OHLC?pair=XXBTZCAD&interval=1440`

2. **Monero (XMR)**: Yahoo Finance (USD) + USD/CAD conversion
   - Endpoint: `https://query1.finance.yahoo.com/v8/finance/chart/XMR-USD`

3. **S&P 500**: Yahoo Finance (SPY) + USD/CAD conversion
   - Endpoint: `https://query1.finance.yahoo.com/v8/finance/chart/SPY`

4. **Gold**: Yahoo Finance Gold Futures + USD/CAD conversion
   - Endpoint: `https://query1.finance.yahoo.com/v8/finance/chart/GC=F`

5. **USD/CAD Rate**: Yahoo Finance
   - Endpoint: `https://query1.finance.yahoo.com/v8/finance/chart/USDCAD=X`

#### Stock Widget Features
- **Tooltips**: Hover over chart bars shows date and exact price
- **Number Formatting**: Canadian locale with comma separators
- **Color Coding**: Green for gains, red for losses
- **Chart Tooltips**: Format "MMM dd: $XX,XXX"

## Key Technical Challenges & Solutions

### 1. .NET Framework Compatibility Issues
**Problem**: Modern C# features not available in .NET Framework 4.7.2
**Solutions**:
- Replaced `TakeLast()` with manual array indexing
- Used traditional if-else instead of switch expressions
- Avoided LINQ methods not available in older framework

### 2. Kraken API Pair Name Inconsistencies
**Problem**: Different pair names for current vs historical data
**Solution**: Separate `PairName` and `HistoricalPairName` properties
```csharp
PairName = "XBTCAD"           // For current price
HistoricalPairName = "XXBTZCAD" // For historical OHLC data
```

### 3. Font Size Hierarchy Requirements
**Problem**: User needed values more prominent than labels
**Solution**: Inverted typical sizing
```csharp
// Ticker name: Small, gray
FontSize = 7, FontWeight = Normal, Color = Gray

// Price value: Large, bold, black  
FontSize = 10, FontWeight = Bold, Color = Black

// Percentage: Large, bold, colored
FontSize = 9, FontWeight = Bold, Color = Green/Red
```

### 4. API Rate Limiting
**Problem**: AlphaVantage limited to 25 requests/day
**Solution**: Switched to Yahoo Finance (unlimited) with proper User-Agent header

### 5. Currency Conversion
**Problem**: Need all prices in CAD
**Solution**: Real-time USD/CAD rate fetching + conversion for USD-based assets

## NuGet Dependencies
```xml
<Reference Include="Newtonsoft.Json" Version="13.0.3" />
<Reference Include="Microsoft.VisualBasic" />
```

## Critical Code Patterns

### Window Positioning
```csharp
private void PositionWindow()
{
    var screenWidth = SystemParameters.PrimaryScreenWidth;
    var screenHeight = SystemParameters.PrimaryScreenHeight;
    var taskbarHeight = SystemParameters.PrimaryScreenHeight - SystemParameters.WorkArea.Height;
    
    this.Width = 122;
    this.Height = screenHeight - taskbarHeight - 5 - 5; // margins
    this.Left = screenWidth - 122 - 5; // right aligned with margin
    this.Top = 5;
    this.WindowStartupLocation = WindowStartupLocation.Manual;
}
```

### Currency Formatting
```csharp
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
```

### Bar Chart with Tooltips
```csharp
var rect = new Rectangle
{
    Width = barWidth - 1,
    Height = Math.Max(barHeight, 1),
    Fill = new SolidColorBrush(Color.FromArgb(150, 100, 149, 237)),
    ToolTip = $"{date:MMM dd}: {FormatCurrency(price)}"
};
```

## Widget Loading Pattern
```csharp
private async void LoadWidgets()
{
    WidgetContainer.Children.Clear();
    
    var clockWidget = new ClockWidget();
    await clockWidget.InitializeAsync();
    WidgetContainer.Children.Add(clockWidget);
    
    var weatherWidget = new WeatherWidget();
    await weatherWidget.InitializeAsync();
    WidgetContainer.Children.Add(weatherWidget);
    
    var stockWidget = new StockWidget();
    await stockWidget.InitializeAsync();
    WidgetContainer.Children.Add(stockWidget);
}
```

## Performance Considerations
- **Memory Usage**: Target <50MB total
- **CPU Usage**: <1% when idle
- **Update Intervals**: Optimized to balance freshness vs resource usage
  - Clock: 1 second
  - Weather: 30 minutes  
  - Stocks: 10 minutes
- **HTTP Client Reuse**: Single HttpClient instance per widget
- **Async Patterns**: All API calls use async/await to prevent UI blocking

## Visual Design System
- **Background**: White (#FFFFFF)
- **Borders**: Light gray (#E0E0E0)
- **Widget Backgrounds**: Off-white (#F8F8F8)
- **Text Primary**: Dark gray (#333333)
- **Text Secondary**: Medium gray (#666666)
- **Text Tertiary**: Light gray (#999999)
- **Success**: Green (#228B22)
- **Danger**: Red (#DC143C)
- **Chart Color**: Blue with transparency (#6495ED with alpha)

## Error Handling Strategy
1. **Graceful Degradation**: Show partial data when possible
2. **User-Friendly Messages**: "Error loading" instead of technical errors
3. **Retry Logic**: Built into timer-based updates
4. **Debug Output**: Comprehensive logging for development
5. **Fallback Values**: Default data when APIs fail

## API Keys Required
1. **OpenWeatherMap**: Free tier (1000 calls/day)
   - Sign up at: https://openweathermap.org/api
   - Replace `YOUR_API_KEY_HERE` in WeatherWidget.xaml.cs

## Future Enhancement Areas
1. **Widget Configuration UI**: Settings panel for customization
2. **Additional Widgets**: News, system monitoring, notes
3. **Theme Support**: Dark mode option
4. **Widget Marketplace**: Downloadable widget plugins
5. **Export/Import**: Configuration backup/restore

## Deployment Notes
- **Single Executable**: Compiles to standalone .exe
- **No Installation Required**: Portable application
- **Startup Integration**: Can be added to Windows startup folder
- **Update Mechanism**: Manual replacement of executable

This documentation provides complete context for continuing development or recreating the sidebar application with all user-specific requirements, technical constraints, and learned solutions preserved.