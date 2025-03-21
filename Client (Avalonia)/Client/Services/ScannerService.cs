using Libs.Data.Models;
using System;
using System.IO;
using System.Text.Json;

namespace Client.Services;

public class ScannerService
{
    private const string ScannerSaveFile = "selected_scanner.json"; // ✅ File for persistence
    private Scanner? _selectedScanner;

    public event Action<Scanner?>? ScannerChanged; // ✅ Notify subscribers when the scanner changes

    public Scanner? SelectedScanner
    {
        get => _selectedScanner;
        set
        {
            if (_selectedScanner != value)
            {
                _selectedScanner = value;
                SaveSelectedScanner();
                ScannerChanged?.Invoke(_selectedScanner); // ✅ Notify all pages
            }
        }
    }

    public ScannerService()
    {
        LoadSavedScanner(); // ✅ Load scanner on startup
    }

    public void LoadSavedScanner()
    {
        if (File.Exists(ScannerSaveFile))
        {
            try
            {
                string json = File.ReadAllText(ScannerSaveFile);
                _selectedScanner = JsonSerializer.Deserialize<Scanner>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error loading saved scanner: {ex.Message}");
            }
        }
    }

    public void SaveSelectedScanner()
    {
        try
        {
            if (_selectedScanner != null)
            {
                string json = JsonSerializer.Serialize(_selectedScanner, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ScannerSaveFile, json);
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error saving selected scanner: {ex.Message}");
        }
    }
}
