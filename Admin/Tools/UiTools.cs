using Avalonia.Controls;
using Avalonia.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform;
using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;

namespace Admin.Tools;

public static class UiTools
{
    public static async Task ShowMessageAsync(string title, string message, MessageType type)
    {
        var window = new Window
        {
            Title = title,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = Math.Min(400, MeasureTextWidth(message) + 100), // ✅ Adjust width based on text
            Height = MeasureTextHeight(message) + 100, // ✅ Adjust height based on text
            Icon = GetWindowIcon(type) // ✅ Set the app icon based on message type
        };

        var textBlock = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            MinWidth = 200,
            MaxWidth = 400,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(10)
        };

        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 0)
        };

        okButton.Click += (_, _) => Dispatcher.UIThread.Post(() => window.Close());

        var stackPanel = new StackPanel
        {
            Children = { textBlock, okButton },
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        window.Content = stackPanel;

        var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (mainWindow != null)
        {
            await Dispatcher.UIThread.InvokeAsync(async () => await window.ShowDialog(mainWindow));
        }
    }

    private static WindowIcon GetWindowIcon(MessageType type)
    {
        string iconPath = type switch
        {
            MessageType.Success => "Assets/success.png", // ✅ Success icon
            MessageType.Error => "Assets/error.png",     // ✅ Error icon
            _ => "Assets/info.png"                      // ✅ Info icon
        };

        return new WindowIcon(AssetLoader.Open(new Uri($"avares://Admin/{iconPath}")));
    }


    private static double MeasureTextWidth(string text)
    {
        return text.Length * 7; // ✅ Approximate width (7px per character)
    }

    private static double MeasureTextHeight(string text)
    {
        int lineCount = (int)Math.Ceiling((double)MeasureTextWidth(text) / 400);
        return lineCount * 20; // ✅ Approximate height (20px per line)
    }

    public enum MessageType
    {
        Info,
        Success,
        Error
    }
}
