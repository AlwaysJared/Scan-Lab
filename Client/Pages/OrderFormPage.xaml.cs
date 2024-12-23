using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Maui.Controls;
using Libs.Data.Models;

namespace Client.Pages
{
    public partial class OrderFormPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private readonly ScannerService _scannerService;

        // ObservableCollection will update UI automatically when items are added/removed
        public ObservableCollection<Roll> OrderRolls { get; set; }

        public bool IsTableVisible { get; set; }

        public OrderFormPage(ScannerService scannerService)
        {
            InitializeComponent();
            OrderRolls = new ObservableCollection<Roll>();
            _httpClient = new HttpClient();  // Initialize HttpClient
            _scannerService = scannerService;
            BindingContext = this;
        }

        // Clear Button Action
        private void OnClearClicked(object sender, EventArgs e)
        {
            // Clear form fields
            OrderIdEntry.Text = string.Empty;
            FirstRollNumberEntry.Text = string.Empty;
            RollCountEntry.Text = string.Empty;

            // Clear the table
            OrderRolls.Clear();
            IsTableVisible = false;
        }

        // Submit Button Action - this method will now post data to an API
        private async void OnSubmitClicked(object sender, EventArgs e)
        {
            // Gather form data
            var orderId = OrderIdEntry.Text;
            var firstRollNumber = FirstRollNumberEntry.Text;
            var rollCount = RollCountEntry.Text;

            // Basic validation (ensure all fields are filled)
            if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(firstRollNumber) || string.IsNullOrEmpty(rollCount))
            {
                await DisplayAlert("Error", "Please fill out all fields", "OK");
                return;
            }

            // Prepare data object
            var orderData = new
            {
                OrderID = orderId,
                // FirstRollNumber = firstRollNumber,
                // RollCount = rollCount
                Rolls = OrderRolls,
                Scanner = _scannerService.SelectedScanner
            };

            // Serialize data to JSON
            var jsonContent = JsonConvert.SerializeObject(orderData);

            // Prepare HttpContent for the POST request
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                // Send POST request to the API
                var response = await _httpClient.PostAsync("http://localhost:5010/api/order/submit", content);

                // Handle response
                if (response.IsSuccessStatusCode)
                {
                    // Success - you can process the response here if needed
                    await DisplayAlert("Success", "Order submitted successfully!", "OK");

                    // Optionally, clear the form after submission
                    OnClearClicked(sender, e);
                }
                else
                {
                    // API call failed
                    await DisplayAlert("Error", "Failed to submit order. Please try again.", "OK");
                }
            }
            catch (Exception ex)
            {
                // Handle any exception that occurred during the API call
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }

            // Clear form after submission (optional)
            // OnClearClicked(sender, e);
        }

        // Method to update the roll numbers table based on the fields
        private void UpdateTable()
        {
            // Ensure the fields are not empty and are valid
            if (int.TryParse(FirstRollNumberEntry.Text, out int firstRollNumber) && int.TryParse(RollCountEntry.Text, out int rollCount))
            {
                OrderRolls.Clear();
                IsTableVisible = true;

                // Add the roll numbers to the table (firstRollNumber + increment for each row)
                for (int i = 0; i < rollCount; i++)
                {
                    OrderRolls.Add(new Roll
                    {
                        RollNumber = firstRollNumber + i
                    });
                }
            }
            else
            {
                OrderRolls.Clear();
                IsTableVisible = false;
            }
        }

        // Event handlers for text change events on the entries
        private void FirstRollNumberEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTable();
        }

        private void RollCountEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTable();
        }

        // Roll class for holding roll number data
        // public class Roll
        // {
        //     public int RollNumber { get; set; }
        // }
    }
}
