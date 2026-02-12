using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        private Guid? _editScannerId;
        public Guid? EditScannerId
        {
            get => _editScannerId;
            set => SetProperty(ref _editScannerId, value);
        }

        private int _selectedTabIndex = 0;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (value == 0)
                {
                    IsEditMode = false;
                    ClearScannerForm();
                }

                SetProperty(ref _selectedTabIndex, value);
            }
        }

        private void OnTabIndexChange(int val)
        {
            if (val == 0)
            {
                IsEditMode = false;
                ClearScannerForm();
            }
            SetProperty(ref _selectedTabIndex, val);
        }

        private bool _isEditMode = false;
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        private string _formTabText = "Add Scanner";
        public string FormTabText
        {
            get => !IsEditMode ? "Add Scanner" : "Edit Scanner";
            set => SetProperty(ref _formTabText, value);
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

        // --- Profile Selection ---
        [ObservableProperty]
        private ObservableCollection<ScannerProfile> profiles = new();

        private ScannerProfile? _selectedProfile;
        public ScannerProfile? SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                SetProperty(ref _selectedProfile, value);
                if (value != null)
                    _ = LoadProfileConfigurationsAsync(value.Id);
                else
                    ProfileConfigurations = new();
                OnPropertyChanged(nameof(HasProfileSelected));
                OnPropertyChanged(nameof(ShowAutoProcessDelay));
            }
        }

        public bool HasProfileSelected => SelectedProfile != null;
        public bool ShowAutoProcessDelay => SelectedProfile?.StrategyClassName == "NoritsuControllerStrategy";

        [ObservableProperty]
        private ObservableCollection<ProfileConfiguration> profileConfigurations = new();

        [ObservableProperty]
        private string? autoProcessDelaySeconds;

        public ScannersViewModel() : this(App.ApiService) { }

        public ScannersViewModel(ApiService apiService)
        {
            _apiService = apiService;

            SelectFolderCommand = new RelayCommand<string>(async (propertyName) => await SelectFolderAsync(propertyName));

            LoadScannersAsync();
            _ = LoadProfilesAsync();
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

        private async Task LoadProfilesAsync()
        {
            try
            {
                string apiUrl = $"{_apiService.ApiAddress}/api/Scanner/profiles";
                var response = await _httpClient.GetStringAsync(apiUrl);
                var profileList = JsonSerializer.Deserialize<List<ScannerProfile>>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (profileList != null)
                {
                    Profiles = new ObservableCollection<ScannerProfile>(profileList);
                }
            }
            catch (Exception ex)
            {
                await UiTools.ShowMessageAsync("Error", $"Error fetching profiles: {ex.Message}", MessageType.Error);
            }
        }

        private async Task LoadProfileConfigurationsAsync(Guid profileId)
        {
            try
            {
                string apiUrl = $"{_apiService.ApiAddress}/api/Scanner/profile-configurations/{profileId}";
                var response = await _httpClient.GetStringAsync(apiUrl);
                var configList = JsonSerializer.Deserialize<List<ProfileConfiguration>>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (configList != null)
                {
                    ProfileConfigurations = new ObservableCollection<ProfileConfiguration>(configList);
                }
            }
            catch (Exception ex)
            {
                await UiTools.ShowMessageAsync("Error", $"Error fetching profile configurations: {ex.Message}", MessageType.Error);
            }
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
            catch (Exception ex)
            {
                await UiTools.ShowMessageAsync("Error", $"Error selecting directory: {ex.Message}", MessageType.Error);
            }

        }

        [RelayCommand]
        private async Task SubmitScanner()
        {
            try
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

                string apiUrl = !IsEditMode ? $"{_apiService.ApiAddress}/api/Scanner/add" : $"{_apiService.ApiAddress}/api/Scanner/update";

                if (IsEditMode)
                {
                    if (!EditScannerId.HasValue)
                    {
                        await ShowMessageAsync("Error", "Scanner ID for edit missing", MessageType.Error);
                        return;
                    }
                    int? delaySeconds = null;
                    if (int.TryParse(AutoProcessDelaySeconds, out int parsed))
                        delaySeconds = parsed;

                    var EditScannerRequest = new
                    {
                        Scnr = new
                        {
                            Id = EditScannerId.Value,
                            ScannerName = ScannerName,
                            Make = ScannerMake,
                            Model = ScannerModel,
                            WatchedDir = WatchedFolderPath,
                            DestinationDir = DestFolderPath,
                            ArchiveDir = ArchiveFolderPath,
                            ArtistName = ArtistName,
                            ProfileId = SelectedProfile?.Id,
                            AutoProcessDelaySeconds = delaySeconds,
                        }
                    };

                    string jsonRequest = JsonSerializer.Serialize(EditScannerRequest,
                                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                                    );
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        // Save profile configurations if any were loaded
                        await SaveProfileConfigurationsAsync();

                        await ShowMessageAsync("Success", "Scanner successfully updated!", MessageType.Success);
                        await ClearScannerForm();
                        SelectedTabIndex = 0;
                        await LoadScannersAsync();
                    }
                    else
                    {
                        await ShowMessageAsync("Failure", $"[Failed to update scanner]: {response.Content}", MessageType.Error);
                    }
                }
                else
                {
                    int? delaySeconds = null;
                    if (int.TryParse(AutoProcessDelaySeconds, out int addParsed))
                        delaySeconds = addParsed;

                    var AddScannerRequest = new
                    {
                        ScannerName,
                        Make = ScannerMake,
                        Model = ScannerModel,
                        WatchedDir = WatchedFolderPath,
                        DestinationDir = DestFolderPath,
                        ArchiveDir = ArchiveFolderPath,
                        ArtistName,
                        ProfileId = SelectedProfile?.Id,
                        AutoProcessDelaySeconds = delaySeconds
                    };
                    string jsonRequest = JsonSerializer.Serialize(AddScannerRequest,
                                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                                    );
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        await ShowMessageAsync("Success", "Scanner added successfully!", MessageType.Success);
                        await ClearScannerForm();
                        SelectedTabIndex = 0;
                        await LoadScannersAsync();
                    }
                    else
                    {
                        await ShowMessageAsync("Failure", $"[Failed to add scanner]: {response.Content}", MessageType.Error);
                    }
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
            EditScannerId = null;
            ScannerName = string.Empty;
            ScannerMake = string.Empty;
            ScannerModel = string.Empty;
            ArtistName = string.Empty;
            WatchedFolderPath = string.Empty;
            DestFolderPath = string.Empty;
            ArchiveFolderPath = string.Empty;
            SelectedProfile = null;
            AutoProcessDelaySeconds = null;
            ProfileConfigurations = new();
            IsEditMode = false;
            FormTabText = "Add Scanner";
        }

        private async Task SaveProfileConfigurationsAsync()
        {
            foreach (var config in ProfileConfigurations)
            {
                try
                {
                    string apiUrl = $"{_apiService.ApiAddress}/api/Scanner/update-profile-configuration";
                    var request = new { ConfigId = config.Id, ConfigValue = config.ConfigValue };
                    string jsonRequest = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                    await _httpClient.PostAsync(apiUrl, content);
                }
                catch (Exception ex)
                {
                    await UiTools.ShowMessageAsync("Error", $"Error saving config '{config.ConfigKey}': {ex.Message}", MessageType.Error);
                }
            }
        }

        [RelayCommand]
        private async Task EditScanner(Scanner scnr)
        {
            EditScannerId = scnr.Id;
            ScannerName = scnr.ScannerName;
            ScannerMake = scnr.Make;
            ScannerModel = scnr.Model;
            ArtistName = scnr.ArtistName;
            WatchedFolderPath = scnr.WatchedDir;
            DestFolderPath = scnr.DestinationDir;
            ArchiveFolderPath = scnr.ArchiveDir;
            AutoProcessDelaySeconds = scnr.AutoProcessDelaySeconds?.ToString();
            SelectedProfile = Profiles.FirstOrDefault(p => p.Id == scnr.ProfileId);
            IsEditMode = true;
            FormTabText = "Edit Scanner";
            SelectedTabIndex = 1;
        }
    }
}
