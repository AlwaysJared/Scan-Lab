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

namespace Client.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly HttpClient _httpClient = new();
    private readonly ApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<Order> orders = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private KeyValuePair<string, int?> _selectedOrderStatus; // ✅ Default to "All" (null)
    public List<KeyValuePair<string, int?>> OrderStatusOptions { get; } // ✅ Use int? for API

    // public DashboardViewModel() : this(App.ApiService) { }
    public DashboardViewModel() { }

    public DashboardViewModel(ApiService apiService)
    {
        _apiService = apiService;

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

        LoadOrdersAsync();
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
            var statusFilter = SelectedOrderStatus.Value.HasValue ? ((int)SelectedOrderStatus.Value.Value).ToString() : "";
            var apiUrl = $"{_apiService.ApiAddress}/api/Order/orders?search={SearchQuery}&status={statusFilter}";

            // string apiUrl = $"{_apiService.ApiAddress}/api/Order/Orders"; // ✅ Fetch orders from API
            var response = await _httpClient.GetStringAsync(apiUrl);
            // Deserialize the response into a list of orders
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.Preserve
            };
            var orderList = JsonSerializer.Deserialize<List<Order>>(response, options);

            if (orderList != null)
            {
                Orders.Clear();
                foreach (var order in orderList)
                {
                    // ✅ Ensure Rolls is never null
                    order.Rolls ??= new List<Roll>();

                    System.Console.WriteLine($"Adding Order: {order.OrderId} (Rolls: {order.Rolls.Count})");
                    Orders.Add(order); // ✅ UI Updates now!
                }
            }
        }
        catch (Exception ex)
        {
            await UiTools.ShowMessageAsync("Error", $"[Error]: {ex.Message}", UiTools.MessageType.Error);
        }
    }

    [RelayCommand]
    public async void StartResumeScanningRoll(Roll? roll)
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
                    await UiTools.ShowMessageAsync("Error",
                        $"[Error]: {response.Content}",
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
    public async void PauseScanningRoll(Roll? roll)
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
                    await UiTools.ShowMessageAsync("Error",
                        $"[Error]: {response.Content}",
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
    public async void CompleteRoll(Roll? roll)
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
                    await UiTools.ShowMessageAsync("Error",
                        $"[Error]: {response.Content}",
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
    public async void DeleteRoll(Roll? roll)
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
                    await UiTools.ShowMessageAsync("Error",
                        $"[Error]: {response.Content}",
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
}
