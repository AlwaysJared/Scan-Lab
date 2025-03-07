using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Libs.Data.Models;
using Libs.Enums;
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
                        PropertyNameCaseInsensitive = true,
                        ReferenceHandler = ReferenceHandler.Preserve
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

        private async void OnRollStatusChanged(object sender, EventArgs e)
        {
            var picker = sender as Picker;
            if (picker?.BindingContext is Roll selectedRoll)
            {
                if (Enum.TryParse(typeof(RollStatus), picker.SelectedItem.ToString(), out var newStatus))
                {
                    selectedRoll.Status = (RollStatus)newStatus;
                    await UpdateRollStatus(selectedRoll.RollId, selectedRoll.Status);
                }
            }
        }

        private async Task UpdateRollStatus(Guid rollId, RollStatus newStatus)
        {
            var apiUrl = "http://localhost:5010/api";

            using (var client = new HttpClient())
            {
                try
                {
                    var url = $"{apiUrl}/Roll/{rollId}/updateStatus";
                    var content = new StringContent(JsonSerializer.Serialize(new { Status = newStatus.ToString() }),
                                                    System.Text.Encoding.UTF8, "application/json");

                    var response = await client.PutAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        await DisplayAlert("Success", "Roll status updated.", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to update roll status.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Update failed: {ex.Message}", "OK");
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