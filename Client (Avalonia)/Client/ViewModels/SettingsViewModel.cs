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
using static Client.Tools.UiTools;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Converters;
using System.Text;

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

    public Scanner? refScanner
    {
        get => _scannerService.SelectedScanner;
        set
        {
            if (_scannerService.SelectedScanner != value)
            {
                _scannerService.SelectedScanner = value;
                OnPropertyChanged(nameof(refScanner));
                SaveSelectedScanner(); // ✅ Save whenever scanner changes
            }
        }
    }

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

    public SettingsViewModel() : this(App.ApiService, App.ScannerService) { }

    public SettingsViewModel(ApiService apiService, ScannerService scannerService)
    {
        _apiService = apiService;
        _scannerService = scannerService;
        SelectedScanner = _scannerService.SelectedScanner;

        SelectFolderCommand = new RelayCommand<string>(async (propertyName) => await SelectFolderAsync(propertyName));

        LoadScannersAsync();
    }

    #region Scanner
    public IRelayCommand<string> SelectFolderCommand { get; }
    private async Task LoadScannersAsync()
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
        catch (Exception ex)
        {
            await UiTools.ShowMessageAsync("Error", $"Error fetching scanners: {ex.Message}", MessageType.Error);
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
    private async Task SelectFolderAsync(string propertyName)
    {
        if (App.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var window = desktop.MainWindow;
        var storageProvider = window?.StorageProvider;
        if (storageProvider is null) return;

        var result = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Folder",
            AllowMultiple = false
        });


        if (SelectedScanner != null && result?.Count > 0)
        {
            string selectedPath = result[0].Path.LocalPath;

            switch (propertyName)
            {
                case "WatchedDir":
                    SelectedScanner.WatchedDir = selectedPath;
                    OnPropertyChanged(nameof(SelectedScanner));
                    break;
                case "DestinationDir":
                    SelectedScanner.DestinationDir = selectedPath;
                    OnPropertyChanged(nameof(SelectedScanner));
                    break;
                case "ArchiveDir":
                    SelectedScanner.ArchiveDir = selectedPath;
                    OnPropertyChanged(nameof(SelectedScanner));
                    break;
            }
        }
    }
    [RelayCommand]
    private async void UpdateScanner()
    {
        var apiUrl = $"{_apiService.ApiAddress}/api/Scanner/update";

        var updateScannerRequest = new
        {
            Scanner = SelectedScanner
        };


        // new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        string jsonRequest = JsonSerializer.Serialize(updateScannerRequest);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            await UiTools.ShowMessageAsync("Success", "Scanner successfully updated", MessageType.Success);
            await LoadScannersAsync();
        }
        else{
            await UiTools.ShowMessageAsync("Error", $"[Error]: {response.Content}", MessageType.Error);
        }
    }
    [RelayCommand]
    private async void CancelUpdateScanner()
    {
        SelectedScanner = refScanner;
        OnPropertyChanged(nameof(SelectedScanner));
    }
    #endregion

    #region API
    [RelayCommand]
    public async Task TestApiAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiAddress))
        {
            await UiTools.ShowMessageAsync("Error", "API Address is required.", MessageType.Error);
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
            await UiTools.ShowMessageAsync("Error", $"[Error]: {ex.Message}", UiTools.MessageType.Error);
        }
    }
    #endregion
}
