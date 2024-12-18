// Services/ScannerService.cs
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Libs.Data.Models;

public class ScannerService
{
    private readonly HttpClient _httpClient;
    public Scanner SelectedScanner { get; set; } = new Scanner{
        Id = Guid.NewGuid(),
        ScannerName = "",
        WatchedDir = "",
        DestinationDir = "",
        ArchiveDir = "",
    };

    private const string ScannerIDKey = "ScannerID";
    private const string ScannerNameKey = "ScannerName";
    
    public ScannerService()
    {
        _httpClient = new HttpClient();
        LoadScanner();
    }

    public async Task<List<Scanner>> GetScannersAsync()
    {
        var response = await _httpClient.GetStringAsync("http://localhost:5010/api/scanner/scanners");
        return JsonSerializer.Deserialize<List<Scanner>>(response, new JsonSerializerOptions{PropertyNameCaseInsensitive = true});
    }

    public void SaveScannerProfile(Scanner scnr){
        Preferences.Set(ScannerIDKey, scnr.Id.ToString());
        Preferences.Set(ScannerNameKey, scnr.ScannerName);
    }

    private void LoadScanner()
    {
        // Load the scanner details from Preferences
        var savedScannerID = Preferences.Get(ScannerIDKey, string.Empty);
        var savedScannerName = Preferences.Get(ScannerNameKey, string.Empty);

        if (!string.IsNullOrEmpty(savedScannerID) && !string.IsNullOrEmpty(savedScannerName))
        {
            SelectedScanner.Id = Guid.Parse(savedScannerID);
            SelectedScanner.ScannerName = savedScannerName;
        }
    }
}
