using Admin.Services;
using Admin.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Admin.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private object? _currentView;

    private readonly ApiService _apiService;

    public MainWindowViewModel(ApiService apiService)
    {
        _apiService = apiService;
        Navigate("Dashboard"); // ✅ Open Dashboard by default
    }


    [RelayCommand]
    private void Navigate(string viewName)
    {
        if(CurrentView != null)
            if(CurrentView.GetType().Name.ToLower().Contains(viewName.ToLower()))
                return;

        CurrentView = viewName switch
        {
            "Orders" => new Orders { DataContext = new OrdersViewModel() },
            "Scanners" => new Scanners { DataContext = new ScannersViewModel(_apiService) },
            "Dashboard" => new Dashboard { DataContext = new DashboardViewModel() },
            "Settings" => new Settings { DataContext = new SettingsViewModel(_apiService) },
            _ => CurrentView
        };
    }
}
