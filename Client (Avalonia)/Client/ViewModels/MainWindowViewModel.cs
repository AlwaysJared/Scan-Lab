using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Client.Views;

namespace Client.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private object? _currentView;

    public MainWindowViewModel()
    {
        Navigate("Dashboard"); // Open Dashboard by default
    }

    [RelayCommand]
    private void Navigate(string viewName)
    {
        CurrentView = viewName switch
        {
            "Dashboard" => new Dashboard(),
            "OrderForm" => new OrderForm(),
            "Settings" => new Settings(),
            _ => CurrentView
        };
    }
}
