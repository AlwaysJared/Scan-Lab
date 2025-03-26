using Avalonia.Controls;
using Avalonia.Input;
using Client.ViewModels;
using System.Text.RegularExpressions;

namespace Client.Views
{
    public partial class OrderForm : UserControl
    {
        public OrderForm()
        {
            InitializeComponent();
            DataContext = new OrderFormViewModel();

            // Attach KeyDown event handlers for each input field
            CustomerInitialsInput.KeyDown += OnTextOnlyKeyPress;
            OrderNumberInput.KeyDown += OnAlphanumericOnlyKeyPress;
            FirstRollNumberInput.KeyDown += OnNumberOnlyKeyPress;
            RollCountInput.KeyDown += OnNumberOnlyKeyPress;
        }

        // Allow only letters (A-Z, a-z)
        private void OnTextOnlyKeyPress(object? sender, KeyEventArgs e)
        {
            if (!Regex.IsMatch(e.Key.ToString(), @"^[A-Za-z]$") &&
                e.Key != Key.Back && e.Key != Key.Delete && e.Key != Key.Space)
            {
                e.Handled = true;  // Block invalid input
            }
        }

        // Allow only alphanumeric (A-Z, a-z, 0-9)
        private void OnAlphanumericOnlyKeyPress(object? sender, KeyEventArgs e)
        {
            if (!Regex.IsMatch(e.Key.ToString(), "^[a-zA-Z0-9]+$") &&
                e.Key != Key.Back && e.Key != Key.Delete && e.Key != Key.Space)
            {
                e.Handled = true;  // Block invalid input
            }
        }

        // Allow only numbers (0-9)
        private void OnNumberOnlyKeyPress(object? sender, KeyEventArgs e)
        {
            if (!(e.Key >= Key.D0 && e.Key <= Key.D9) &&  // Top row numbers
                !(e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) &&  // Numpad numbers
                !(e.Key == Key.Back || e.Key == Key.Delete))
            {
                e.Handled = true;  // Block invalid input
            }
        }
    }
}
