using CommunityToolkit.Mvvm.ComponentModel;
using Libs.Data.Models;
using Client.Services;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using System;
using Client.Tools;

namespace Client.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly HttpClient _httpClient = new();
    private readonly ScannerService _scannerService;
    private readonly ApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<Scanner> scanners = new();

    [ObservableProperty]
    private bool isLoading = true; // ✅ Re-added IsLoading

    public bool IsNotLoading => !IsLoading; // ✅ Re-added IsNotLoading


    public Scanner? SelectedScanner
    {
        get => _scannerService.SelectedScanner;
        set
        {
            if (_scannerService.SelectedScanner != value)
            {
                _scannerService.SelectedScanner = value;
                OnPropertyChanged(nameof(SelectedScanner));
                SaveSelectedScanner(); // ✅ Save whenever scanner changes
            }
        }
    }

    public string ApiAddress
    {
        get => _apiService.ApiAddress;
        set => _apiService.ApiAddress = value; // ✅ Automatically saves when changed
    }

    public SettingsViewModel() : this(App.ApiService,App.ScannerService) { }

    public SettingsViewModel(ApiService apiService, ScannerService scannerService)
    {
        _apiService = apiService;
        _scannerService = scannerService;
        LoadScannersAsync();
    }

    #region Scanner
    private async void LoadScannersAsync()
    {
        try
        {
            IsLoading = true;
            OnPropertyChanged(nameof(IsNotLoading));

            string apiUrl = $"{_apiService.ApiAddress}/api/Scanner/Scanners"; // ✅ Replace with actual API URL
            var response = await _httpClient.GetStringAsync(apiUrl);
            var scannerList = JsonSerializer.Deserialize<List<Scanner>>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (scannerList != null)
            {
                Scanners = new ObservableCollection<Scanner>(scannerList);

                // ✅ Ensure selected scanner is valid
                if (_scannerService.SelectedScanner == null || !Scanners.Where(s => s.Id == _scannerService.SelectedScanner.Id).Any())
                {
                    _scannerService.SelectedScanner = Scanners.FirstOrDefault();
                    SaveSelectedScanner();
                }
                else
                {
                    SelectedScanner = Scanners.Where(s => s.Id == _scannerService.SelectedScanner.Id).FirstOrDefault();
                    OnPropertyChanged(nameof(SelectedScanner));
                    // SaveSelectedScanner();
                }

            }
        }
        catch (HttpRequestException ex)
        {
            System.Console.WriteLine($"Error fetching scanners: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsNotLoading));
        }
    }
    private void SaveSelectedScanner()
    {
        _scannerService.SaveSelectedScanner(); // ✅ Delegate saving to ScannerService
    }
    #endregion

    #region API
    [RelayCommand]
    public async Task TestApiAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiAddress))
        {
            Console.WriteLine("API Address is required.");
            return;
        }

        try
        {
            var response = await _httpClient.GetAsync($"{ApiAddress}/api/ping");
            if (response.IsSuccessStatusCode)
            {
                await UiTools.ShowMessageAsync("Success", "API is reachable.", UiTools.MessageType.Success);
            }
            else
            {
                await UiTools.ShowMessageAsync("Error", $"[API test failed]: {response.Content}", UiTools.MessageType.Error);
            }
        }
        catch (Exception ex)
        {
            await UiTools.ShowMessageAsync("Error",$"[Error]: {ex.Message}", UiTools.MessageType.Error);
        }
    }
    #endregion
}
