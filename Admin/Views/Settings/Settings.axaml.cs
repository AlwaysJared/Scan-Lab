using Avalonia.Controls;
using Admin.ViewModels;

namespace Admin.Views
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
