using Notas.Views;
using Notas.Models;


namespace Notas
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new Views.PagePrincipal());
        }
    }
}
