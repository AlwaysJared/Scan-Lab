using Admin.ViewModels;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Admin.Views
{
    public partial class Orders : UserControl
    {
        public Orders()
        {
            InitializeComponent();
            DataContext = new OrdersViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
