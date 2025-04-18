using System;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Platform;
using Client.Converters;

namespace Client.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var uri = new Uri("avares://Client/Assets/film-roll.png");
        var stream = AssetLoader.Open(uri); // Will throw if path is wrong
        Icon = new WindowIcon(stream);

        Resources["RollActionVis"] = new RollActionVisibilityMultiConver();
        Resources["InverseBooleanToVisibilityConverter"] = new InverseBooleanToVisibilityConverter();
    }
}