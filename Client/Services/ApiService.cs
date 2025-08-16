using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace Client.Services;

public class ApiService
{
    private readonly TokenService _tokenService;
    public readonly HttpClient _httpClient;

    private static readonly string ApiConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScanLab", "api_config.json");

    private string _apiAddress = string.Empty;
    public event Action<string>? ApiAddressChanged; // ✅ Notify when the API address updates

    public string ApiAddress
    {
        get => _apiAddress;
        set
        {
            if (_apiAddress != value)
            {
                _apiAddress = value;
                SaveApiAddress();
                ApiAddressChanged?.Invoke(_apiAddress); // ✅ Notify subscribers
            }
        }
    }

    public ApiService(TokenService tokenService)
    {
        _tokenService = tokenService;
        _httpClient = new HttpClient();
        LoadApiAddress(); // ✅ Load API address on startup
    }

    public void AddAuthHeader()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
        if (_tokenService.HasValidToken())
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _tokenService.JwtToken);
        }
    }

    private void LoadApiAddress()
    {
        if (File.Exists(ApiConfigFile))
        {
            try
            {
                string json = File.ReadAllText(ApiConfigFile);
                _apiAddress = JsonSerializer.Deserialize<string>(json) ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading API address: {ex.Message}");
            }
        }
    }

    private void SaveApiAddress()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ApiConfigFile)!); // ✅ Ensure directory exists
            string json = JsonSerializer.Serialize(_apiAddress, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ApiConfigFile, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving API address: {ex.Message}");
        }
    }
}
