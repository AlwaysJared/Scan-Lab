using Admin.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Admin.Views
{
    public partial class ActivityLog : UserControl
    {
        public ActivityLog()
        {
            InitializeComponent();
            DataContext = new ActivityLogViewModel();
        }

        protected override async void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            if (DataContext is ActivityLogViewModel vm)
                await vm.InitPageAsync();
        }
    }
}
