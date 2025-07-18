using System;

namespace gtt_sidebar.Widgets.Notes
{
    /// <summary>
    /// Helper class with markdown examples for users
    /// </summary>
    public static class MarkdownHelper
    {
        /// <summary>
        /// Gets sample markdown text to help users understand formatting
        /// </summary>
        public static string GetSampleMarkdown()
        {
            return @"# Main Header
This is a main header with large bold text.

## Sub Header
This is a smaller sub-header.

You can make text **bold** by wrapping it with double asterisks.

You can make text *italic* by wrapping it with single asterisks.

You can combine **bold** and *italic* formatting.

Examples:
- **Important note**: This is bold
- *Emphasis*: This is italic
- **Task 1**: Call **John** at *3:00 PM*
- **Password**: *secret123* (remember this!)

## Quick Tips
- Type **text** for bold
- Type *text* for italic  
- Type # Header for large headers
- Type ## Header for smaller headers

Try editing this text to see the formatting in action!";
        }

        /// <summary>
        /// Gets help text for markdown syntax
        /// </summary>
        public static string GetMarkdownHelp()
        {
            return @"## Markdown Formatting Help

**Bold Text**: **your text**
*Italic Text*: *your text*
# Large Header: # Your Header
## Small Header: ## Your Header

Examples:
**URGENT**: Call dentist at *2 PM*
# Shopping List
## Groceries
- **Milk** (2% fat)
- *Bread* (whole wheat)";
        }
    }
}