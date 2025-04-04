using Avalonia.Controls;
using Client.ViewModels;

namespace Client.Views
{
    public partial class Dashboard : UserControl
    {
        public Dashboard()
        {
            InitializeComponent();
            DataContext = new DashboardViewModel();
        }
    }
}
