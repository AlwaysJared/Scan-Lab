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
using System.Windows.Input;
using Client.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using static Client.Tools.UiTools;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Collections.Concurrent;
using Libs.Services.SP500Export;

namespace Client.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly HttpClient _httpClient = new();
    private readonly ApiService _apiService;
    private readonly ScannerService _scannerService;
    private readonly AuthService _authService;
    private readonly MainWindowViewModel _mainWindowVm;

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private bool _completedOrdersChecked = false;
    public bool CompletedOrdersChecked
    {
        get => _completedOrdersChecked;
        set
        {
            if (_completedOrdersChecked != value)
            {
                _completedOrdersChecked = value;
                OnPropertyChanged(nameof(CompletedOrdersChecked));
                RestartSearchDelay();
            }
        }
    }

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

    // --- SP-500 Auto Export Status ---
    private readonly ConcurrentDictionary<Guid, SP500ExportStatusDto> _exportStatuses = new();
    private System.Timers.Timer? _exportPollTimer;

    [ObservableProperty]
    private ObservableCollection<SP500ExportStatusDto> activeExports = new();

    private bool _hasActiveExports;
    public bool HasActiveExports
    {
        get => _hasActiveExports;
        set => SetProperty(ref _hasActiveExports, value);
    }

    // public DashboardViewModel() : this(App.ApiService) { }
    public DashboardViewModel() { }

    public DashboardViewModel(ApiService apiService,
        ScannerService scannerService,
        AuthService authService,
        MainWindowViewModel mainWindowVm
    )
    {
        _apiService = apiService;
        _scannerService = scannerService;
        _authService = authService;
        _mainWindowVm = mainWindowVm;

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

        // Check for any active SP500 export sessions
        _ = CheckForActiveExports();
    }

    [RelayCommand]
    public async Task LoadOrdersAsync()
    {
        IsLoading = true;
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
                scannerId = _scannerSearchChecked ? (Guid?)_scannerService.SelectedScanner.Id : null,
                fetchCompletedOrders = _completedOrdersChecked
            };


            // new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            string jsonRequest = JsonSerializer.Serialize(getOrdersRequest);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            _apiService.AddAuthHeader();
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
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await UiTools.ShowMessageAsync("Error", "Session Expired. Please log in again to continue", UiTools.MessageType.Error);
                    _mainWindowVm.Logout();
                    return;
                }
                var errMsg = await response.Content.ReadAsStringAsync();
                await UiTools.ShowMessageAsync("Error", errMsg, UiTools.MessageType.Error);
            }
        }
        catch (Exception ex)
        {
            await UiTools.ShowMessageAsync("Error", $"[Error]: {ex.Message}", UiTools.MessageType.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DeleteOrder(Order order)
    {
        try
        {
            if (order is not null)
            {
                if (string.IsNullOrWhiteSpace(_apiService.ApiAddress))
                {
                    await UiTools.ShowMessageAsync("Error", $"[Error]: API Address is not configured.", UiTools.MessageType.Error);
                    return;
                }

                string apiUrl = $"{_apiService.ApiAddress}/api/Order/Delete";
                var content = new StringContent(JsonSerializer.Serialize(
                    new { OrderId = order.OrderId }),
                    System.Text.Encoding.UTF8, "application/json"
                );
                _apiService.AddAuthHeader();
                // var response = await _httpClient.PostAsync(apiUrl, content);
                var response = await _apiService._httpClient.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    LoadOrdersAsync();

                    await UiTools.ShowMessageAsync("Success",
                        $"Order #{order.OrderId} successfully deleted",
                        UiTools.MessageType.Success
                    );
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        await UiTools.ShowMessageAsync("Error", "Session Expired. Please log in again to continue", UiTools.MessageType.Error);
                        _mainWindowVm.Logout();
                        return;
                    }
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
                _apiService.AddAuthHeader();
                var response = await _apiService._httpClient.PutAsync(apiUrl, content);

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
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        await UiTools.ShowMessageAsync("Error", "Session Expired. Please log in again to continue", UiTools.MessageType.Error);
                        _mainWindowVm.Logout();
                        return;
                    }
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
                _apiService.AddAuthHeader();
                var response = await _apiService._httpClient.PutAsync(apiUrl, content);

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
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        await UiTools.ShowMessageAsync("Error", "Session Expired. Please log in again to continue", UiTools.MessageType.Error);
                        _mainWindowVm.Logout();
                        return;
                    }
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
                _apiService.AddAuthHeader();
                var response = await _apiService._httpClient.PostAsync(apiUrl, content);

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
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        await UiTools.ShowMessageAsync("Error", "Session Expired. Please log in again to continue", UiTools.MessageType.Error);
                        _mainWindowVm.Logout();
                        return;
                    }
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
                _apiService.AddAuthHeader();
                var response = await _apiService._httpClient.PutAsync(apiUrl, content);

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
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        await UiTools.ShowMessageAsync("Error", "Session Expired. Please log in again to continue", UiTools.MessageType.Error);
                        _mainWindowVm.Logout();
                        return;
                    }
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
                _apiService.AddAuthHeader();
                var response = await _apiService._httpClient.PostAsync(apiUrl, content);

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
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        await UiTools.ShowMessageAsync("Error", "Session Expired. Please log in again to continue", UiTools.MessageType.Error);
                        _mainWindowVm.Logout();
                        return;
                    }
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
    public async Task OpenOrderFolder(string orderPath)
    {
        try
        {
            string pathToOpen;

            if (!string.IsNullOrWhiteSpace(orderPath) && Directory.Exists(orderPath))
            {
                pathToOpen = orderPath;
            }
            else
            {
                await UiTools.ShowMessageAsync("Path Not Found",
                    string.IsNullOrWhiteSpace(orderPath) ? "Path not provided" : $"Path {orderPath} not found.",
                    MessageType.Info);
                // Fallback to a default known-good directory
                // Cross-platform default (Documents folder)
                pathToOpen = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = pathToOpen,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                // Optional: log or show error message
                Debug.WriteLine($"Failed to open folder: {ex.Message}");
            }

        }
        catch (Exception ex)
        {
            await UiTools.ShowMessageAsync("Error", ex.Message, MessageType.Error);
        }
    }


    private void RestartSearchDelay()
    {
        _searchDelayTimer.Stop();
        _searchDelayTimer.Start();
    }

    public ICommand ShowAddRollModalCommand => new RelayCommand<string>(ShowAddRollModal);

    private async void ShowAddRollModal(string? orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId)) return;

        var modal = new AddRollModal(orderId);
        var owner = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var addResp = await modal.ShowDialogWithResult(owner);

        if (addResp?.Success ?? false)
        {
            LoadOrdersAsync();

            await UiTools.ShowMessageAsync("Success",
                    $"Roll #{addResp.RollNumber} sucessfully added",
                    UiTools.MessageType.Success
            );
        }
    }

    // --- SP-500 Auto Export Commands ---

    [RelayCommand]
    public async Task StartExport(Roll? roll)
    {
        try
        {
            if (roll is null) return;

            if (string.IsNullOrWhiteSpace(_apiService.ApiAddress))
            {
                await UiTools.ShowMessageAsync("Error", "[Error]: API Address is not configured.", MessageType.Error);
                return;
            }

            string apiUrl = $"{_apiService.ApiAddress}/api/SP500Export/start/{roll.RollId}";
            _apiService.AddAuthHeader();
            var response = await _apiService._httpClient.PostAsync(apiUrl, null);

            if (response.IsSuccessStatusCode)
            {
                await UiTools.ShowMessageAsync("Success",
                    $"Auto-export started for Roll #{roll.RollNumber}",
                    MessageType.Success);

                StartExportPolling();
                await LoadOrdersAsync();
            }
            else
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await UiTools.ShowMessageAsync("Error", "Session Expired. Please log in again to continue", MessageType.Error);
                    _mainWindowVm.Logout();
                    return;
                }
                var errMsg = await response.Content.ReadAsStringAsync();
                await UiTools.ShowMessageAsync("Error", $"[Error]: {errMsg}", MessageType.Error);
            }
        }
        catch (Exception ex)
        {
            await UiTools.ShowMessageAsync("Error", $"[Error]: {ex.Message}", MessageType.Error);
        }
    }

    [RelayCommand]
    public async Task StopExport(Roll? roll)
    {
        try
        {
            if (roll is null) return;

            if (string.IsNullOrWhiteSpace(_apiService.ApiAddress))
            {
                await UiTools.ShowMessageAsync("Error", "[Error]: API Address is not configured.", MessageType.Error);
                return;
            }

            string apiUrl = $"{_apiService.ApiAddress}/api/SP500Export/stop/{roll.RollId}";
            _apiService.AddAuthHeader();
            var response = await _apiService._httpClient.PostAsync(apiUrl, null);

            if (response.IsSuccessStatusCode)
            {
                await UiTools.ShowMessageAsync("Success",
                    $"Auto-export stopped for Roll #{roll.RollNumber}",
                    MessageType.Success);

                _exportStatuses.TryRemove(roll.RollId, out _);
                await PollExportStatuses();
                await LoadOrdersAsync();
            }
            else
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await UiTools.ShowMessageAsync("Error", "Session Expired. Please log in again to continue", MessageType.Error);
                    _mainWindowVm.Logout();
                    return;
                }
                var errMsg = await response.Content.ReadAsStringAsync();
                await UiTools.ShowMessageAsync("Error", $"[Error]: {errMsg}", MessageType.Error);
            }
        }
        catch (Exception ex)
        {
            await UiTools.ShowMessageAsync("Error", $"[Error]: {ex.Message}", MessageType.Error);
        }
    }

    [RelayCommand]
    public async Task StopExportByRollId(Guid rollId)
    {
        // Used by the Active Exports panel Stop button
        var roll = Orders.SelectMany(o => o.Rolls ?? new List<Roll>())
            .FirstOrDefault(r => r.RollId == rollId);

        if (roll != null)
            await StopExport(roll);
    }

    // --- Export Status Polling ---

    private void StartExportPolling()
    {
        if (_exportPollTimer != null) return;

        _exportPollTimer = new System.Timers.Timer(5000) { AutoReset = true };
        _exportPollTimer.Elapsed += async (s, e) =>
        {
            await PollExportStatuses();
        };
        _exportPollTimer.Start();
    }

    private void StopExportPolling()
    {
        _exportPollTimer?.Stop();
        _exportPollTimer?.Dispose();
        _exportPollTimer = null;
    }

    private async Task PollExportStatuses()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_apiService?.ApiAddress)) return;

            string apiUrl = $"{_apiService.ApiAddress}/api/SP500Export/sessions";
            _apiService.AddAuthHeader();
            var response = await _apiService._httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var sessions = JsonSerializer.Deserialize<List<SP500ExportStatusDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _exportStatuses.Clear();
                if (sessions != null)
                {
                    foreach (var session in sessions)
                        _exportStatuses[session.RollId] = session;
                }

                Dispatcher.UIThread.Post(() =>
                {
                    ActiveExports.Clear();
                    foreach (var status in _exportStatuses.Values)
                        ActiveExports.Add(status);

                    HasActiveExports = ActiveExports.Count > 0;

                    if (!HasActiveExports)
                        StopExportPolling();
                });
            }
        }
        catch
        {
            // Silently handle polling errors to avoid flooding the UI
        }
    }

    public async Task CheckForActiveExports()
    {
        await PollExportStatuses();
        if (HasActiveExports)
            StartExportPolling();
    }
}
