# GTT Sidebar - Architecture & Development Guide

## Project Overview
A lightweight Windows desktop sidebar application (122px width) displaying real-time widgets. Built with C# WPF (.NET Framework 4.7.2) for Windows 10/11. The sidebar is designed to complement LibreWolf browser's fixed window size.

## Core Architecture Diagram

```
gtt-sidebar/
│
├── Core/                              [Application core infrastructure]
│   ├── Application/
│   │   ├── App.xaml/.cs              → WPF application entry point, startup URI configuration
│   │   └── MainWindow.xaml/.cs       → Main sidebar window (122px), widget container, tray icon management
│   │
│   ├── Interfaces/
│   │   ├── IWidget.cs                → Base widget contract: Name, GetControl(), InitializeAsync(), Dispose()
│   │   ├── ITimerSubscriber.cs       → Timer-based widgets: UpdateInterval, OnTimerTickAsync()
│   │   ├── IConfigurableWidget.cs    → Settings support for widgets (future use)
│   │   └── IWidgetMetadata.cs        → Widget discovery metadata (future use)
│   │
│   ├── Managers/
│   │   ├── SharedResourceManager.cs   → SINGLETON: Centralized HttpClient, timer system, settings cache, batch saves
│   │   ├── WidgetManager.cs          → Auto-discovery, loading, lifecycle management, ordering
│   │   ├── WindowPositioner.cs       → Sidebar positioning logic (left/right, margins)
│   │   └── StartupManager.cs         → Windows registry startup management
│   │
│   ├── Settings/
│   │   ├── SettingsData.cs           → Data models: WindowSettings, SystemMonitorSettings, enums
│   │   ├── SettingsStorage.cs        → JSON persistence with async operations (%APPDATA%/gtt-sidebar/)
│   │   ├── SettingsWindow.xaml/.cs   → 350x600px settings UI with collapsible sections
│   │   ├── ShortcutData.cs           → Shortcut models: ShortcutItem, ShortcutsData, type detection
│   │   ├── ShortcutStorage.cs        → Shortcuts JSON + custom icon management
│   │   ├── IconCatalog.cs            → 50 built-in Phosphor icon definitions
│   │   └── IconPicker.xaml/.cs       → 220x240px icon selection popup (5x10 grid)
│   │
│   └── Icons/DefaultIcons/           → 50 PNG icon files (18x18px each)
│
├── Widgets/                           [Individual widget implementations]
│   ├── ClockWidget/
│   │   └── ClockWidget.xaml/.cs      → Time/date display, 1-second DispatcherTimer
│   │
│   ├── WeatherWidget/
│   │   └── WeatherWidget.xaml/.cs    → OpenWeatherMap API, 30-min updates via ITimerSubscriber
│   │                                    Current + 3-day forecast, location change on click
│   │
│   ├── StockWidget/
│   │   └── StockWidget.xaml/.cs      → BTC/XMR (Kraken), SPY/Gold (Yahoo), 10-min updates
│   │                                    USD/CAD conversion, 7-day percentage charts
│   │
│   ├── Notes/
│   │   ├── NotesData.cs              → NoteTab, NotesData, WindowPosition models
│   │   ├── NotesStorage.cs           → JSON persistence with async, backup functionality
│   │   ├── NotesWidget.xaml/.cs      → 175px height preview, click to open popup
│   │   ├── NotesPopup.xaml/.cs       → 370x230px editor, 8 tabs max, auto-save every 15s
│   │   └── MarkdownHelper.cs         → Markdown utilities (currently unused)
│   │
│   ├── Shortcuts/
│   │   └── ShortcutsWidget.xaml/.cs  → 4x4 icon grid, launches exe/URL/commands
│   │                                    PNG icon support, settings integration
│   │
│   └── SystemMonitor/
│       └── SystemMonitorWidget.xaml/.cs → CPU/RAM (PerformanceCounters), Ping (8.8.8.8)
│                                          Configurable thresholds, 5min/1hr averages
│
├── Properties/                        [Project configuration]
│   ├── AssemblyInfo.cs               → Assembly metadata and version info
│   ├── Resources.Designer.cs/.resx   → Resource management (auto-generated)
│   └── Settings.Designer.cs/.settings → Application settings (auto-generated)
│
├── App.config                         → .NET Framework 4.7.2 configuration
├── gtt-sidebar.csproj                → Project file with all references and resources
├── gtt-sidebar.sln                   → Solution file
├── packages.config                   → NuGet packages (Newtonsoft.Json 13.0.3)
└── gtt-sidebar-icon.ico             → Application icon
```

## Critical Relationships & Data Flow

### 1. Initialization Flow
```
App.xaml → MainWindow → WidgetManager.DiscoverAndLoadWidgets() → IWidget implementations
                     ↓
            SharedResourceManager.Instance (singleton initialization)
                     ↓
            Widgets.InitializeAsync() → Subscribe to timers/events
```

### 2. Settings System Flow
```
SettingsWindow ←→ SettingsData ←→ SettingsStorage (JSON)
       ↓                              ↓
   UI Events                    %APPDATA%/gtt-sidebar/settings.json
       ↓
SharedResourceManager.UpdateSettings() → SettingsChanged event → All widgets refresh
```

### 3. Timer Management (SharedResourceManager)
```
MasterTimer (1s) → Check each ITimerSubscriber.UpdateInterval
                 → Call OnTimerTickAsync() when interval reached
                 → Weather (30min), Stocks (10min), SystemMonitor (configurable)
```

### 4. Shortcuts System
```
ShortcutsWidget ←→ ShortcutsData ←→ ShortcutsStorage
                        ↓
                  IconPicker popup → Icon selection
                        ↓
            SettingsWindow management → Drag & drop reordering
```

## Key Technical Details

### Dependencies
- **Framework**: .NET Framework 4.7.2 (C# 7.3 max)
- **UI**: WPF with XAML
- **JSON**: Newtonsoft.Json 13.0.3
- **System**: Microsoft.VisualBasic (RAM detection), System.Windows.Forms (tray icon)

### Storage Locations
```
%APPDATA%/gtt-sidebar/
├── settings.json      → Window position, margins, thresholds
├── shortcuts.json     → User shortcuts with icon references
├── notes.json         → Notes tabs and content
└── icons/            → Custom PNG icons (GUID filenames)
```

### Widget Loading Order
1. Clock
2. Weather  
3. Notes
4. Shortcuts
5. Stocks
6. System Monitor (if exists)

### Critical Singleton Pattern
**SharedResourceManager** provides:
- Single HttpClient instance (prevents socket exhaustion)
- Centralized timer management (reduces CPU usage)
- Settings caching (reduces file I/O)
- Batch save operations (2-second intervals)

### Event System
- `SettingsApplied` → Window repositioning
- `ShortcutsChanged` → Widget refresh
- `NotesChanged` → Preview update
- `SettingsChanged` (SharedResourceManager) → All widgets update

### Performance Constraints
- **Memory**: <50MB total footprint
- **CPU**: <1% when idle
- **Startup**: <2 seconds
- **Widget Updates**: Async to prevent UI blocking

## Development Guidelines

### Adding New Widgets
1. Create folder in `Widgets/NewWidget/`
2. Implement `IWidget` interface
3. Optional: Implement `ITimerSubscriber` for periodic updates
4. Add to `WidgetManager.GetWidgetOrder()` for positioning
5. Widget auto-discovered via reflection

### Common Pitfalls to Avoid
- **Namespace collisions**: Don't name class same as parent namespace
- **HttpClient**: Always use `SharedResourceManager.Instance.HttpClient`
- **Timers**: Use `ITimerSubscriber`, not individual DispatcherTimers
- **Settings**: Cache via SharedResourceManager, don't read files directly
- **IDs**: Use GUIDs for data items, never array indices
- **Disposal**: Always implement proper cleanup in `Dispose()`

### Framework Limitations (.NET 4.7.2)
- No `TakeLast()` - use manual array indexing
- No switch expressions - use if-else chains
- No `GetValueOrDefault()` - use `ContainsKey()` checks
- Limited async/await patterns

### Testing Checklist
- [ ] Widget loads without errors
- [ ] Settings changes apply immediately
- [ ] Disposal doesn't throw exceptions
- [ ] Works with empty/null data
- [ ] Handles API failures gracefully
- [ ] Memory usage stays constant over time

## Troubleshooting Quick Reference

### Common Issues
1. **Settings not updating**: Check SharedResourceManager event subscriptions
2. **Widget not appearing**: Verify IWidget implementation and naming
3. **Icons not showing**: Check PNG paths in GetBuiltinIconPath()
4. **High CPU usage**: Ensure using ITimerSubscriber, not custom timers
5. **Memory leaks**: Check event handler disposal and HttpClient usage

### Debug Output Locations
- Visual Studio Output window (Debug.WriteLine)
- Event Viewer → Applications (for crashes)
- `%APPDATA%/gtt-sidebar/` for data files

## API Keys & External Services
- **OpenWeatherMap**: API key in WeatherWidget.xaml.cs (free tier)
- **Kraken**: Public API, no key needed
- **Yahoo Finance**: Public API, no key needed
- **Ping Target**: Google DNS (8.8.8.8)