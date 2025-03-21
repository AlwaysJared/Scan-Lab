using CommunityToolkit.Mvvm.ComponentModel;
using Libs.Data.Models;
using Client.Services;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace Client.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly HttpClient _httpClient = new();
    private readonly ScannerService _scannerService;

    [ObservableProperty]
    private ObservableCollection<Scanner> scanners = new();

    [ObservableProperty]
    private bool isLoading = true; // ✅ Re-added IsLoading

    public bool IsNotLoading => !IsLoading; // ✅ Re-added IsNotLoading


    public Scanner? SelectedScanner
    {
        get => _scannerService.SelectedScanner;
        set
        {
            if (_scannerService.SelectedScanner != value)
            {
                _scannerService.SelectedScanner = value;
                OnPropertyChanged(nameof(SelectedScanner));
                SaveSelectedScanner(); // ✅ Save whenever scanner changes
            }
        }
    }

    public SettingsViewModel() : this(App.ScannerService) { }

    public SettingsViewModel(ScannerService scannerService)
    {
        _scannerService = scannerService;
        LoadScannersAsync();
    }

    private async void LoadScannersAsync()
    {
        try
        {
            IsLoading = true;
            OnPropertyChanged(nameof(IsNotLoading));

            string apiUrl = "http://localhost:5010/api/Scanner/Scanners"; // ✅ Replace with actual API URL
            var response = await _httpClient.GetStringAsync(apiUrl);
            var scannerList = JsonSerializer.Deserialize<List<Scanner>>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (scannerList != null)
            {
                Scanners = new ObservableCollection<Scanner>(scannerList);

                // ✅ Ensure selected scanner is valid
                if (_scannerService.SelectedScanner == null || !Scanners.Where(s => s.Id == _scannerService.SelectedScanner.Id).Any())
                {
                    _scannerService.SelectedScanner = Scanners.FirstOrDefault();
                    SaveSelectedScanner();
                }
                else
                {
                    SelectedScanner = Scanners.Where(s => s.Id == _scannerService.SelectedScanner.Id).FirstOrDefault();
                    OnPropertyChanged(nameof(SelectedScanner));
                    // SaveSelectedScanner();
                }

            }
        }
        catch (HttpRequestException ex)
        {
            System.Console.WriteLine($"Error fetching scanners: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsNotLoading));
        }
    }

    private void SaveSelectedScanner()
    {
        _scannerService.SaveSelectedScanner(); // ✅ Delegate saving to ScannerService
    }
}
