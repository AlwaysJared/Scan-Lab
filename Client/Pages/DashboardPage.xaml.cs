using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Libs.Data.Models;
using Microsoft.Maui.Controls;

namespace Client.Pages
{
    public partial class DashboardPage : ContentPage
    {
        private string _searchQuery = string.Empty;
        private string _statusFilter = "All";

        // Observable collection to bind to the CollectionView
        public List<Order> Orders { get; set; }

        public DashboardPage()
        {
            InitializeComponent();
            Orders = new List<Order>();
            BindingContext = this;
        }

        // Method to fetch orders from the API
        private async Task FetchOrdersAsync()
        {
            var apiUrl = "http://localhost:5010/api"; // Replace with your API URL

            using (var client = new HttpClient())
            {
                try
                {
                    // Build the request URL with search and status filters
                    var url = $"{apiUrl}/Order/orders?search={_searchQuery}&status={0}";
                    var response = await client.GetStringAsync(url);
                    
                    // Deserialize the response into a list of orders
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    Orders = JsonSerializer.Deserialize<List<Order>>(response,options);
                    
                    // Refresh the UI by binding the updated list
                    OrdersCollectionView.ItemsSource = Orders;
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to fetch orders: {ex.Message}", "OK");
                }
            }
        }

        // Event handler when search text changes
        private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchQuery = e.NewTextValue;
            await FetchOrdersAsync(); // Fetch orders with the new search query
        }

        // Load orders on page load
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await FetchOrdersAsync(); // Fetch the orders when the page appears
        }
    }
}
