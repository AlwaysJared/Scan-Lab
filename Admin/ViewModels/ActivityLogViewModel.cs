using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Admin.Services;
using Admin.Tools;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Libs.Data.Models;
using static Admin.Models.DTOs.Logs;
using static Admin.Tools.UiTools;

namespace Admin.ViewModels
{
    public partial class ActivityLogViewModel : ViewModelBase
    {
        private readonly HttpClient _httpClient = new();
        // private readonly ScannerService _scannerService;
        private readonly ApiService _apiService;

        [ObservableProperty]
        private ObservableCollection<LogEntry> logs = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PageInfo))]
        [NotifyCanExecuteChangedFor(nameof(FirstPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(PrevPageCommand))]
        private int currentPage = 1;

        [ObservableProperty]
        private int pageSize = 20;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PageInfo))]
        [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(LastPageCommand))]
        private int totalPages = 1;

        public bool CanGoPrev => CurrentPage > 1;
        public bool CanGoNext => CurrentPage < TotalPages;

        [RelayCommand(CanExecute = nameof(CanGoPrev))]
        private async Task FirstPageAsync()
        {
            if (CurrentPage != 1)
            {
                CurrentPage = 1;
                await LoadLogsAsync();
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoPrev))]
        private async Task PrevPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadLogsAsync();
            }

        }

        [RelayCommand(CanExecute = nameof(CanGoNext))]
        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadLogsAsync();
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoNext))]
        private async Task LastPageAsync()
        {
            if (CurrentPage != TotalPages)
            {
                CurrentPage = TotalPages;
                await LoadLogsAsync();
            }
        }

        public string PageInfo => $"Page {CurrentPage} of {TotalPages}";


        [ObservableProperty]
        private bool isLoading = true; // ✅ Re-added IsLoading
        public bool IsNotLoading => !IsLoading; // ✅ Re-added IsNotLoading

        [ObservableProperty]
        private ObservableCollection<int> pageNumbers = new();

        public string ApiAddress
        {
            get => _apiService.ApiAddress;
            set => _apiService.ApiAddress = value; // ✅ Automatically saves when changed
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private KeyValuePair<string, int?> _selectedArea; // ✅ Default to "All" (null)
        public KeyValuePair<string, int?> SelectedArea
        {
            get => _selectedArea;
            set
            {
                SetProperty(ref _selectedArea, value);
                RestartSearchDelay();
            }
        }
        public List<KeyValuePair<string, int?>> AreaOptions { get; } // ✅ Use int? for API


        private KeyValuePair<string, int?> _selectedLogLevel; // ✅ Default to "All" (null)
        public KeyValuePair<string, int?> SelectedLogLevel
        {
            get => _selectedLogLevel;
            set
            {
                SetProperty(ref _selectedLogLevel, value);
                RestartSearchDelay();
            }
        }
        public List<KeyValuePair<string, int?>> LogLevelOptions { get; } // ✅ Use int? for API


        private readonly System.Timers.Timer _searchDelayTimer = new(500) { AutoReset = false };
        private void RestartSearchDelay()
        {
            _searchDelayTimer.Stop();
            _searchDelayTimer.Start();
        }

        private bool _isInitializing = true;


        public ActivityLogViewModel() : this(App.ApiService) { }

        public ActivityLogViewModel(ApiService apiService)
        {
            _isInitializing = true;
            _apiService = apiService;

            CurrentPage = 1;

            // UpdatePageNumbers();

            // ✅ Populate dropdown with int? values
            LogLevelOptions = new List<KeyValuePair<string, int?>>
            {
                new KeyValuePair<string, int?>("All", null) // ✅ "All" option with null value
            };
            foreach (Libs.Enums.LogLevel status in Enum.GetValues(typeof(Libs.Enums.LogLevel)))
            {
                LogLevelOptions.Add(new KeyValuePair<string, int?>(status.ToString(), (int)status));
            }
            SelectedLogLevel = LogLevelOptions[0];

            // ✅ Populate dropdown with int? values
            AreaOptions = new List<KeyValuePair<string, int?>>
            {
                new KeyValuePair<string, int?>("All", null) // ✅ "All" option with null value
            };
            foreach (Libs.Enums.LogArea status in Enum.GetValues(typeof(Libs.Enums.LogArea)))
            {
                AreaOptions.Add(new KeyValuePair<string, int?>(status.ToString(), (int)status));
            }
            SelectedArea = AreaOptions[0];

            // LoadLogsAsync();
            _searchDelayTimer.Elapsed += (s, e) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    LoadLogsCommand.Execute(null);
                });
            };

            _isInitializing = false;
        }

        [RelayCommand]
        public async Task LoadLogsAsync()
        {
            try
            {
                Logs.Clear();
                TotalPages = 1;
                IsLoading = true;
                OnPropertyChanged(nameof(IsNotLoading));

                if (string.IsNullOrWhiteSpace(_apiService.ApiAddress))
                {
                    await UiTools.ShowMessageAsync("Error", $"[Error]: API Address is not configured.", UiTools.MessageType.Error);
                    return;
                }


                // ✅ Convert null to an empty string for API query
                var levelFilter = SelectedLogLevel.Value.HasValue ? ((Libs.Enums.LogLevel?)SelectedLogLevel.Value.Value) : null;
                var areaFilter = SelectedArea.Value.HasValue ? ((Libs.Enums.LogArea?)SelectedArea.Value.Value) : null;
                var apiUrl = $"{_apiService.ApiAddress}/api/Log/Logs";

                var getLogsRequest = new
                {
                    level = levelFilter.ToString(),
                    area = areaFilter.ToString(),
                    page = CurrentPage,
                    pageSize = pageSize
                };


                // new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                string jsonRequest = JsonSerializer.Serialize(getLogsRequest);
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
                    var logsDTO = JsonSerializer.Deserialize<GetLogsDTO>(json, options);

                    if (logsDTO.Logs.Any())
                    {
                        TotalPages = logsDTO.TotalPages;

                        foreach (var l in logsDTO.Logs)
                            Logs.Add(l);
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
                await UiTools.ShowMessageAsync("Error", $"Error fetching scanners: {ex.Message}", MessageType.Error);
            }
            finally
            {
                IsLoading = false;
                // UpdatePageNumbers();
                // Tell the buttons their CanExecute state might have changed
                FirstPageCommand.NotifyCanExecuteChanged();
                PrevPageCommand.NotifyCanExecuteChanged();
                NextPageCommand.NotifyCanExecuteChanged();
                LastPageCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(IsNotLoading));
            }
        }

        public async Task InitPageAsync()
        {
            await LoadLogsAsync();
        }
    }
}
