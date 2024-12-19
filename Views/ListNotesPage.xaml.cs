using Firebase.Database;
using Firebase.Database.Query;
using Notas.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Notas.Views
{
    public partial class ListNotesPage : ContentPage
    {
        private readonly FirebaseClient _firebaseClient;
        private ObservableCollection<Nota> _notas;
        private ObservableCollection<Nota> _notasOriginales; // Lista original de notas

        public ListNotesPage()
        {
            InitializeComponent();
            _firebaseClient = new FirebaseClient("https://examenmiguel-c6ad8-default-rtdb.firebaseio.com/");
            _notas = new ObservableCollection<Nota>();
            _notasOriginales = new ObservableCollection<Nota>(); // Inicializar
            NotasCollectionView.ItemsSource = _notas;
            NotasCollectionView.IsVisible = false; // Ocultar la lista al inicio
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarNotasAsync(); // Cargar las notas cada vez que la página se muestra
        }

        private async Task CargarNotasAsync()
        {
            try
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;
                NotasCollectionView.IsVisible = false; // Ocultar el CollectionView mientras se cargan las notas

                var notas = await _firebaseClient
                    .Child("Notas")
                    .OnceAsync<Nota>();

                _notas.Clear(); // Limpia la colección antes de recargar
                _notasOriginales.Clear();

                foreach (var nota in notas.OrderByDescending(n => DateTime.Parse(n.Object.Fecha)))
                {
                    // Asigna la clave al objeto Nota
                    nota.Object.Key = nota.Key;
                    _notas.Add(nota.Object);
                    _notasOriginales.Add(nota.Object); // Mantén la lista original
                }

                NotasCollectionView.IsVisible = true; // Mostrar la lista cuando las notas están cargadas
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudieron cargar las notas: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async void OnVerDetallesClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var nota = button?.CommandParameter as Nota;

            if (nota != null)
            {
                // Pasa tanto la nota como su clave al constructor
                await Navigation.PushAsync(new NoteDetailsPage(nota, nota.Key));
            }
        }

        private async void OnRegresarClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void OnSearchButtonPressed(object sender, EventArgs e)
        {
            var searchBar = sender as SearchBar;
            var searchText = searchBar?.Text?.ToLower() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Si no hay texto de búsqueda, mostrar todas las notas
                _notas.Clear();
                foreach (var nota in _notasOriginales)
                {
                    _notas.Add(nota);
                }
            }
            else
            {
                // Filtrar notas que coincidan con el texto de búsqueda
                var filteredNotas = _notasOriginales.Where(n =>
                    n.Titulo?.ToLower().Contains(searchText) == true ||
                    n.Descripcion?.ToLower().Contains(searchText) == true);

                _notas.Clear();
                foreach (var nota in filteredNotas)
                {
                    _notas.Add(nota);
                }
            }
        }
    }
}
