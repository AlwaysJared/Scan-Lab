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
        public ObservableCollection<TableItem> RollsPerStaffTableItems { get; } = new ObservableCollection<TableItem>();
        public ObservableCollection<TableItem> RollsPerScannerTableItems { get; } = new ObservableCollection<TableItem>();
        public ObservableCollection<TableItem> OrdersPerScannerTableItems { get; } = new ObservableCollection<TableItem>();

        [ObservableProperty]
        private bool isDropdownOpen;
        [ObservableProperty]
        private bool isRollsPerStaffDropdownOpen;
        [ObservableProperty]
        private bool isRollsPerScannerDropdownOpen;
        [ObservableProperty]
        private bool isOrdersPerScannerDropdownOpen;

        public ObservableCollection<OptionItem> AvailableOptions { get; } = new();
        public ObservableCollection<OptionItem> RollsPerStaffAvailableOptions { get; } = new();
        public ObservableCollection<OptionItem> RollsPerScannerAvailableOptions { get; } = new();
        public ObservableCollection<OptionItem> OrdersPerSCannerAvailableOptions { get; } = new();

        public Dictionary<string, bool> SelectedOptionsDict { get; } = new();

        // Convenience property for button content
        public string SelectedOptionsText =>
            string.Join(", ", SelectedOptionsDict.Where(kvp => kvp.Value).Select(kvp => kvp.Key));

        public Dictionary<string, bool> RollsPerStaffSelectedOptionsDict { get; } = new();

        // Convenience property for button content
        public string RollsPerStaffSelectedOptionsText =>
            string.Join(", ", RollsPerStaffSelectedOptionsDict.Where(kvp => kvp.Value).Select(kvp => kvp.Key));

        public Dictionary<string, bool> RollsPerScannerSelectedOptionsDict { get; } = new();

        // Convenience property for button content
        public string RollsPerScannerSelectedOptionsText =>
            string.Join(", ", RollsPerScannerSelectedOptionsDict.Where(kvp => kvp.Value).Select(kvp => kvp.Key));

        public Dictionary<string, bool> OrdersPerScannerSelectedOptionsDict { get; } = new();

        // Convenience property for button content
        public string OrdersPerScannerSelectedOptionsText =>
            string.Join(", ", OrdersPerScannerSelectedOptionsDict.Where(kvp => kvp.Value).Select(kvp => kvp.Key));


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


        private DateTime? _rollsPerStaffStartDate;
        public DateTime? RollsPerStaffStartDate
        {
            get => _rollsPerStaffStartDate;
            set => SetProperty(ref _rollsPerStaffStartDate, value);
        }


        private DateTime? _rollsPerStaffEndDate;
        public DateTime? RollsPerStaffEndDate
        {
            get => _rollsPerStaffEndDate;
            set => SetProperty(ref _rollsPerStaffEndDate, value);
        }

        private DateTime? _rollsPerScannerStartDate;
        public DateTime? RollsPerScannerStartDate
        {
            get => _rollsPerScannerStartDate;
            set => SetProperty(ref _rollsPerScannerStartDate, value);
        }


        private DateTime? _rollsPerScannerEndDate;
        public DateTime? RollsPerScannerEndDate
        {
            get => _rollsPerScannerEndDate;
            set => SetProperty(ref _rollsPerScannerEndDate, value);
        }

        private DateTime? _ordersPerScannerStartDate;
        public DateTime? OrdersPerScannerStartDate
        {
            get => _ordersPerScannerStartDate;
            set => SetProperty(ref _ordersPerScannerStartDate, value);
        }


        private DateTime? _ordersPerScannerEndDate;
        public DateTime? OrdersPerScannerEndDate
        {
            get => _ordersPerScannerEndDate;
            set => SetProperty(ref _ordersPerScannerEndDate, value);
        }



        [ObservableProperty]
        private bool includeArchived = true;

        [ObservableProperty]
        private bool rollsPerStaffIncludeArchived = true;

        [ObservableProperty]
        private bool rollsPerScannerIncludeArchived = true;

        [ObservableProperty]
        private bool ordersPerScannerIncludeArchived = true;

        private readonly MainWindowViewModel _mainWindowVm;

        public DashboardViewModel(ApiService apiService, MainWindowViewModel mainWindowVm)
        {
            _apiService = apiService;
            _mainWindowVm = mainWindowVm;
            PopulateStaff();

            OrdersPerStaff();
            RollsPerStaff();
        }

        [RelayCommand]
        private void ApplyFilters()
        {
            Console.WriteLine($"Start Date: {RollsPerStaffStartDate}");
            Console.WriteLine($"End Date: {RollsPerStaffEndDate}");
            Console.WriteLine($"Average: {RollsPerStaffIncludeArchived}");


            // Example: print selected items
            var selected = RollsPerStaffAvailableOptions.Where(x => x.IsSelected);
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
                TableItems.Clear();
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
                    foreach (var a in staffResp.Analytics)
                    {
                        TableItems.Add(new TableItem { Id = a.Id, Name = a.Name, Value = a.Value.ToString() });
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
                await UiTools.ShowMessageAsync("Error", ex.Message, UiTools.MessageType.Error);
            }
        }

        [RelayCommand]
        private async void RollsPerStaff()
        {
            try
            {
                RollsPerStaffTableItems.Clear();
                var apiUrl = $"{_apiService.ApiAddress}/api/Analytics/RollsPerStaff";

                var getStaffRequest = new
                {
                    Ids = RollsPerStaffAvailableOptions.Where(x => x.IsSelected).Select(x => x.Value).ToList(),
                    StartDate,
                    EndDate,
                    IsAverage = RollsPerStaffIncludeArchived,
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
                    foreach (var a in staffResp.Analytics)
                    {
                        RollsPerStaffTableItems.Add(new TableItem { Id = a.Id, Name = a.Name, Value = a.Value.ToString() });
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
                        RollsPerStaffAvailableOptions.Add(new OptionItem { Label = staff.FirstName + " " + staff.LastName, Value = staff.Id.ToString() });
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
