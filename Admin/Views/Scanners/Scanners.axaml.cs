using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Admin.Views
{
    public partial class Scanners : UserControl
    {
        public Scanners()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
