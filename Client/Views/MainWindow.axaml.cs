using Avalonia.Controls;
using Client.Converters;

namespace Client.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Resources["RollActionVis"] = new RollActionVisibilityMultiConver();
    }
}