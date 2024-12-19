using Notas.Models;
namespace Notas.Views;

public partial class PagePrincipal : ContentPage
{
	public PagePrincipal()
	{
		InitializeComponent();
		
    }
    // Evento para navegar a la página de creación de notas
    private async void ButtonCrear_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CreateNotePage());
    }

    // Evento para navegar a la página de lista de notas
    private async void ButtonVerNotas_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ListNotesPage());
    }
}