using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Client.Views;
using Client.ViewModels;
using Client.Services;

namespace Client.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private object? _currentView;

    private readonly ApiService _apiService;
    private readonly ScannerService _scannerService;

    public MainWindowViewModel(ApiService apiService, ScannerService scannerService)
    {
        _apiService = apiService;
        _scannerService = scannerService;
        Navigate("Dashboard"); // ✅ Open Dashboard by default
    }

    [RelayCommand]
    private void Navigate(string viewName)
    {
        CurrentView = viewName switch
        {
            "Dashboard" => new Dashboard { DataContext = new DashboardViewModel(_apiService) },
            "OrderForm" => new OrderForm { DataContext = new OrderFormViewModel(_apiService, _scannerService) },
            "Settings" => new Settings { DataContext = new SettingsViewModel(_apiService, _scannerService) },
            _ => CurrentView
        };
    }
}



// using CommunityToolkit.Mvvm.ComponentModel;
// using CommunityToolkit.Mvvm.Input;
// using Client.Views;

// namespace Client.ViewModels;

// public partial class MainWindowViewModel : ViewModelBase
// {
//     [ObservableProperty]
//     private object? _currentView;

//     public MainWindowViewModel()
//     {
//         Navigate("Dashboard"); // Open Dashboard by default
//     }

//     [RelayCommand]
//     private void Navigate(string viewName)
//     {
//         CurrentView = viewName switch
//         {
//             "Dashboard" => new Dashboard(),
//             "OrderForm" => new OrderForm(),
//             "Settings" => new Settings(),
//             _ => CurrentView
//         };
//     }
// }
