using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using Libs.Data.Models;
using Libs.Enums;
using Microsoft.Maui.Controls;

namespace Client.Pages
{
    public partial class DashboardPage : ContentPage
    {
        private string _searchQuery = string.Empty;
        private CancellationTokenSource _searchDelayToken; // ✅ Used to cancel previous API calls

        private KeyValuePair<string, int?> _selectedOrderStatus; // ✅ Default to "All" (null)
        public List<KeyValuePair<string, int?>> OrderStatusOptions { get; } // ✅ Use int? for API


        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                DelayedSearchAsync(); // ✅ Calls the new delayed search method
            }
        }

        public KeyValuePair<string, int?> SelectedOrderStatus
        {
            get => _selectedOrderStatus;
            set
            {
                _selectedOrderStatus = value;
                FetchOrdersAsync(); // ✅ Call API with updated status filter
            }
        }

        // Observable collection to bind to the CollectionView
        public List<Order> Orders { get; set; }

        //Roll actions commands
        public ICommand StartResumeScanningCommand { get; }
        public ICommand PauseScanningCommand { get; }
        public ICommand DeleteRollCommand { get; }
        public ICommand CompleteScanningCommand { get; private set; }

        public DashboardPage()
        {
            InitializeComponent();
            Orders = new List<Order>();

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

            StartResumeScanningCommand = new Command<Roll>(async (roll) => await StartResumeScanning(roll));
            PauseScanningCommand = new Command<Roll>(async (roll) => await PauseScanning(roll));
            DeleteRollCommand = new Command<Roll>(async (roll) => await DeleteRoll(roll));
            CompleteScanningCommand = new Command<Roll>(async (roll) => await CompleteScanning(roll));
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
                    // ✅ Convert null to an empty string for API query
                    var statusFilter = SelectedOrderStatus.Value.HasValue ? ((int)SelectedOrderStatus.Value.Value).ToString() : "";
                    var url = $"{apiUrl}/Order/orders?search={SearchQuery}&status={statusFilter}";
                    var response = await client.GetStringAsync(url);

                    // Deserialize the response into a list of orders
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReferenceHandler = ReferenceHandler.Preserve
                    };
                    Orders = JsonSerializer.Deserialize<List<Order>>(response, options);

                    // ✅ Sort rolls in each order by RollNumber (ascending)
                    foreach (var order in Orders)
                    {
                        order.Rolls = order.Rolls.OrderBy(roll => roll.RollNumber).ToList();
                    }


                    // Refresh the UI by binding the updated list
                    OrdersCollectionView.ItemsSource = Orders.OrderByDescending(o => o.DateCreated);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to fetch orders: {ex.Message}", "OK");
                }
            }
        }
        private async void DelayedSearchAsync()
        {
            _searchDelayToken?.Cancel(); // ✅ Cancel any previous pending search
            _searchDelayToken = new CancellationTokenSource();

            try
            {
                await Task.Delay(500, _searchDelayToken.Token); // ✅ Wait 500ms before triggering API call

                // ✅ If not canceled, fetch data
                if (!_searchDelayToken.Token.IsCancellationRequested)
                {
                    await FetchOrdersAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // ✅ Ignore if the delay was canceled (user kept typing)
            }
        }
        private async void OnRollStatusChanged(object sender, EventArgs e)
        {
            // var picker = sender as Picker;
            // if (picker?.BindingContext is Roll selectedRoll)
            // {
            //     if (Enum.TryParse(typeof(RollStatus), picker.SelectedItem.ToString(), out var newStatus))
            //     {
            //         selectedRoll.Status = (RollStatus)newStatus;
            //         await UpdateRollStatus(selectedRoll.RollId, selectedRoll.Status);
            //     }
            // }
            await FetchOrdersAsync();
        }
        private async Task<bool> CompleteRoll(Guid rollId)
        {
            var apiUrl = "http://localhost:5010/api";

            using (var client = new HttpClient())
            {
                try
                {
                    var url = $"{apiUrl}/Roll/Complete";
                    var content = new StringContent(JsonSerializer.Serialize(new { RollId = rollId }),
                                                    System.Text.Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else
                    {
                        // API call failed
                        //get message string
                        var msg = await response.Content.ReadAsStringAsync();
                        await DisplayAlert("Error",
                        $"Failed to proces roll\n[ERROR]:\n{msg}",
                        "OK");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Update failed: {ex.Message}", "OK");
                    return false;
                }
            }
        }
        private async Task<bool> UpdateRollStatus(Guid rollId, RollStatus newStatus)
        {
            var apiUrl = "http://localhost:5010/api";

            using (var client = new HttpClient())
            {
                try
                {
                    var url = $"{apiUrl}/Roll/updateStatus";
                    var content = new StringContent(JsonSerializer.Serialize(new { RollId = rollId, Status = newStatus }),
                                                    System.Text.Encoding.UTF8, "application/json");

                    var response = await client.PutAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else
                    {
                        // API call failed
                        //get message string
                        var msg = await response.Content.ReadAsStringAsync();
                        await DisplayAlert("Error",
                        $"Failed to update roll status\n[ERROR]:\n{msg}",
                        "OK");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Update failed: {ex.Message}", "OK");
                    return false;
                }
            }
        }

        #region Roll Action Button Event Handlers
        private async Task StartResumeScanning(Roll roll)
        {
            if (roll == null) return;

            var newStatus = RollStatus.ScanningInProgress;
            var success = await UpdateRollStatus(roll.RollId, newStatus);

            if (success)
            {
                roll.Status = newStatus; // ✅ Update status only if API call succeeds

                // ✅ Find the parent order and update its status
                var parentOrder = Orders.FirstOrDefault(order => order.Rolls.Contains(roll));
                if (parentOrder != null)
                {
                    parentOrder.Status = OrderStatus.Processing; // ✅ Set parent order's status
                }

                RefreshUI();
            }
        }

        private async Task PauseScanning(Roll roll)
        {
            Console.WriteLine($"✅ in PauseScanning");

            if (roll == null) return;
            Console.WriteLine($"✅ Start/Resume button clicked for Roll ID: {roll.RollId}");


            var newStatus = RollStatus.ScanningPaused;
            var success = await UpdateRollStatus(roll.RollId, newStatus);

            if (success)
            {
                roll.Status = newStatus; // ✅ Update status only if API call succeeds

                // ✅ Find the parent order and update its status
                var parentOrder = Orders.FirstOrDefault(order => order.Rolls.Contains(roll));
                if (parentOrder != null)
                {
                    parentOrder.Status = OrderStatus.Created; // ✅ Set parent order's status
                }

                RefreshUI();
            }
        }

        private async Task DeleteRoll(Roll roll)
        {
            if (roll == null) return;

            var apiUrl = "http://localhost:5010/api";
            using (var client = new HttpClient())
            {
                try
                {
                    var url = $"{apiUrl}/Roll/{roll.RollId}/delete";
                    var response = await client.DeleteAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        await DisplayAlert("Success", "Roll deleted successfully.", "OK");

                        // ✅ Remove roll from the list and refresh UI
                        foreach (var order in Orders)
                        {
                            order.Rolls.Remove(roll);
                        }

                        RefreshUI();
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to delete roll.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Deletion failed: {ex.Message}", "OK");
                }
            }
        }

        private async Task CompleteScanning(Roll roll)
        {
            if (roll == null) return;
            Console.WriteLine($"Complete Scanning button clicked for Roll ID: {roll.RollId}");

            var newStatus = RollStatus.ScanningCompleted;
            var success = await CompleteRoll(roll.RollId);

            if (success)
            {
                roll.Status = newStatus; // ✅ Update roll status

                // ✅ Find parent order and update status if needed
                var parentOrder = Orders.FirstOrDefault(order => order.Rolls.Contains(roll));
                if (parentOrder != null)
                {
                    parentOrder.Status = OrderStatus.Created; // ✅ Update parent order
                }

                RefreshUI();
            }
        }
        #endregion

        // Event handler when search text changes
        // private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        // {
        //     _searchQuery = e.NewTextValue;
        // }

        // Load orders on page load
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await FetchOrdersAsync(); // Fetch the orders when the page appears
            RefreshUI();
        }

        // Force UI refresh
        private async Task RefreshUI()
        {
            // OrdersCollectionView.ItemsSource = null;
            // OrdersCollectionView.ItemsSource = Orders;

            await FetchOrdersAsync();
        }
    }
}