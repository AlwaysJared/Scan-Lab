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

namespace Client.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly HttpClient _httpClient = new();
    private readonly ApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<Order> orders = new();

    public DashboardViewModel() : this(App.ApiService) { }

    public DashboardViewModel(ApiService apiService)
    {
        _apiService = apiService;
        LoadOrdersAsync();
    }

    [RelayCommand]
    private async Task LoadOrdersAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_apiService.ApiAddress))
            {
                await UiTools.ShowMessageAsync("Error", $"[Error]: API Address is not configured.", UiTools.MessageType.Error);
                return;
            }

            string apiUrl = $"{_apiService.ApiAddress}/api/Order/Orders"; // ✅ Fetch orders from API
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
    public void StartResumeScanningRoll(Roll? roll)
    {
        if(roll is not null){
            Console.WriteLine($"Scanning Roll: {roll.RollNumber}");
        }
        
    }

    [RelayCommand]
    public void PauseScanningRoll(Roll? roll)
    {
        if (roll is not null)
        {
            Console.WriteLine($"Pausing Roll: {roll.RollNumber}");
        }
    }

    [RelayCommand]
    public void CompleteRoll(Roll? roll)
    {
        if (roll is not null)
        {
            Console.WriteLine($"Completing Roll: {roll.RollNumber}");
        }
    }

    [RelayCommand]
    public void DeleteRoll(Roll? roll)
    {
        if (roll is not null)
        {
            Console.WriteLine($"Deleting Roll: {roll.RollNumber}");
        }
    }


}
