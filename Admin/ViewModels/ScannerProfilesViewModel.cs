using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Admin.Services;
using Admin.Tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Libs.Data.Models;
using static Admin.Tools.UiTools;

namespace Admin.ViewModels
{
    public partial class ScannerProfilesViewModel : ViewModelBase
    {
        private readonly HttpClient _httpClient = new();
        private readonly ApiService _apiService;

        [ObservableProperty]
        private ObservableCollection<ScannerProfile> profiles = new();

        [ObservableProperty]
        private ObservableCollection<string> availableStrategies = new();

        [ObservableProperty]
        private bool isLoading = true;
        public bool IsNotLoading => !IsLoading;

        private Guid? _editProfileId;
        public Guid? EditProfileId
        {
            get => _editProfileId;
            set => SetProperty(ref _editProfileId, value);
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
                    ClearProfileForm();
                }
                SetProperty(ref _selectedTabIndex, value);
            }
        }

        private bool _isEditMode = false;
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        private string _formTabText = "Add Profile";
        public string FormTabText
        {
            get => !IsEditMode ? "Add Profile" : "Edit Profile";
            set => SetProperty(ref _formTabText, value);
        }

        [ObservableProperty]
        private string profileName;

        private string? _selectedStrategy;
        public string? SelectedStrategy
        {
            get => _selectedStrategy;
            set => SetProperty(ref _selectedStrategy, value);
        }

        [ObservableProperty]
        private string description;

        public ScannerProfilesViewModel() : this(App.ApiService) { }

        public ScannerProfilesViewModel(ApiService apiService)
        {
            _apiService = apiService;
            _ = LoadProfilesAsync();
            _ = LoadStrategiesAsync();
        }

        private async Task LoadProfilesAsync()
        {
            try
            {
                IsLoading = true;
                OnPropertyChanged(nameof(IsNotLoading));

                string apiUrl = $"{_apiService.ApiAddress}/api/ScannerProfile/profiles";
                var response = await _httpClient.GetStringAsync(apiUrl);
                var profileList = JsonSerializer.Deserialize<List<ScannerProfile>>(response,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (profileList != null)
                {
                    Profiles = new ObservableCollection<ScannerProfile>(profileList);
                }
            }
            catch (Exception ex)
            {
                await UiTools.ShowMessageAsync("Error", $"Error fetching profiles: {ex.Message}", MessageType.Error);
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(IsNotLoading));
            }
        }

        private async Task LoadStrategiesAsync()
        {
            try
            {
                string apiUrl = $"{_apiService.ApiAddress}/api/ScannerProfile/strategies";
                var response = await _httpClient.GetStringAsync(apiUrl);
                var strategyList = JsonSerializer.Deserialize<List<string>>(response,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (strategyList != null)
                {
                    AvailableStrategies = new ObservableCollection<string>(strategyList);
                }
            }
            catch (Exception ex)
            {
                await UiTools.ShowMessageAsync("Error", $"Error fetching strategies: {ex.Message}", MessageType.Error);
            }
        }

        [RelayCommand]
        private async Task SubmitProfile()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ProfileName)
                    || string.IsNullOrWhiteSpace(SelectedStrategy))
                {
                    await ShowMessageAsync("Error", "Profile Name and Strategy are required.", MessageType.Error);
                    return;
                }

                if (IsEditMode)
                {
                    if (!EditProfileId.HasValue)
                    {
                        await ShowMessageAsync("Error", "Profile ID for edit missing.", MessageType.Error);
                        return;
                    }

                    string apiUrl = $"{_apiService.ApiAddress}/api/ScannerProfile/update";
                    var updateRequest = new
                    {
                        Id = EditProfileId.Value,
                        ProfileName,
                        StrategyClassName = SelectedStrategy,
                        Description
                    };

                    string jsonRequest = JsonSerializer.Serialize(updateRequest,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.PutAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        await ShowMessageAsync("Success", "Profile successfully updated!", MessageType.Success);
                        await ClearProfileForm();
                        SelectedTabIndex = 0;
                        await LoadProfilesAsync();
                    }
                    else
                    {
                        var errMsg = await response.Content.ReadAsStringAsync();
                        await ShowMessageAsync("Failure", $"Failed to update profile: {errMsg}", MessageType.Error);
                    }
                }
                else
                {
                    string apiUrl = $"{_apiService.ApiAddress}/api/ScannerProfile/add";
                    var addRequest = new
                    {
                        ProfileName,
                        StrategyClassName = SelectedStrategy,
                        Description
                    };

                    string jsonRequest = JsonSerializer.Serialize(addRequest,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        await ShowMessageAsync("Success", "Profile added successfully!", MessageType.Success);
                        await ClearProfileForm();
                        SelectedTabIndex = 0;
                        await LoadProfilesAsync();
                    }
                    else
                    {
                        var errMsg = await response.Content.ReadAsStringAsync();
                        await ShowMessageAsync("Failure", $"Failed to add profile: {errMsg}", MessageType.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("Failure", $"Error submitting profile: {ex.Message}", MessageType.Error);
            }
        }

        [RelayCommand]
        private async Task EditProfile(ScannerProfile profile)
        {
            EditProfileId = profile.Id;
            ProfileName = profile.ProfileName;
            SelectedStrategy = profile.StrategyClassName;
            Description = profile.Description ?? string.Empty;
            IsEditMode = true;
            FormTabText = "Edit Profile";
            SelectedTabIndex = 1;
        }

        [RelayCommand]
        private async Task DeleteProfile(ScannerProfile profile)
        {
            try
            {
                string apiUrl = $"{_apiService.ApiAddress}/api/ScannerProfile/delete/{profile.Id}";

                HttpResponseMessage response = await _httpClient.DeleteAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    await ShowMessageAsync("Success", "Profile deleted successfully!", MessageType.Success);
                    await LoadProfilesAsync();
                }
                else
                {
                    var errMsg = await response.Content.ReadAsStringAsync();
                    await ShowMessageAsync("Failure", $"Failed to delete profile: {errMsg}", MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("Failure", $"Error deleting profile: {ex.Message}", MessageType.Error);
            }
        }

        [RelayCommand]
        private async Task ClearProfileForm()
        {
            EditProfileId = null;
            ProfileName = string.Empty;
            SelectedStrategy = null;
            Description = string.Empty;
            IsEditMode = false;
            FormTabText = "Add Profile";
        }
    }
}
