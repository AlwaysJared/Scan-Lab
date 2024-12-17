// Services/ScannerService.cs
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Libs.Data.Models;

public class ScannerService
{
    private readonly HttpClient _httpClient;
    public Scanner SelectedScanner { get; set; }
    
    public ScannerService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<List<Scanner>> GetScannersAsync()
    {
        var response = await _httpClient.GetStringAsync("http://localhost:5010/api/scanner/scanners");
        return JsonSerializer.Deserialize<List<Scanner>>(response, new JsonSerializerOptions{PropertyNameCaseInsensitive = true});
    }
}
