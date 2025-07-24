using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using gtt_sidebar.Core.Interfaces;

namespace gtt_sidebar.Core.Managers
{
    /// <summary>
    /// Manages widget discovery, loading, and lifecycle
    /// </summary>
    public class WidgetManager
    {
        private List<IWidget> _loadedWidgets = new List<IWidget>();

        /// <summary>
        /// Discovers and loads all available widgets in the application
        /// </summary>
        /// <returns>List of loaded widgets</returns>
        public List<IWidget> DiscoverAndLoadWidgets()
        {
            try
            {
                // Find all types that implement IWidget
                var widgetTypes = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => typeof(IWidget).IsAssignableFrom(t) &&
                               !t.IsInterface &&
                               !t.IsAbstract);

                foreach (var widgetType in widgetTypes)
                {
                    try
                    {
                        var widget = (IWidget)Activator.CreateInstance(widgetType);
                        _loadedWidgets.Add(widget);
                        System.Diagnostics.Debug.WriteLine($"Loaded widget: {widget.Name}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to load widget {widgetType.Name}: {ex.Message}");
                    }
                }

                // Sort widgets by name to maintain consistent order
                _loadedWidgets = _loadedWidgets.OrderBy(w => GetWidgetOrder(w.Name)).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during widget discovery: {ex.Message}");
            }

            return _loadedWidgets;
        }

        /// <summary>
        /// Defines the display order for widgets
        /// </summary>
        private int GetWidgetOrder(string widgetName)
        {
            switch (widgetName)
            {
                case "Clock": return 1;
                case "Weather": return 2;
                case "Stocks": return 3;
                case "Notes": return 4;
                case "Shortcuts": return 5;  
                default: return 99; 
            }
        }

        /// <summary>
        /// Initializes all loaded widgets asynchronously
        /// </summary>
        public async Task InitializeWidgetsAsync()
        {
            var localWidgets = _loadedWidgets.Where(w => w.Name == "Clock" || w.Name == "Notes" || w.Name == "Shortcuts").ToList();
            var apiWidgets = _loadedWidgets.Where(w => w.Name == "Weather" || w.Name == "Stocks" || w.Name == "System Monitor").ToList();

            // Initialize local widgets first
            foreach (var widget in localWidgets)
            {
                await widget.InitializeAsync();
            }

            // Initialize API widgets in parallel
            var apiTasks = apiWidgets.Select(widget => widget.InitializeAsync()).ToArray();
            await Task.WhenAll(apiTasks);
        }

        /// <summary>
        /// Disposes all loaded widgets
        /// </summary>
        public void DisposeWidgets()
        {
            foreach (var widget in _loadedWidgets)
            {
                try
                {
                    widget.Dispose();
                    System.Diagnostics.Debug.WriteLine($"Disposed widget: {widget.Name}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing widget {widget.Name}: {ex.Message}");
                }
            }
            _loadedWidgets.Clear();
        }

        /// <summary>
        /// Gets all currently loaded widgets
        /// </summary>
        public List<IWidget> GetLoadedWidgets()
        {
            return new List<IWidget>(_loadedWidgets);
        }
    }
}