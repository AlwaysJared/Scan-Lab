using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Admin.Views
{
    public partial class StaffManagement : UserControl
    {
        public StaffManagement()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
