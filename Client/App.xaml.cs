using Microsoft.Maui.Controls;

namespace Client
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Set the AppShell as the main page
            MainPage = new AppShell();
        }
    }
}
