namespace CampusRating
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(SignPage), typeof(SignPage));
            Routing.RegisterRoute(nameof(DetalhesLocalPage), typeof(DetalhesLocalPage));
        }
    }
}
