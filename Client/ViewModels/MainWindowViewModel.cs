using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Client.Views;
using Client.ViewModels;
using Client.Services;
using Client.Tools;
using System.Threading.Tasks;

namespace Client.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private bool _isLoggedIn;

    private readonly ApiService _apiService;
    private readonly ScannerService _scannerService;
    private readonly TokenService _tokenService;
    private readonly AuthService _authService;

    public MainWindowViewModel(ApiService apiService,
        ScannerService scannerService,
        TokenService tokenService,
        AuthService authService
    )
    {
        _apiService = apiService;
        _scannerService = scannerService;
        _tokenService = tokenService;
        _authService = authService;
        // Navigate("Dashboard"); // ✅ Open Dashboard by default
        UpdateLoginState();
        ShowLogin();
    }

    private void UpdateLoginState()
    {
        IsLoggedIn = _tokenService.HasValidToken();
    }

    private void ShowLogin()
    {
        CurrentView = new Login { DataContext = new LoginViewModel(_apiService, _authService, _tokenService, this) };
    }

    public void OnLoginSuccessful()
    {
        UpdateLoginState();
        Navigate("Dashboard");
    }

    [RelayCommand]
    public void Logout()
    {
        _tokenService.ClearToken();
        UpdateLoginState();
        ShowLogin();
    }

    [RelayCommand]
    public async Task Navigate(string viewName)
    {
        if (!_tokenService.HasValidToken())
        {
            await UiTools.ShowMessageAsync("Error", "Session expired. Please log in again to continue", UiTools.MessageType.Error);
            UpdateLoginState();
            ShowLogin();
            return;
        }

        if (CurrentView != null)
            if (CurrentView.GetType().Name.ToLower().Contains(viewName.ToLower()))
                return;
        
        CurrentView = viewName switch
        {
            "Dashboard" => new Dashboard { DataContext = new DashboardViewModel(_apiService, _scannerService, _authService, this) },
            "OrderForm" => new OrderForm { DataContext = new OrderFormViewModel(_apiService, _scannerService, _authService) },
            "Settings" => new Settings { DataContext = new SettingsViewModel(_apiService, _scannerService, _authService) },
            _ => CurrentView
        };
    }
}
