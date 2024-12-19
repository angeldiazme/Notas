using Microsoft.Maui.Controls;
using Notas.Models;
using Notas.Services;
using Plugin.Maui.Audio;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Notas.Views
{
    public partial class NoteDetailsPage : ContentPage
    {
        private readonly FirebaseService _firebaseService = new FirebaseService();
        private IAudioPlayer _audioPlayer; // Reproductor de audio
        private IAudioPlayer _newAudioPlayer; // Reproductor de nuevo audio
        private string _audioPath; // Ruta del archivo de audio actual
        private string _imagePath; // Ruta de la imagen actual
        private string _newAudioPath; // Ruta del nuevo audio
        private string _newImagePath; // Ruta de la nueva imagen
        private Nota _notaActual;
        private string _notaKey; // Clave de la nota en Firebase
        private bool _editMode = false; // Estado del modo de edici�n
        private Plugin.AudioRecorder.AudioRecorderService _audioRecorder = new Plugin.AudioRecorder.AudioRecorderService();

        public NoteDetailsPage(Nota nota, string notaKey)
        {
            InitializeComponent();

            _notaActual = nota;
            _notaKey = notaKey;

            if (string.IsNullOrEmpty(_notaKey))
            {
                DisplayAlert("Error", "La clave de la nota es inv�lida.", "OK");
                return;
            }
            RellenarCamposNota();
        }
        private void RellenarCamposNota()
        {
            TituloEntry.Text = _notaActual.Titulo;
            DescripcionEditor.Text = _notaActual.Descripcion;
            FechaEntry.Text = _notaActual.Fecha;

            // Configurar imagen desde Firebase
            if (!string.IsNullOrEmpty(_notaActual.ImagenBase64))
            {
                try
                {
                    var imageBytes = Convert.FromBase64String(_notaActual.ImagenBase64);

                    // Usar una ruta din�mica basada en la clave de la nota
                    _imagePath = Path.Combine(FileSystem.AppDataDirectory, $"nota_imagen_{_notaKey}_{Guid.NewGuid()}.jpg");

                    // Escribir la imagen en la ruta especificada
                    File.WriteAllBytes(_imagePath, imageBytes);

                    // Actualizar la vista previa de la imagen
                    ImagenPreview.Source = ImageSource.FromFile(_imagePath);
                }
                catch (Exception ex)
                {
                    DisplayAlert("Error", $"No se pudo cargar la imagen: {ex.Message}", "OK");
                }
            }
            else
            {
                ImagenPreview.Source = null; // En caso de que no haya imagen
            }

            // Configurar audio desde Firebase
            if (!string.IsNullOrEmpty(_notaActual.AudioBase64))
            {
                try
                {
                    var audioBytes = Convert.FromBase64String(_notaActual.AudioBase64);

                    // Usar una ruta din�mica basada en la clave de la nota
                    _audioPath = Path.Combine(FileSystem.AppDataDirectory, $"nota_audio_{_notaKey}_{Guid.NewGuid()}.mp3");

                    // Escribir el archivo de audio en la ruta especificada
                    File.WriteAllBytes(_audioPath, audioBytes);

                    // Crear el reproductor de audio
                    _audioPlayer = AudioManager.Current.CreatePlayer(_audioPath);

                    // Actualizar el texto del indicador de audio
                    AudioLabel.Text = "Audio cargado.";
                }
                catch (Exception ex)
                {
                    DisplayAlert("Error", $"No se pudo cargar el audio: {ex.Message}", "OK");
                }
            }
            else
            {
                AudioLabel.Text = "No se ha cargado ning�n audio.";
            }
        }
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _audioPlayer?.Dispose();
            _audioPlayer = null;
            _newAudioPlayer?.Dispose();
            _newAudioPlayer = null;
        }
        private void ModificarNota_Clicked(object sender, EventArgs e)
        {
            _editMode = !_editMode;

            TituloEntry.IsEnabled = _editMode;
            DescripcionEditor.IsEnabled = _editMode;
            ActualizarButton.IsEnabled = _editMode;
            ActualizarImagenButton.IsEnabled = _editMode;
            ActualizarAudioButton.IsEnabled = _editMode;

            ModificarButton.Text = _editMode ? "Cancelar" : "Modificar";
        }
        private async void ActualizarImagen_Clicked(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.CapturePhotoAsync();
                if (photo != null)
                {
                    string localPath = Path.Combine(FileSystem.AppDataDirectory, photo.FileName);
                    using (var stream = await photo.OpenReadAsync())
                    using (var newStream = File.OpenWrite(localPath))
                    {
                        await stream.CopyToAsync(newStream);
                    }

                    _newImagePath = localPath;
                    ImagenPreview.Source = ImageSource.FromFile(_newImagePath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo actualizar la imagen: {ex.Message}", "OK");
            }
        }
        private async void ActualizarAudio_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (_audioRecorder.IsRecording)
                {
                    // Detener la grabaci�n
                    await _audioRecorder.StopRecording();
                    _newAudioPath = _audioRecorder.FilePath;

                    if (!string.IsNullOrEmpty(_newAudioPath) && File.Exists(_newAudioPath))
                    {
                        AudioLabel.Text = "Nuevo audio guardado.";
                        _newAudioPlayer = AudioManager.Current.CreatePlayer(_newAudioPath);
                    }
                    else
                    {
                        await DisplayAlert("Error", "No se pudo guardar el archivo de audio.", "OK");
                    }
                }
                else
                {
                    // Solicitar permisos antes de iniciar la grabaci�n
                    var micStatus = await Permissions.RequestAsync<Permissions.Microphone>();
                    if (micStatus != PermissionStatus.Granted)
                    {
                        await DisplayAlert("Permiso Denegado", "Se necesita acceso al micr�fono para grabar audio.", "OK");
                        return;
                    }

                    // Iniciar la grabaci�n sin l�mite de tiempo
                    await DisplayAlert("Grabaci�n", "Iniciando grabaci�n. Presione nuevamente para detener.", "OK");
                    await _audioRecorder.StartRecording();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al manejar la grabaci�n de audio: {ex.Message}", "OK");
            }
        }
        private void ReproducirAudio_Clicked(object sender, EventArgs e)
        {
            try
            {
                _audioPlayer?.Play();
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"Error al reproducir audio: {ex.Message}", "OK");
            }
        }

        private async void Regresar_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void PausarAudio_Clicked(object sender, EventArgs e)
        {
            try
            {
                _audioPlayer?.Pause();
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"Error al pausar audio: {ex.Message}", "OK");
            }
        }

        private void DetenerAudio_Clicked(object sender, EventArgs e)
        {
            try
            {
                _audioPlayer?.Stop();
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"Error al detener audio: {ex.Message}", "OK");
            }
        }
        private async void EliminarNota_Clicked(object sender, EventArgs e)
        {
            bool confirmar = await DisplayAlert("Confirmaci�n", "�Desea eliminar esta nota?", "S�", "No");

            if (confirmar)
            {
                try
                {
                    CargandoIndicator.IsRunning = true;
                    CargandoIndicator.IsVisible = true;

                    await _firebaseService.EliminarNotaAsync(_notaKey);
                    await DisplayAlert("�xito", "La nota fue eliminada correctamente.", "OK");
                    await Navigation.PopAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Error al eliminar la nota: {ex.Message}", "OK");
                }
                finally
                {
                    CargandoIndicator.IsRunning = false;
                    CargandoIndicator.IsVisible = false;
                }
            }
        }

        private async void ActualizarNota_Clicked(object sender, EventArgs e)
        {
            try
            {
                CargandoIndicator.IsRunning = true;
                CargandoIndicator.IsVisible = true;

                if (string.IsNullOrWhiteSpace(TituloEntry.Text) || string.IsNullOrWhiteSpace(DescripcionEditor.Text))
                {
                    await DisplayAlert("Error", "El t�tulo y la descripci�n son obligatorios.", "OK");
                    return;
                }

                // Actualizar los campos de la nota
                _notaActual.Titulo = TituloEntry.Text;
                _notaActual.Descripcion = DescripcionEditor.Text;

                // Actualizar imagen si se tom� una nueva
                if (!string.IsNullOrEmpty(_newImagePath) && File.Exists(_newImagePath))
                {
                    var imageBytes = File.ReadAllBytes(_newImagePath);
                    _notaActual.ImagenBase64 = Convert.ToBase64String(imageBytes);
                }

                // Actualizar audio si se grab� uno nuevo
                if (!string.IsNullOrEmpty(_newAudioPath) && File.Exists(_newAudioPath))
                {
                    var audioBytes = File.ReadAllBytes(_newAudioPath);
                    _notaActual.AudioBase64 = Convert.ToBase64String(audioBytes);
                }

                // Actualizar la nota en Firebase
                await _firebaseService.ActualizarNotaAsync(_notaActual, _notaKey);

                await DisplayAlert("�xito", "La nota se actualiz� correctamente.", "OK");

                ModificarNota_Clicked(null, null); // Desactivar el modo de edici�n
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al actualizar la nota: {ex.Message}", "OK");
            }
            finally
            {
                await Navigation.PopAsync();
                CargandoIndicator.IsRunning = false;
                CargandoIndicator.IsVisible = false;
            }
        }
    }
}