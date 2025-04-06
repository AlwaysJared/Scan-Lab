using CommunityToolkit.Mvvm.ComponentModel;
using Libs.Data.Models;
using Admin.Services;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using System;
using static Admin.Tools.UiTools;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Converters;
using System.Text;
using Admin.ViewModels;
using Admin.Tools;

namespace Admin.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly HttpClient _httpClient = new();
    private readonly ApiService _apiService;
    
    public string ApiAddress
    {
        get => _apiService.ApiAddress;
        set => _apiService.ApiAddress = value; // ✅ Automatically saves when changed
    }

    
    public SettingsViewModel() : this(App.ApiService) { }

    public SettingsViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    #region API
    [RelayCommand]
    public async Task TestApiAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiAddress))
        {
            await UiTools.ShowMessageAsync("Error", "API Address is required.", MessageType.Error);
            return;
        }

        try
        {
            var response = await _httpClient.GetAsync($"{ApiAddress}/api/ping");
            if (response.IsSuccessStatusCode)
            {
                await UiTools.ShowMessageAsync("Success", "API is reachable.", UiTools.MessageType.Success);
            }
            else
            {
                await UiTools.ShowMessageAsync("Error", $"[API test failed]: {response.Content}", UiTools.MessageType.Error);
            }
        }
        catch (Exception ex)
        {
            await UiTools.ShowMessageAsync("Error", $"[Error]: {ex.Message}", UiTools.MessageType.Error);
        }
    }
    #endregion
}
