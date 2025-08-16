using System;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Input;
using Avalonia.Controls;
using Client.Services;
using Client.Tools;
using Client.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Libs.Data.Models;

namespace Client.ViewModels;

public partial class AddRollModalViewModel : ViewModelBase
{
    private readonly HttpClient _httpClient = new();
    public string OrderId { get; }

    [ObservableProperty]
    private string? rollNumber = "";

    private readonly Window _window;
    private readonly ApiService _apiService;

    public event Action<string>? RequestCloseWithSuccess;
    public event Action? RequestClose;
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand SubmitCommand { get; }

    public AddRollModalViewModel(string orderId, Window window)
    {
        _apiService = App.ApiService;
        _window = window;
        OrderId = orderId;
        CancelCommand = new RelayCommand(() => _window.Close());
        SubmitCommand = new RelayCommand(OnSubmit);
    }

    private async void OnSubmit()
    {
        try
        {
            if (String.IsNullOrWhiteSpace(OrderId))
            {
                await UiTools.ShowMessageAsync("Error", $"[Error]: Order ID missing", UiTools.MessageType.Error);
                return;
            }

            if (String.IsNullOrWhiteSpace(RollNumber))
            {
                await UiTools.ShowMessageAsync("Error", $"[Error]: Please enter a roll number", UiTools.MessageType.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(_apiService.ApiAddress))
            {
                await UiTools.ShowMessageAsync("Error", $"[Error]: API Address is not configured", UiTools.MessageType.Error);
                return;
            }

            string apiUrl = $"{_apiService.ApiAddress}/api/Roll/add";
            var content = new StringContent(JsonSerializer.Serialize(
                new { OrderId, RollNumber }),
                System.Text.Encoding.UTF8, "application/json"
            );
            _apiService.AddAuthHeader();
            // var response = await _httpClient.PostAsync(apiUrl, content);
            var response = await _apiService._httpClient.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                // await UiTools.ShowMessageAsync("Success",
                //     $"Roll #{RollNumber} sucessfully added",
                //     UiTools.MessageType.Success
                // );

                RequestCloseWithSuccess?.Invoke(RollNumber);
            }
            else
            {
                var errMsg = await response.Content.ReadAsStringAsync();
                await UiTools.ShowMessageAsync("Error",
                    $"[Error]: {errMsg}",
                    UiTools.MessageType.Error
                );
            }
        }
        catch (Exception ex)
        {
            await UiTools.ShowMessageAsync("Error",
                $"[Error]: {ex.Message}",
                UiTools.MessageType.Error
            );
        }
    }


}
