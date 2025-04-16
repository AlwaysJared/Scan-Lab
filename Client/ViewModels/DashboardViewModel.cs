using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Libs.Data.Models;
using Client.Services;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System;
using Client.Tools;
using Avalonia.Threading;
using System.Linq;
using Libs.Enums;
using System.Text;
using System.ComponentModel;

namespace Client.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly HttpClient _httpClient = new();
    private readonly ApiService _apiService;
    private readonly ScannerService _scannerService;

    private bool _scannerSearchChecked = true;

    public bool ScannerSearchChecked
    {
        get => _scannerSearchChecked;
        set
        {
            if (_scannerSearchChecked != value)
            {
                _scannerSearchChecked = value;
                OnPropertyChanged(nameof(ScannerSearchChecked));
                RestartSearchDelay();
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    [ObservableProperty]
    private ObservableCollection<Order> orders = new();

    // [ObservableProperty]
    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            SetProperty(ref _searchQuery, value);
            RestartSearchDelay();
        }
    }

    // [ObservableProperty]
    private KeyValuePair<string, int?> _selectedOrderStatus; // ✅ Default to "All" (null)
    public KeyValuePair<string, int?> SelectedOrderStatus
    {
        get => _selectedOrderStatus;
        set
        {
            SetProperty(ref _selectedOrderStatus, value);
            RestartSearchDelay();
        }
    }
    public List<KeyValuePair<string, int?>> OrderStatusOptions { get; } // ✅ Use int? for API

    private readonly System.Timers.Timer _searchDelayTimer = new(500) { AutoReset = false };

    // public DashboardViewModel() : this(App.ApiService) { }
    public DashboardViewModel() { }

    public DashboardViewModel(ApiService apiService, ScannerService scannerService)
    {
        _apiService = apiService;
        _scannerService = scannerService;

        // ✅ Populate dropdown with int? values
        OrderStatusOptions = new List<KeyValuePair<string, int?>>
        {
            new KeyValuePair<string, int?>("All", null) // ✅ "All" option with null value
        };
        foreach (OrderStatus status in Enum.GetValues(typeof(OrderStatus)))
        {
            OrderStatusOptions.Add(new KeyValuePair<string, int?>(status.ToString(), (int)status));
        }
        SelectedOrderStatus = OrderStatusOptions[0];

        // _searchDelayTimer.Elapsed += (s, e) => LoadOrdersCommand.Execute(null);
        _searchDelayTimer.Elapsed += (s, e) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                LoadOrdersCommand.Execute(null);
            });
        };

        // LoadOrdersAsync();
    }

    [RelayCommand]
    public async Task LoadOrdersAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_apiService.ApiAddress))
            {
                await UiTools.ShowMessageAsync("Error", $"[Error]: API Address is not configured.", UiTools.MessageType.Error);
                return;
            }


            // ✅ Convert null to an empty string for API query
            var statusFilter = SelectedOrderStatus.Value.HasValue ? ((OrderStatus?)SelectedOrderStatus.Value.Value) : null;
            var apiUrl = $"{_apiService.ApiAddress}/api/Order/orders";

            var getOrdersRequest = new
            {
                search = SearchQuery,
                orderStatus = statusFilter,
                scannerId = _scannerSearchChecked ? (Guid?)_scannerService.SelectedScanner.Id : null
            };


            // new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            string jsonRequest = JsonSerializer.Serialize(getOrdersRequest);
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
                var orderList = JsonSerializer.Deserialize<List<Order>>(json, options);

                if (orderList != null)
                {
                    orderList = orderList.OrderByDescending(o => o.DateCreated).ToList();
                    Orders.Clear();
                    foreach (var order in orderList)
                    {
                        // ✅ Ensure Rolls is never null
                        order.Rolls ??= new List<Roll>();
                        order.Rolls = order.Rolls.OrderBy(r => r.RollNumber).ToList();

                        // System.Console.WriteLine($"Adding Order: {order.OrderId} (Rolls: {order.Rolls.Count})");
                        Orders.Add(order); // ✅ UI Updates now!
                    }
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
            await UiTools.ShowMessageAsync("Error", $"[Error]: {ex.Message}", UiTools.MessageType.Error);
        }
    }

    [RelayCommand]
    public async Task StartResumeScanningRoll(Roll? roll)
    {
        try
        {
            if (roll is not null)
            {
                if (string.IsNullOrWhiteSpace(_apiService.ApiAddress))
                {
                    await UiTools.ShowMessageAsync("Error", $"[Error]: API Address is not configured.", UiTools.MessageType.Error);
                    return;
                }

                string apiUrl = $"{_apiService.ApiAddress}/api/Roll/UpdateStatus";
                var content = new StringContent(JsonSerializer.Serialize(
                    new { RollId = roll.RollId, Status = RollStatus.ScanningInProgress }),
                    System.Text.Encoding.UTF8, "application/json"
                );
                var response = await _httpClient.PutAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    await UiTools.ShowMessageAsync("Success",
                        $"Roll #{roll.RollNumber} marked as 'in progress'",
                        UiTools.MessageType.Success
                    );
                    await LoadOrdersAsync();
                }
                else
                {
                    var errMsg = await response.Content.ReadAsStringAsync();
                    await UiTools.ShowMessageAsync("Error",
                        $"[Error]: {errMsg}",
                        UiTools.MessageType.Error
                    );
                }
            }
        }
        catch (Exception ex)
        {
            await UiTools.ShowMessageAsync("Error", $"[Error]: {ex.Message}", UiTools.MessageType.Error);
        }


    }

    [RelayCommand]
    public async Task PauseScanningRoll(Roll? roll)
    {
        try
        {
            if (roll is not null)
            {
                if (string.IsNullOrWhiteSpace(_apiService.ApiAddress))
                {
                    await UiTools.ShowMessageAsync("Error", $"[Error]: API Address is not configured.", UiTools.MessageType.Error);
                    return;
                }

                string apiUrl = $"{_apiService.ApiAddress}/api/Roll/UpdateStatus";
                var content = new StringContent(JsonSerializer.Serialize(
                    new { RollId = roll.RollId, Status = RollStatus.ScanningPaused }),
                    System.Text.Encoding.UTF8, "application/json"
                );
                var response = await _httpClient.PutAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    await UiTools.ShowMessageAsync("Success",
                        $"Roll #{roll.RollNumber} marked as 'scanning paused'",
                        UiTools.MessageType.Success
                    );
                    await LoadOrdersAsync();
                }
                else
                {
                    var errMsg = await response.Content.ReadAsStringAsync();
                    await UiTools.ShowMessageAsync("Error",
                        $"[Error]: {errMsg}",
                        UiTools.MessageType.Error
                    );
                }
            }
        }
        catch (Exception ex)
        {
            await UiTools.ShowMessageAsync("Error", $"[Error]: {ex.Message}", UiTools.MessageType.Error);
        }
    }

    [RelayCommand]
    public async Task CompleteRoll(Roll? roll)
    {
        try
        {
            if (roll is not null)
            {
                if (string.IsNullOrWhiteSpace(_apiService.ApiAddress))
                {
                    await UiTools.ShowMessageAsync("Error", $"[Error]: API Address is not configured.", UiTools.MessageType.Error);
                    return;
                }

                string apiUrl = $"{_apiService.ApiAddress}/api/Roll/complete";
                var content = new StringContent(JsonSerializer.Serialize(
                    new { RollId = roll.RollId }),
                    System.Text.Encoding.UTF8, "application/json"
                );
                var response = await _httpClient.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    await UiTools.ShowMessageAsync("Success",
                        $"Roll #{roll.RollNumber} sucessfully processed",
                        UiTools.MessageType.Success
                    );
                    await LoadOrdersAsync();
                }
                else
                {
                    var errMsg = await response.Content.ReadAsStringAsync();
                    await UiTools.ShowMessageAsync("Error",
                        $"[Error]: {errMsg}",
                        UiTools.MessageType.Error
                    );
                }
            }
        }
        catch (Exception ex)
        {
            await UiTools.ShowMessageAsync("Error", $"[Error]: {ex.Message}", UiTools.MessageType.Error);
        }
    }

    [RelayCommand]
    public async Task ResetRoll(Roll? roll)
    {
        try
        {
            if (roll is not null)
            {
                if (string.IsNullOrWhiteSpace(_apiService.ApiAddress))
                {
                    await UiTools.ShowMessageAsync("Error", $"[Error]: API Address is not configured.", UiTools.MessageType.Error);
                    return;
                }

                string apiUrl = $"{_apiService.ApiAddress}/api/Roll/UpdateStatus";
                var content = new StringContent(JsonSerializer.Serialize(
                    new { RollId = roll.RollId, Status = RollStatus.Created }),
                    System.Text.Encoding.UTF8, "application/json"
                );
                var response = await _httpClient.PutAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    await UiTools.ShowMessageAsync("Success",
                        $"Roll #{roll.RollNumber} status sucessfully reset",
                        UiTools.MessageType.Success
                    );
                    await LoadOrdersAsync();
                }
                else
                {
                    var errMsg = await response.Content.ReadAsStringAsync();
                    await UiTools.ShowMessageAsync("Error",
                        $"[Error]: {errMsg}",
                        UiTools.MessageType.Error
                    );
                }
            }
        }
        catch (Exception ex)
        {
            await UiTools.ShowMessageAsync("Error", $"[Error]: {ex.Message}", UiTools.MessageType.Error);
        }
    }

    [RelayCommand]
    public async Task DeleteRoll(Roll? roll)
    {
        try
        {
            if (roll is not null)
            {
                if (string.IsNullOrWhiteSpace(_apiService.ApiAddress))
                {
                    await UiTools.ShowMessageAsync("Error", $"[Error]: API Address is not configured.", UiTools.MessageType.Error);
                    return;
                }

                string apiUrl = $"{_apiService.ApiAddress}/api/Roll/delete";
                var content = new StringContent(JsonSerializer.Serialize(
                    new { RollId = roll.RollId }),
                    System.Text.Encoding.UTF8, "application/json"
                );
                var response = await _httpClient.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    await UiTools.ShowMessageAsync("Success",
                        $"Roll #{roll.RollNumber} sucessfully deleted",
                        UiTools.MessageType.Success
                    );
                    await LoadOrdersAsync();
                }
                else
                {
                    var errMsg = await response.Content.ReadAsStringAsync();
                    await UiTools.ShowMessageAsync("Error",
                        $"[Error]: {errMsg}",
                        UiTools.MessageType.Error
                    );
                }
            }
        }
        catch (Exception ex)
        {
            await UiTools.ShowMessageAsync("Error", $"[Error]: {ex.Message}", UiTools.MessageType.Error);
        }
    }

    private void RestartSearchDelay()
    {
        _searchDelayTimer.Stop();
        _searchDelayTimer.Start();
    }

    private bool IsRollButtonVisible(RollStatus rollStatus, string action){
        try{
            switch (action.ToLower()){
                case "start":
                    switch(rollStatus){
                        case RollStatus.Created:
                        case RollStatus.ScanningPaused:
                            return true;
                        default:
                            return false;
                    }
                case "pause":
                    switch (rollStatus){
                        case RollStatus.ScanningInProgress:
                            return true;
                        default:
                            return false;
                    }
                case "complete":
                    switch (rollStatus){
                        case RollStatus.ScanningInProgress:
                        case RollStatus.ScanningPaused:
                            return true;
                        default:
                            return false;
                    }
                case "delete":
                    switch (rollStatus){
                        case RollStatus.Processing:
                        case RollStatus.Processed:
                        case RollStatus.ScanningCompleted:
                            return false;
                        default:
                            return true;
                    }
                default:
                    return false;
            }
        }
        catch
        {
            return true;
        }
    }
}
