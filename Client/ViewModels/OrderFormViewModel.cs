using Avalonia.Input;
using Client.Services;
using Client.Tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Libs.Data.Models;
using Libs.Enums;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Client.Tools.UiTools;

namespace Client.ViewModels;

public partial class OrderFormViewModel : ViewModelBase
{
    private readonly HttpClient _httpClient = new();
    private readonly ScannerService _scannerService;
    private readonly ApiService _apiService;
    private readonly AuthService _authService;

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    [ObservableProperty]
    private string orderId = string.Empty;

    [ObservableProperty]
    private Customer? customer;

    [ObservableProperty]
    private List<Roll> rolls = new List<Roll>();

    [ObservableProperty]
    private string customerInitials = string.Empty;

    [ObservableProperty]
    private string orderNumber = string.Empty;

    [ObservableProperty]
    private string firstRollNumber = string.Empty;

    [ObservableProperty]
    private string rollCount = string.Empty;
    public Scanner? SelectedScanner => _scannerService.SelectedScanner;

    public OrderFormViewModel() : this(App.ApiService, App.ScannerService, App.AuthService) { }

    public OrderFormViewModel(ApiService apiService, ScannerService scannerService, AuthService authService)
    {
        NumberOnlyCommand = new RelayCommand<KeyEventArgs>(OnNumberOnlyKeyPress);
        // Load the currently selected scanner from SettingsViewModel
        _scannerService = scannerService;
        _apiService = apiService;
        _authService = authService;
    }

    public RelayCommand<KeyEventArgs> NumberOnlyCommand { get; }

    private void OnNumberOnlyKeyPress(KeyEventArgs e)
    {
        // Allow only numbers and control keys (Backspace, Delete, Arrow keys)
        if (!(e.Key >= Key.D0 && e.Key <= Key.D9) &&  // Top row numbers
            !(e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) &&  // Numpad numbers
            !(e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Tab ||
              e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Enter))
        {
            e.Handled = true;  // Block invalid input
        }
    }

    [RelayCommand]
    public async Task SubmitOrderAsync()
    {
        try
        {
            if (SelectedScanner == null)
            {
                await ShowMessageAsync("Error", "No scanner selected for order. Please choose a scanner in Settings", MessageType.Error);
            }

            if (string.IsNullOrWhiteSpace(OrderId)
                || string.IsNullOrWhiteSpace(CustomerInitials)
                || string.IsNullOrWhiteSpace(FirstRollNumber)
                || string.IsNullOrWhiteSpace(RollCount))
            {
                await ShowMessageAsync("Error", "Please fill out all fields", MessageType.Error);
                return;
            }

            for (int i = 0; i < int.Parse(RollCount); i++)
            {
                rolls.Add(new Roll
                {
                    RollNumber = long.Parse(FirstRollNumber) + i, // ✅ Incrementing roll numbers
                    Status = RollStatus.Created, // ✅ Default status (modify if needed)
                });
            }

            var submitOrderRequest = new
            {
                OrderId,
                Scanner = SelectedScanner, // ✅ Pass the selected scanner
                Customer,
                CustomerInitials,
                Rolls
            };

            string apiUrl = $"{_apiService.ApiAddress}/api/Order/submit"; // ✅ Replace with actual API URL


            string jsonRequest = JsonSerializer.Serialize(submitOrderRequest,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            
            _apiService.AddAuthHeader();
            // HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
            HttpResponseMessage response = await _apiService._httpClient.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                await ShowMessageAsync("Success", "Order submitted successfully!", MessageType.Success);
                ClearForm();
            }
            else
            {
                var errMsg = await response.Content.ReadAsStringAsync();
                await ShowMessageAsync("Failure", $"[Failed to submit order]: {errMsg}", MessageType.Error);
            }
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Failure", $"Error submitting order: {ex.Message}", MessageType.Error);
        }
        finally
        {
            rolls.Clear();
        }
    }

    [RelayCommand]
    private void ClearForm()
    {
        CustomerInitials = string.Empty;
        OrderNumber = string.Empty;
        OrderId = string.Empty;
        FirstRollNumber = string.Empty;
        RollCount = string.Empty;
    }
}
