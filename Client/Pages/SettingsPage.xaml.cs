using System.Text.Json;
using System.Windows.Input;
using Client.ViewModels;
using Libs.Data.Models;
using Microsoft.Maui.Controls;

namespace Client.Pages
{
    public partial class SettingsPage : ContentPage
    {
        private readonly ScannerService _scannerService;
        private readonly HttpClient _httpClient;
        private SettingsViewModel _viewModel;

        public SettingsPage(ScannerService scannerService)
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            _scannerService = scannerService;
            _viewModel = new SettingsViewModel();
            _viewModel.SelectedScanner = _scannerService.SelectedScanner;
            BindingContext = _viewModel;
            LoadScanners();
        }

        // Load Scanners when the page is loaded
        private async void LoadScanners()
        {
            // Call the ScannerService to get the list of scanners
            var scanners = await _scannerService.GetScannersAsync();

            // var response = await _httpClient.GetStringAsync("http://localhost:5010/api/scanner/scanners");
            // var scanners = JsonSerializer.Deserialize<List<Scanner>>(
            //         response, 
            //         new JsonSerializerOptions
            //         {
            //             PropertyNameCaseInsensitive = true
            //         }
            //     );
            
            // Bind the scanners to the Picker
            ScannerPicker.ItemsSource = scanners;
            ScannerPicker.ItemDisplayBinding = new Binding("ScannerName");

            // Set the default selected item based on the current selected scanner
            if(_scannerService.SelectedScanner != null){
                var selectedScanner = scanners.FirstOrDefault(s => s.Id == _scannerService.SelectedScanner.Id);
                if (selectedScanner != null)
                {
                    ScannerPicker.SelectedItem = selectedScanner;
                    _viewModel.SelectedScanner = selectedScanner;
                }
            }
        }

        private void OnScannerSelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedScanner = ScannerPicker.SelectedItem as Scanner;
            if (selectedScanner != null)
            {
                // Update the global selected scanner
                _scannerService.SelectedScanner = selectedScanner;
                _viewModel.SelectedScanner = selectedScanner;
                _scannerService.SaveScannerProfile(selectedScanner);
            }
        }

        // Edit Scanner Configuration section
        private void OnEditScannerConfiguration(object sender, EventArgs e)
        {
            _viewModel.IsScannerEditable = true;
        }

        // Edit API section
        private void OnEditAPI(object sender, EventArgs e)
        {
            _viewModel.IsAPIEditable = true;
        }
    }
}
