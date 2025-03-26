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
        Orders = new ObservableCollection<Order>();
        LoadOrdersAsync();
    }

    [RelayCommand]
    private async Task LoadOrdersAsync()
    {
        if (string.IsNullOrWhiteSpace(_apiService.ApiAddress))
        {
            System.Console.WriteLine("API Address is not configured.");
            return;
        }

        try
        {
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
                    System.Console.WriteLine($"Loaded Order: ID={order.OrderId}, Customer={order.Customer?.FirstName}");
                    Orders.Add(order); // ✅ Properly updates UI
                }
            }
        }
        catch (HttpRequestException ex)
        {
            System.Console.WriteLine($"Error fetching orders: {ex.Message}");
        }
    }
}
