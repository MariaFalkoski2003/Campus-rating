using Microsoft.Extensions.DependencyInjection;

namespace CampusRating
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell()) { Title = "Campus Rating" };
        }
    }
}
