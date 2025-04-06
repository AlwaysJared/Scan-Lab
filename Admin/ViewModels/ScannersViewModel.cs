using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Admin.Services;
using Admin.Tools;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Libs.Data.Models;
using static Admin.Tools.UiTools;

namespace Admin.ViewModels
{

    public partial class ScannersViewModel : ViewModelBase
    {
        private readonly HttpClient _httpClient = new();
        // private readonly ScannerService _scannerService;
        private readonly ApiService _apiService;

        [ObservableProperty]
        private ObservableCollection<Scanner> scanners = new();

        [ObservableProperty]
        private bool isLoading = true; // ✅ Re-added IsLoading
        public bool IsNotLoading => !IsLoading; // ✅ Re-added IsNotLoading

        public string ApiAddress
        {
            get => _apiService.ApiAddress;
            set => _apiService.ApiAddress = value; // ✅ Automatically saves when changed
        }

        [ObservableProperty]
        private string scannerName;

        [ObservableProperty]
        private string scannerMake;

        [ObservableProperty]
        private string scannerModel;

        [ObservableProperty]
        private string artistName;

        [ObservableProperty]
        private string watchedFolderPath;

        [ObservableProperty]
        private string destFolderPath;

        [ObservableProperty]
        private string archiveFolderPath;

        public ScannersViewModel() : this(App.ApiService) { }

        public ScannersViewModel(ApiService apiService)
        {
            _apiService = apiService;

            SelectFolderCommand = new RelayCommand<string>(async (propertyName) => await SelectFolderAsync(propertyName));

            // LoadScannersAsync();
        }

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

        private async Task SelectFolderAsync(string propertyName)
        {
            if (App.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            var window = desktop.MainWindow;
            var storageProvider = window?.StorageProvider;
            if (storageProvider is null) return;

            var titleFolder = "";
            switch (propertyName)
            {
                case "WatchedDir":
                    titleFolder = "Watched";
                    break;
                case "DestinationDir":
                    titleFolder = "Destination";
                    break;
                case "ArchiveDir":
                    titleFolder = "Archive";
                    break;
            }

            var result = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = $"Select {titleFolder} Folder",
                AllowMultiple = false
            });


            if (result?.Count > 0)
            {
                string selectedPath = result[0].Path.LocalPath;

                switch (propertyName)
                {
                    case "WatchedDir":
                        WatchedFolderPath = selectedPath;
                        break;
                    case "DestinationDir":
                        DestFolderPath = selectedPath; ;
                        break;
                    case "ArchiveDir":
                        ArchiveFolderPath = selectedPath; ;
                        break;
                }
            }
        }

        [RelayCommand]
        private async Task SubmitScanner()
        {
            if (string.IsNullOrWhiteSpace(ScannerName)
            || string.IsNullOrWhiteSpace(ScannerMake)
            || string.IsNullOrWhiteSpace(ScannerModel)
            || string.IsNullOrWhiteSpace(ArtistName)
            || string.IsNullOrWhiteSpace(WatchedFolderPath)
            || string.IsNullOrWhiteSpace(DestFolderPath)
            || string.IsNullOrWhiteSpace(ArchiveFolderPath))
            {
                await ShowMessageAsync("Error", "Please fill out all fields", MessageType.Error);
                return;
            }

            var addScannerRequest = new
            {
                ScannerName,
                Make = ScannerMake,
                Model = ScannerModel,
                WatchedDir = WatchedFolderPath,
                DestinationDir = DestFolderPath,
                ArchiveDir = ArchiveFolderPath,
                ArtistName
            };

            string apiUrl = $"{_apiService.ApiAddress}/api/Scanner/add"; // ✅ Replace with actual API URL

            try
            {
                string jsonRequest = JsonSerializer.Serialize(addScannerRequest,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                );
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    await ShowMessageAsync("Success", "Scanner added successfully!", MessageType.Success);
                    await ClearScannerForm();
                }
                else
                {
                    await ShowMessageAsync("Failure", $"[Failed to add scanner]: {response.Content}", MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("Failure", $"Error submitting scanner: {ex.Message}", MessageType.Error);
            }
        }

        [RelayCommand]
        private async Task ClearScannerForm()
        {
            ScannerName = string.Empty;
            ScannerMake = string.Empty;
            ScannerModel = string.Empty;
            ArtistName = string.Empty;
            WatchedFolderPath = string.Empty;
            DestFolderPath = string.Empty;
            ArchiveFolderPath = string.Empty;
        }
    }
}
