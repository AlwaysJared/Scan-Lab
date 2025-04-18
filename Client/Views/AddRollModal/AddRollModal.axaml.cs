using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Client.ViewModels;
using Client.Models;
using System;
using Avalonia.Platform;

namespace Client.Views
{
    public partial class AddRollModal : Window
    {
        private TaskCompletionSource<bool> _tcs = new();

        private TaskCompletionSource<AddRollModalResult>? _resultSource;

        public AddRollModal(string orderId)
        {
            InitializeComponent();

            var uri = new Uri("avares://Client/Assets/film-roll.png");
            var stream = AssetLoader.Open(uri); // Will throw if path is wrong
            Icon = new WindowIcon(stream);

            var viewModel = new AddRollModalViewModel(orderId, this);
            viewModel.RequestCloseWithSuccess += rollNumber => Close(new AddRollModalResult
            {
                Success = true,
                RollNumber = rollNumber
            });
            viewModel.RequestClose += () => Close(new AddRollModalResult { Success = false });

            DataContext = viewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Optionally, you can initialize or set up anything specific here
        }

        // This method can be used to close the modal programmatically
        public void CloseModal()
        {
            this.Close();
        }

        public Task<AddRollModalResult> ShowDialogWithResult(Window owner)
        {
            return ShowDialog<AddRollModalResult>(owner);
        }
    }
}
