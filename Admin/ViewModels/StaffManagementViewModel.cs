using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Admin.Models.DTOs.Staff;
using Admin.Services;
using Admin.Tools;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Libs.Data.Models;
using Libs.Data.RequestResponse.Staff;
using static Admin.Tools.UiTools;

namespace Admin.ViewModels
{

    public partial class StaffManagementViewModel : ViewModelBase
    {
        private readonly HttpClient _httpClient = new();
        private readonly ApiService _apiService;

        [ObservableProperty]
        private ObservableCollection<Staff> staff = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PageInfo))]
        [NotifyCanExecuteChangedFor(nameof(FirstPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(PrevPageCommand))]
        private int currentPage = 1;

        [ObservableProperty]
        private int pageSize = 20;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PageInfo))]
        [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(LastPageCommand))]
        private int totalPages = 1;

        public bool CanGoPrev => CurrentPage > 1;
        public bool CanGoNext => CurrentPage < TotalPages;

        [RelayCommand(CanExecute = nameof(CanGoPrev))]
        private async Task FirstPageAsync()
        {
            if (CurrentPage != 1)
            {
                CurrentPage = 1;
                await LoadStaffAsync();
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoPrev))]
        private async Task PrevPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadStaffAsync();
            }

        }

        [RelayCommand(CanExecute = nameof(CanGoNext))]
        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadStaffAsync();
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoNext))]
        private async Task LastPageAsync()
        {
            if (CurrentPage != TotalPages)
            {
                CurrentPage = TotalPages;
                await LoadStaffAsync();
            }
        }

        public string PageInfo => $"Page {CurrentPage} of {TotalPages}";

        [ObservableProperty]
        private bool isLoading = true; // ✅ Re-added IsLoading
        public bool IsNotLoading => !IsLoading; // ✅ Re-added IsNotLoading

        public string ApiAddress
        {
            get => _apiService.ApiAddress;
            set => _apiService.ApiAddress = value; // ✅ Automatically saves when changed
        }

        private Guid? _editStaffId;
        public Guid? EditStaffId
        {
            get => _editStaffId;
            set => SetProperty(ref _editStaffId, value);
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
                    ClearStaffForm();
                }

                SetProperty(ref _selectedTabIndex, value);
            }
        }

        private void OnTabIndexChange(int val)
        {
            if (val == 0)
            {
                IsEditMode = false;
                ClearStaffForm();
            }
            SetProperty(ref _selectedTabIndex, val);
        }

        private bool _isEditMode = false;
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        private string _formTabText = "Add Staff";
        public string FormTabText
        {
            get => !IsEditMode ? "Add Staff Member" : "Edit Staff Member";
            set => SetProperty(ref _formTabText, value);
        }

        [ObservableProperty]
        private string staffFirstName;

        [ObservableProperty]
        private string staffLastName;

        [ObservableProperty]
        private string staffEmail;

        [ObservableProperty]
        private string staffPassword;

        [ObservableProperty]
        private string staffUsername;

        public StaffManagementViewModel() : this(App.ApiService) { }

        public StaffManagementViewModel(ApiService apiService)
        {
            _apiService = apiService;

            LoadStaffAsync();
        }

        [RelayCommand]
        public async Task LoadStaffAsync()
        {
            try
            {
                Staff.Clear();
                TotalPages = 1;
                IsLoading = true;
                OnPropertyChanged(nameof(IsNotLoading));

                if (string.IsNullOrWhiteSpace(_apiService.ApiAddress))
                {
                    await UiTools.ShowMessageAsync("Error", $"[Error]: API Address is not configured.", UiTools.MessageType.Error);
                    return;
                }


                // Add relevant filters here

                var apiUrl = $"{_apiService.ApiAddress}/api/staff/staff";

                var getStaffRequest = new
                {
                    page = CurrentPage,
                    PageSize
                };


                // new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                string jsonRequest = JsonSerializer.Serialize(getStaffRequest);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    // var response = await _httpClient.GetStringAsync(apiUrl);
                    // Deserialize the response into a list of orders
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReferenceHandler = ReferenceHandler.Preserve
                    };
                    var staffDTO = JsonSerializer.Deserialize<GetStaffDTO>(json, options);

                    if (staffDTO.Staff.Any())
                    {
                        TotalPages = staffDTO.TotalPages;

                        foreach (var l in staffDTO.Staff)
                            Staff.Add(l);
                    }
                }
                else
                {
                    var errMsg = await response.Content.ReadAsStringAsync();
                    await UiTools.ShowMessageAsync("Error", errMsg, UiTools.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                await UiTools.ShowMessageAsync("Error", $"Error fetching staffs: {ex.Message}", MessageType.Error);
            }
            finally
            {
                IsLoading = false;
                // UpdatePageNumbers();
                // Tell the buttons their CanExecute state might have changed
                FirstPageCommand.NotifyCanExecuteChanged();
                PrevPageCommand.NotifyCanExecuteChanged();
                NextPageCommand.NotifyCanExecuteChanged();
                LastPageCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(IsNotLoading));
            }
        }

        [RelayCommand]
        private async Task SubmitStaff()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(StaffFirstName)
                || string.IsNullOrWhiteSpace(StaffLastName)
                || string.IsNullOrWhiteSpace(StaffEmail)
                || string.IsNullOrWhiteSpace(StaffPassword)
                || string.IsNullOrWhiteSpace(StaffUsername))
                {
                    await ShowMessageAsync("Error", "Please fill out all fields", MessageType.Error);
                    return;
                }

                string apiUrl = !IsEditMode ? $"{_apiService.ApiAddress}/api/auth/register" : $"{_apiService.ApiAddress}/api/staff/update";

                if (IsEditMode)
                {
                    if (!EditStaffId.HasValue)
                    {
                        await ShowMessageAsync("Error", "Staff ID for edit missing", MessageType.Error);
                        return;
                    }
                    var EditStaffRequest = new
                    {
                        Scnr = new
                        {
                            Id = EditStaffId.Value,
                            FirstName = StaffFirstName,
                            LastName = StaffLastName,
                            Email = StaffEmail,
                            Username = StaffUsername,
                            Password = StaffPassword
                        }
                    };

                    string jsonRequest = JsonSerializer.Serialize(EditStaffRequest,
                                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                                    );
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        await ShowMessageAsync("Success", "Staff successfully updated!", MessageType.Success);
                        await ClearStaffForm();
                        SelectedTabIndex = 0;
                        await LoadStaffAsync();
                    }
                    else
                    {
                        await ShowMessageAsync("Failure", $"[Failed to update staff]: {response.Content}", MessageType.Error);
                    }
                }
                else
                {
                    var AddStaffRequest = new
                    {
                        Id = IsEditMode ? EditStaffId : null,
                        FirstName = StaffFirstName,
                        LastName = StaffLastName,
                        Email = StaffEmail,
                        Username = StaffUsername,
                        Password = StaffPassword
                    };
                    string jsonRequest = JsonSerializer.Serialize(AddStaffRequest,
                                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                                    );
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        await ShowMessageAsync("Success", "Staff added successfully!", MessageType.Success);
                        await ClearStaffForm();
                        SelectedTabIndex = 0;
                        await LoadStaffAsync();
                    }
                    else
                    {
                        await ShowMessageAsync("Failure", $"[Failed to add staff]: {response.Content}", MessageType.Error);
                    }
                }

            }
            catch (Exception ex)
            {
                await ShowMessageAsync("Failure", $"Error submitting staff: {ex.Message}", MessageType.Error);
            }
        }

        [RelayCommand]
        private async Task ClearStaffForm()
        {
            EditStaffId = null;
            StaffFirstName = string.Empty;
            StaffLastName = string.Empty;
            StaffEmail = string.Empty;
            StaffPassword = string.Empty;
            StaffUsername = string.Empty;
            IsEditMode = false;
            FormTabText = "Add Staff";
        }

        [RelayCommand]
        private async Task EditStaff(Staff staff)
        {
            EditStaffId = staff.Id;
            StaffFirstName = staff.FirstName;
            StaffLastName = staff.LastName;
            StaffEmail = staff.Email;
            // StaffPassword = staff.PasswordHash;
            StaffUsername = staff.UserName;

            IsEditMode = true;
            FormTabText = "Edit Staff";
            SelectedTabIndex = 1;
        }
    }
}
