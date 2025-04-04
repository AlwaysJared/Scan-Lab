using Admin.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Admin.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private object? _currentView;

    public MainWindowViewModel()
    {
        Navigate("Dashboard");
    }

    [RelayCommand]
    private void Navigate(string viewName)
    {
        CurrentView = viewName switch
        {
            "Orders" => new Orders { DataContext = new OrdersViewModel() },
            "Scanners" => new Scanners { DataContext = new Scanners() },
            "Dashboard" => new Dashboard { DataContext = new Dashboard() },
            _ => CurrentView
        };
    }
}
