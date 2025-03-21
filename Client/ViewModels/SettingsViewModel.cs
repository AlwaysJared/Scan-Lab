using System;
using System.Windows.Input;
using Client.Interfaces;
using CommunityToolkit.Maui.Storage;
using Libs.Data.Models;
using Microsoft.Maui.Controls;

namespace Client.ViewModels
{
    public class SettingsViewModel : BindableObject
    {
        private Scanner _selectedScanner;
        private bool _isScannerEditable;
        private bool _isAPIEditable;

        public Scanner SelectedScanner
        {
            get => _selectedScanner;
            set
            {
                _selectedScanner = value;
                OnPropertyChanged();
            }
        }

        public bool IsScannerEditable
        {
            get => _isScannerEditable;
            set
            {
                _isScannerEditable = value;
                OnPropertyChanged();
            }
        }

        public bool IsAPIEditable
        {
            get => _isAPIEditable;
            set
            {
                _isAPIEditable = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand SaveScannerCommand => new Command(SaveScannerConfiguration);
        public ICommand CancelScannerCommand => new Command(CancelScannerConfiguration);
        public ICommand SaveApiCommand => new Command(SaveAPIConfiguration);
        public ICommand CancelApiCommand => new Command(CancelAPIConfiguration);
        public ICommand TestApiCommand => new Command(TestAPIUrl);

        // Commands for Directory Browsing
        public ICommand BrowseWatchedDir => new Command(async () => await OpenFolderPicker("WatchedDir"));
        public ICommand BrowseDestinationDir => new Command(async () => await OpenFolderPicker("DestinationDir"));
        public ICommand BrowseArchiveDir => new Command(async () => await OpenFolderPicker("ArchiveDir"));

        // Open Folder Picker and assign directory to the corresponding property
        private async Task OpenFolderPicker(string property)
        {
            try
            {
                // // Get the current directory from the property if set, otherwise use a default location (e.g., Home directory)
                // string initialDirectory = GetInitialDirectory(property);

                // // Use DependencyService to pick the folder using platform-specific code
                // var folderPicker = DependencyService.Get<IFolderPicker>();
                // var selectedFolder = await folderPicker.PickFolderAsync(initialDirectory);

                var selectedFolder = await FolderPicker.PickAsync(default);

                if (selectedFolder.IsSuccessful)
                {
                    if (!string.IsNullOrEmpty(selectedFolder.Folder.Path))
                    {
                        var tempScnr = SelectedScanner;
                        // Set the selected directory based on the property name
                        if (property == "WatchedDir")
                        {
                            tempScnr.WatchedDir = selectedFolder.Folder.Path;
                        }
                        else if (property == "DestinationDir")
                        {
                            tempScnr.DestinationDir = selectedFolder.Folder.Path;
                        }
                        else if (property == "ArchiveDir")
                        {
                            tempScnr.ArchiveDir = selectedFolder.Folder.Path;
                        }

                        SelectedScanner = tempScnr;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions, e.g., user canceled the picker, or error occurred
                Console.WriteLine($"Error selecting folder: {ex.Message}");
            }
        }

        // Helper method to get the initial directory to open based on the property
        private string GetInitialDirectory(string property)
        {
            string directory = null;

            switch (property)
            {
                case "WatchedDir":
                    directory = SelectedScanner?.WatchedDir;
                    break;
                case "DestinationDir":
                    directory = SelectedScanner?.DestinationDir;
                    break;
                case "ArchiveDir":
                    directory = SelectedScanner?.ArchiveDir;
                    break;
            }

            // Return directory if valid, otherwise return null (which will default to the home directory)
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // Default to Documents folder
            }

            return directory;
        }

        private void SaveScannerConfiguration()
        {
            // Add logic to save the scanner configuration
            IsScannerEditable = false;
        }
        private void CancelScannerConfiguration()
        {
            // Reset values or cancel editing
            IsScannerEditable = false;
        }
        private void SaveAPIConfiguration()
        {
            // Add logic to save the API configuration
            IsAPIEditable = false;
        }
        private void CancelAPIConfiguration()
        {
            // Reset values or cancel editing
            IsAPIEditable = false;
        }
        private void TestAPIUrl()
        {
            // Test the API URL
            // Implement logic to test the API URL here.
        }
    }
}
