using InventoryManagement.Core.Enums;

namespace InventoryManagement.Console.UI;

/// <summary>
/// Utility class for color-coded console output and common UI patterns.
/// </summary>
public static class ConsoleHelper
{
    /// <summary>Writes a line in the specified color, then resets.</summary>
    public static void WriteColor(string text, ConsoleColor color)
    {
        var prev = System.Console.ForegroundColor;
        System.Console.ForegroundColor = color;
        System.Console.Write(text);
        System.Console.ForegroundColor = prev;
    }

    /// <summary>Writes a line in the specified color, then resets.</summary>
    public static void WriteLineColor(string text, ConsoleColor color)
    {
        var prev = System.Console.ForegroundColor;
        System.Console.ForegroundColor = color;
        System.Console.WriteLine(text);
        System.Console.ForegroundColor = prev;
    }

    /// <summary>Prints a success message in green.</summary>
    public static void Success(string message) => WriteLineColor($"  ✓ {message}", ConsoleColor.Green);

    /// <summary>Prints a warning message in yellow.</summary>
    public static void Warning(string message) => WriteLineColor($"  ⚠ {message}", ConsoleColor.Yellow);

    /// <summary>Prints an error message in red.</summary>
    public static void Error(string message) => WriteLineColor($"  ✗ {message}", ConsoleColor.Red);

    /// <summary>Prints an info message in cyan.</summary>
    public static void Info(string message) => WriteLineColor($"  ℹ {message}", ConsoleColor.Cyan);

    /// <summary>Prints a section header with decorative borders.</summary>
    public static void PrintHeader(string title)
    {
        var border = new string('═', Math.Max(title.Length + 4, 50));
        System.Console.WriteLine();
        WriteLineColor($"  ╔{border}╗", ConsoleColor.DarkCyan);
        WriteLineColor($"  ║  {title.PadRight(border.Length - 2)}║", ConsoleColor.DarkCyan);
        WriteLineColor($"  ╚{border}╝", ConsoleColor.DarkCyan);
        System.Console.WriteLine();
    }

    /// <summary>Prints a sub-header.</summary>
    public static void PrintSubHeader(string title)
    {
        System.Console.WriteLine();
        WriteLineColor($"  ── {title} ──", ConsoleColor.Cyan);
        System.Console.WriteLine();
    }

    /// <summary>Prints a separator line.</summary>
    public static void PrintSeparator(int width = 60)
    {
        WriteLineColor($"  {new string('─', width)}", ConsoleColor.DarkGray);
    }

    /// <summary>Gets color based on stock health level.</summary>
    public static ConsoleColor GetStockLevelColor(StockLevel level) => level switch
    {
        StockLevel.Critical => ConsoleColor.Red,
        StockLevel.Low => ConsoleColor.Yellow,
        StockLevel.Healthy => ConsoleColor.Green,
        _ => ConsoleColor.White
    };

    /// <summary>Gets the display symbol for a stock level.</summary>
    public static string GetStockLevelIndicator(StockLevel level) => level switch
    {
        StockLevel.Critical => "🔴",
        StockLevel.Low => "🟡",
        StockLevel.Healthy => "🟢",
        _ => "⚪"
    };

    /// <summary>Prompts the user for input with a label.</summary>
    public static string Prompt(string label)
    {
        WriteColor($"  {label}: ", ConsoleColor.White);
        return System.Console.ReadLine()?.Trim() ?? string.Empty;
    }

    /// <summary>Prompts the user for an integer.</summary>
    public static int? PromptInt(string label)
    {
        var input = Prompt(label);
        return int.TryParse(input, out var value) ? value : null;
    }

    /// <summary>Prompts the user for a decimal.</summary>
    public static decimal? PromptDecimal(string label)
    {
        var input = Prompt(label);
        return decimal.TryParse(input, out var value) ? value : null;
    }

    /// <summary>Displays a menu and returns the selected option number.</summary>
    public static int ShowMenu(string title, string[] options)
    {
        PrintHeader(title);
        for (int i = 0; i < options.Length; i++)
        {
            WriteColor($"  [{i + 1}] ", ConsoleColor.Cyan);
            System.Console.WriteLine(options[i]);
        }
        WriteColor($"  [0] ", ConsoleColor.DarkGray);
        System.Console.WriteLine("Back / Exit");
        System.Console.WriteLine();

        var choice = PromptInt("Select option");
        return choice ?? -1;
    }

    /// <summary>Prints a formatted table row.</summary>
    public static void PrintTableRow(string[] columns, int[] widths, ConsoleColor color = ConsoleColor.White)
    {
        var prev = System.Console.ForegroundColor;
        System.Console.ForegroundColor = color;
        System.Console.Write("  ");
        for (int i = 0; i < columns.Length; i++)
        {
            var col = columns[i].Length > widths[i]
                ? columns[i][..(widths[i] - 2)] + ".."
                : columns[i];
            System.Console.Write(col.PadRight(widths[i] + 2));
        }
        System.Console.WriteLine();
        System.Console.ForegroundColor = prev;
    }

    /// <summary>Prints a table header with underline.</summary>
    public static void PrintTableHeader(string[] columns, int[] widths)
    {
        PrintTableRow(columns, widths, ConsoleColor.Cyan);
        System.Console.Write("  ");
        for (int i = 0; i < widths.Length; i++)
        {
            WriteColor(new string('─', widths[i]) + "  ", ConsoleColor.DarkGray);
        }
        System.Console.WriteLine();
    }

    /// <summary>Waits for user to press any key.</summary>
    public static void WaitForKey()
    {
        System.Console.WriteLine();
        WriteColor("  Press any key to continue...", ConsoleColor.DarkGray);
        System.Console.ReadKey(true);
        System.Console.WriteLine();
    }

    /// <summary>Formats a currency value.</summary>
    public static string FormatCurrency(decimal value) => $"${value:N2}";

    /// <summary>Prints a progress bar.</summary>
    public static void PrintProgressBar(double percent, int width = 30)
    {
        var filled = (int)(percent / 100 * width);
        var empty = width - filled;
        var color = percent > 80 ? ConsoleColor.Red : percent > 60 ? ConsoleColor.Yellow : ConsoleColor.Green;

        WriteColor("[", ConsoleColor.DarkGray);
        WriteColor(new string('█', filled), color);
        WriteColor(new string('░', empty), ConsoleColor.DarkGray);
        WriteColor($"] {percent:F1}%", ConsoleColor.DarkGray);
    }
}
