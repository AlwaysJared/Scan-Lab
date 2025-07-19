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

    public Scanner? refScanner = new Scanner
    {
        ScannerName = "",
        WatchedDir = "",
        DestinationDir = "",
        ArchiveDir = "",
    };
    // {
    //     get; set;
    //     // get => _scannerService.SelectedScanner;
    //     // set
    //     // {
    //     //     if (_scannerService.SelectedScanner != value)
    //     //     {
    //     //         _scannerService.SelectedScanner = value;
    //     //         OnPropertyChanged(nameof(refScanner));
    //     //         SaveSelectedScanner(); // ✅ Save whenever scanner changes
    //     //     }
    //     // }
    // }

    public Scanner? SelectedScanner
    {
        get => _scannerService.SelectedScanner;
        set
        {
            if (_scannerService.SelectedScanner != value)
            {
                _scannerService.SelectedScanner = value;
                // refScanner = value;
                WatchedFolderPath = _scannerService.SelectedScanner != null ? _scannerService.SelectedScanner.WatchedDir : "";
                DestFolderPath = _scannerService.SelectedScanner != null ? _scannerService.SelectedScanner.DestinationDir : "";
                ArchiveFolderPath = _scannerService.SelectedScanner != null ? _scannerService.SelectedScanner.ArchiveDir : "";

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

    private string _watchedfolderPath;
    // Property to bind to the TextBox
    public string WatchedFolderPath
    {
        get => _scannerService.SelectedScanner != null ? _scannerService.SelectedScanner.WatchedDir : "";
        set => SetProperty(ref _watchedfolderPath, value); // SetProperty is provided by ObservableObject
    }

    private string _destfolderPath;
    // Property to bind to the TextBox
    public string DestFolderPath
    {
        get => _scannerService.SelectedScanner != null ? _scannerService.SelectedScanner.DestinationDir : "";
        set => SetProperty(ref _destfolderPath, value); // SetProperty is provided by ObservableObject
    }

    private string _archivefolderPath;
    // Property to bind to the TextBox
    public string ArchiveFolderPath
    {
        get => _scannerService.SelectedScanner != null ? _scannerService.SelectedScanner.ArchiveDir : "";
        set => SetProperty(ref _archivefolderPath, value); // SetProperty is provided by ObservableObject
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
                Scanners = new();
                Scanners = new ObservableCollection<Scanner>(scannerList);

                // ✅ Ensure selected scanner is valid
                if (_scannerService.SelectedScanner == null || !Scanners.Where(s => s.Id == _scannerService.SelectedScanner.Id).Any())
                {
                    if (refScanner?.Id != new Guid())
                    {
                        SelectedScanner = Scanners.Where(s => s.Id == refScanner.Id).FirstOrDefault();
                        OnPropertyChanged(nameof(SelectedScanner));
                    }
                    else
                    {
                        _scannerService.SelectedScanner = Scanners.FirstOrDefault();
                        SelectedScanner = Scanners.FirstOrDefault();
                    }

                    SaveSelectedScanner();
                }
                else
                {
                    SelectedScanner = Scanners.Where(s => s.Id == _scannerService.SelectedScanner.Id).FirstOrDefault();
                    OnPropertyChanged(nameof(SelectedScanner));
                    // SaveSelectedScanner();
                }

                refScanner.Id = _scannerService.SelectedScanner?.Id ?? new Guid();
                refScanner.Make = _scannerService.SelectedScanner?.Make ?? "";
                refScanner.Model = _scannerService.SelectedScanner?.Model ?? "";
                refScanner.ScannerName = _scannerService.SelectedScanner?.ScannerName ?? "";
                refScanner.ArtistName = _scannerService.SelectedScanner?.ArtistName ?? "";
                refScanner.WatchedDir = _scannerService.SelectedScanner?.WatchedDir ?? "";
                refScanner.DestinationDir = _scannerService.SelectedScanner?.DestinationDir ?? "";
                refScanner.ArchiveDir = _scannerService.SelectedScanner?.ArchiveDir ?? "";
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
        try
        {
            if (App.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            var window = desktop.MainWindow;
            var storageProvider = window?.StorageProvider;
            if (storageProvider is null) return;

            var titleFolder = "";
            var startDir = "";
            switch (propertyName)
            {
                case "WatchedDir":
                    titleFolder = "Watched";
                    startDir = WatchedFolderPath;
                    break;
                case "DestinationDir":
                    titleFolder = "Destination";
                    startDir = DestFolderPath;
                    break;
                case "ArchiveDir":
                    titleFolder = "Archive";
                    startDir = ArchiveFolderPath;
                    break;
            }

            var options = new FolderPickerOpenOptions
            {
                Title = $"Select {titleFolder} Folder",
                AllowMultiple = false,
                SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(startDir)
            };

            var result = await storageProvider.OpenFolderPickerAsync(options);

            if (SelectedScanner != null && result?.Count > 0)
            {
                // string selectedPath = result[0].Path.LocalPath;
                var folderUri = result[0].Path;

                string selectedPath;
                if (folderUri.IsAbsoluteUri)
                {
                    selectedPath = folderUri.LocalPath;
                }
                else
                {
                    // Fall back to .ToString() for relative or UNC paths
                    selectedPath = folderUri.ToString();
                }

                switch (propertyName)
                {
                    case "WatchedDir":
                        SelectedScanner.WatchedDir = selectedPath;
                        WatchedFolderPath = selectedPath;
                        OnPropertyChanged(nameof(SelectedScanner));
                        break;
                    case "DestinationDir":
                        SelectedScanner.DestinationDir = selectedPath;
                        DestFolderPath = selectedPath;
                        OnPropertyChanged(nameof(SelectedScanner));
                        break;
                    case "ArchiveDir":
                        SelectedScanner.ArchiveDir = selectedPath;
                        ArchiveFolderPath = selectedPath;
                        OnPropertyChanged(nameof(SelectedScanner));
                        break;
                }
            }

        }
        catch (Exception ex)
        {
            await UiTools.ShowMessageAsync("Error", ex.Message, MessageType.Error);
        }
    }

    [RelayCommand]
    private async Task UpdateScanner()
    {
        try
        {
            var apiUrl = $"{_apiService.ApiAddress}/api/Scanner/update";

            var updateScannerRequest = new
            {
                Scnr = SelectedScanner
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
            else
            {
                var errMsg = await response.Content.ReadAsStringAsync();
                await UiTools.ShowMessageAsync("Error", $"[Error]: {errMsg}", MessageType.Error);
            }
        }
        catch (Exception ex)
        {

            await UiTools.ShowMessageAsync("Error", ex.Message, MessageType.Error);
        }

    }

    [RelayCommand]
    private async Task CancelUpdateScanner()
    {
        try
        {
            SelectedScanner.Id = refScanner?.Id ?? new Guid();
            SelectedScanner.Make = refScanner?.Make ?? "";
            SelectedScanner.Model = refScanner?.Model ?? "";
            SelectedScanner.ScannerName = refScanner?.ScannerName ?? "";
            SelectedScanner.ArtistName = refScanner?.ArtistName ?? "";
            SelectedScanner.WatchedDir = refScanner?.WatchedDir ?? "";
            SelectedScanner.DestinationDir = refScanner?.DestinationDir ?? "";
            SelectedScanner.ArchiveDir = refScanner?.ArchiveDir ?? "";
            WatchedFolderPath = refScanner.WatchedDir;
            DestFolderPath = refScanner.DestinationDir;
            ArchiveFolderPath = refScanner.ArchiveDir;
            OnPropertyChanged(nameof(SelectedScanner));
        }
        catch (Exception ex)
        {
            await UiTools.ShowMessageAsync("Error", ex.Message, MessageType.Error);
        }

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
                var errMsg = await response.Content.ReadAsStringAsync();
                await UiTools.ShowMessageAsync("Error", $"[API test failed]: {errMsg}", UiTools.MessageType.Error);
            }
        }
        catch (Exception ex)
        {
            await UiTools.ShowMessageAsync("Error", $"[Error]: {ex.Message}", UiTools.MessageType.Error);
        }
    }
    #endregion
}
