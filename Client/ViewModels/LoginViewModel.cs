using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Client.Services;
using System.Threading.Tasks;

namespace Client.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly ApiService _apiService;
        private readonly AuthService _authService;
        private readonly TokenService _tokenService;
        private readonly MainWindowViewModel _mainWindowVm;

        [ObservableProperty]
        private string? username;
        [ObservableProperty]
        private string? password;
        [ObservableProperty]
        private string? errorMessage;

        public LoginViewModel(ApiService apiService,
            AuthService authService,
            TokenService tokenService,
            MainWindowViewModel mainWindowVm
        )
        {
            _apiService = apiService;
            _authService = authService;
            _tokenService = tokenService;
            _mainWindowVm = mainWindowVm;
        }

        public string? ApiAddressMessage => string.IsNullOrEmpty(_apiService.ApiAddress)
            ? "API address is not configured. Please set the API address in settings."
            : null;

        [RelayCommand]
        private async Task LoginAsync()
        {
            ErrorMessage = null;

            var token = await _authService.AuthenticateAsync(Username, Password);
            if (string.IsNullOrEmpty(token))
            {
                ErrorMessage = "Invalid username or password.";
                return;
            }

            _tokenService.JwtToken = token;
            _mainWindowVm.OnLoginSuccessful();
            // _mainWindowVm.Navigate("Dashboard");
        }
    }
}
