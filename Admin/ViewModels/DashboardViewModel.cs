using Admin.Models.DTOs.Staff;
using Admin.Services;
using Admin.Tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Libs.Data.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Libs.Data.DTOs.Analytics;

namespace Admin.ViewModels
{
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly ApiService _apiService;
        public ObservableCollection<TableItem> TableItems { get; } = new ObservableCollection<TableItem>();

        [ObservableProperty]
        private bool isDropdownOpen;

        public ObservableCollection<OptionItem> AvailableOptions { get; } = new();

        public Dictionary<string, bool> SelectedOptionsDict { get; } = new();

        // Convenience property for button content
        public string SelectedOptionsText =>
            string.Join(", ", SelectedOptionsDict.Where(kvp => kvp.Value).Select(kvp => kvp.Key));


        // [ObservableProperty]
        // private ObservableCollection<OptionItem> availableOptions = new()
        // {
        //     new OptionItem { Label = "Option A", Value = "A" },
        //     new OptionItem { Label = "Option B", Value = "B" },
        //     new OptionItem { Label = "Option C", Value = "C" }
        // };

        // [ObservableProperty]
        // private ObservableCollection<OptionItem> selectedOptions = new();

        private DateTime? _startDate;
        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }


        private DateTime? _endDate;
        public DateTime? EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        [ObservableProperty]
        private bool includeArchived;

        private readonly MainWindowViewModel _mainWindowVm;

        public DashboardViewModel(ApiService apiService, MainWindowViewModel mainWindowVm)
        {
            _apiService = apiService;
            _mainWindowVm = mainWindowVm;
            PopulateStaff();
        }

        [RelayCommand]
        private void ApplyFilters()
        {
            Console.WriteLine($"Start Date: {StartDate}");
            Console.WriteLine($"End Date: {EndDate}");
            Console.WriteLine($"Average: {IncludeArchived}");


            // Example: print selected items
            var selected = AvailableOptions.Where(x => x.IsSelected);
            foreach (var item in selected)
            {
                Console.WriteLine($"Selected: {item.Label} - {item.Value}");
            }
        }

        [RelayCommand]
        private async void OrdersPerStaff()
        {
            try
            {
                var apiUrl = $"{_apiService.ApiAddress}/api/Analytics/OrdersPerStaff";

                var getStaffRequest = new
                {
                    Ids = AvailableOptions.Where(x => x.IsSelected).Select(x => x.Value).ToList(),
                    StartDate,
                    EndDate,
                    IsAverage = IncludeArchived,
                    Interval = 1
                };


                // new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                string jsonRequest = JsonSerializer.Serialize(getStaffRequest);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                // _apiService.AddAuthHeader();
                // HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
                HttpResponseMessage response = await _apiService._httpClient.PostAsync(apiUrl, content);
                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReferenceHandler = ReferenceHandler.Preserve
                    };
                    var staffResp = JsonSerializer.Deserialize<OrdersPerStaffDTO>(json, options);
                    
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        await UiTools.ShowMessageAsync("Error", "Session Expired. Please log in again to continue", UiTools.MessageType.Error);
                        // _mainWindowVm.Logout();
                        return;
                    }
                    var errMsg = await response.Content.ReadAsStringAsync();
                    await UiTools.ShowMessageAsync("Error", errMsg, UiTools.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                await UiTools.ShowMessageAsync("Error", ex.Message, UiTools.MessageType.Error);
            }
        }

        [RelayCommand]
        private async void PopulateStaff()
        {
            try
            {
                var apiUrl = $"{_apiService.ApiAddress}/api/Staff/Staff";

                var getStaffRequest = new
                {
                    GetAllStaff = true,
                };


                // new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                string jsonRequest = JsonSerializer.Serialize(getStaffRequest);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                // _apiService.AddAuthHeader();
                // HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
                HttpResponseMessage response = await _apiService._httpClient.PostAsync(apiUrl, content);
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
                    var staffResp = JsonSerializer.Deserialize<GetStaffDTO>(json, options);
                    foreach (var staff in staffResp?.Staff ?? new List<Staff>())
                    {
                        AvailableOptions.Add(new OptionItem { Label = staff.FirstName + " " + staff.LastName, Value = staff.Id.ToString() });
                    }
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        await UiTools.ShowMessageAsync("Error", "Session Expired. Please log in again to continue", UiTools.MessageType.Error);
                        // _mainWindowVm.Logout();
                        return;
                    }
                    var errMsg = await response.Content.ReadAsStringAsync();
                    await UiTools.ShowMessageAsync("Error", errMsg, UiTools.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                await UiTools.ShowMessageAsync($"Error", ex.ToString(), UiTools.MessageType.Error);
            }

        }
    }

    public class OptionItem : ObservableObject
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    public class TableItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
