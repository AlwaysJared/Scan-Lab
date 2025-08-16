using Libs.Data.Models;
using Libs.Data.RequestResponse.Auth;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Client.Services;

public class AuthService
{
    private readonly ApiService _apiService;
    private readonly HttpClient _httpClient = new();
    public AuthService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<string?> AuthenticateAsync(string username, string password)
    {
        var response = await _httpClient.PostAsJsonAsync(_apiService.ApiAddress + "/api/auth/login", new { username, password });
        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return result?.Token;
    }

}
