using Avalonia.Controls;
using Client.ViewModels;

namespace Client.Views
{
    public partial class Settings : UserControl
    {
        public Settings()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
        }
    }
}
