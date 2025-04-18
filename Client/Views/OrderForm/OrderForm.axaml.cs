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
            var isShiftPressed = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            if ((e.Key >= Key.D0 && e.Key <= Key.D9 && isShiftPressed) ||
                (!Regex.IsMatch(e.Key.ToString(), @"^[A-Za-z]$") &&
                e.Key != Key.Back && e.Key != Key.Delete && e.Key != Key.Space && e.Key != Key.Tab))
            {
                e.Handled = true;  // Block invalid input
            }
        }

        // Allow only alphanumeric (A-Z, a-z, 0-9)
        private void OnAlphanumericOnlyKeyPress(object? sender, KeyEventArgs e)
        {
            var isShiftPressed = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            if ((e.Key >= Key.D0 && e.Key <= Key.D9 && isShiftPressed) ||
                (!Regex.IsMatch(e.Key.ToString(), "^[a-zA-Z0-9]+$") &&
                e.Key != Key.Back && e.Key != Key.Delete && e.Key != Key.Space && e.Key != Key.Tab))
            {
                e.Handled = true;  // Block invalid input
            }
        }

        // Allow only number keys (0–9), block letters and special characters
        private void OnNumberOnlyKeyPress(object? sender, KeyEventArgs e)
        {
            // Check if Shift is held down
            var isShiftPressed = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

            if (
                // Block if shift is used with top number row
                (e.Key >= Key.D0 && e.Key <= Key.D9 && isShiftPressed) ||

                // Block if not a number key (top row or numpad) or allowed control key
                (!((e.Key >= Key.D0 && e.Key <= Key.D9) ||
                   (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) ||
                   e.Key == Key.Back ||
                   e.Key == Key.Delete ||
                   e.Key == Key.Tab ||
                   e.Key == Key.Left ||
                   e.Key == Key.Right))
            )
            {
                e.Handled = true;
            }
        }
    }
}
