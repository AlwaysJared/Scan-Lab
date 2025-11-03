using Admin.ViewModels;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Admin.Views
{
    public partial class Dashboard : UserControl
    {
        public Dashboard()
        {
            InitializeComponent();
            // DataContext = new DashboardViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
