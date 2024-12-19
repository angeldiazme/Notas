using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using Notas.Services;
using Notas.Models;
using Plugin.AudioRecorder;
using Plugin.Maui.Audio;
using System.IO;

namespace Notas.Views
{
    public partial class CreateNotePage : ContentPage
    {
        private string _newImagePath = null; // Ruta de la nueva imagen capturada
        private Nota _notaActual = null; // Datos de la nota actual cargados desde Firebase

        private string _imagePath; // Ruta de la imagen tomada
        private readonly AudioRecorderService _audioRecorder;
        private IAudioPlayer _audioPlayer; // Reproductor de audio
        private string _audioPath; // Ruta del archivo de audio
        private readonly FirebaseService _firebaseService = new FirebaseService();

        public CreateNotePage()
        {
            InitializeComponent();
            FechaEntry.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            // Configuración del servicio de grabación de audio
            _audioRecorder = new AudioRecorderService
            {
                StopRecordingOnSilence = false,
                TotalAudioTimeout = TimeSpan.FromMinutes(5),
                AudioSilenceTimeout = TimeSpan.FromSeconds(2),
                FilePath = Path.Combine(FileSystem.AppDataDirectory, "audioRecording.wav")
            };
        }

        // Solicitar permisos
        private async Task SolicitarPermisosAsync()
        {
            var status = await Permissions.RequestAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Permiso requerido", "Se necesita acceso al micrófono para grabar audio.", "OK");
            }
        }

        // Evento para tomar una foto
        private async void TomarFoto_Clicked(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.CapturePhotoAsync();
                if (photo != null)
                {
                    // Guardar la foto tomada en una ruta temporal única
                    string localPath = Path.Combine(FileSystem.AppDataDirectory, $"foto_{DateTime.Now:yyyyMMddHHmmss}.jpg");
                    using (var stream = await photo.OpenReadAsync())
                    using (var newStream = File.OpenWrite(localPath))
                    {
                        await stream.CopyToAsync(newStream);
                    }

                    // Actualizar la ruta de la imagen y el control de vista previa
                    _imagePath = localPath;
                    ImagenPreview.Source = ImageSource.FromFile(_imagePath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo tomar la foto: {ex.Message}", "OK");
            }
        }

        // Evento para grabar audio
        private async void GrabarAudio_Clicked(object sender, EventArgs e)
        {
            try
            {
                await SolicitarPermisosAsync();

                if (!_audioRecorder.IsRecording)
                {
                    AudioLabel.Text = "Grabando audio...";
                    await _audioRecorder.StartRecording();
                }
                else
                {
                    await _audioRecorder.StopRecording();
                    _audioPath = _audioRecorder.GetAudioFilePath();

                    if (!string.IsNullOrEmpty(_audioPath) && File.Exists(_audioPath))
                    {
                        AudioLabel.Text = $"Audio guardado: {Path.GetFileName(_audioPath)}";

                        // Crear el reproductor con la ruta del nuevo archivo
                        _audioPlayer = AudioManager.Current.CreatePlayer(_audioPath);

                        AudioControls.IsVisible = true;
                    }
                    else
                    {
                        await DisplayAlert("Error", "No se pudo guardar el archivo de audio.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al grabar audio: {ex.Message}", "OK");
            }
        }

        // Reproducir audio
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

        // Pausar audio
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

        // Detener audio
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

        // Evento para guardar la nota
        private async void GuardarButton_Clicked(object sender, EventArgs e)
        {
            GuardarButton.IsEnabled = false;

            try
            {
                string titulo = TituloEntry.Text;
                string descripcion = DescripcionEntry.Text;
                string fecha = FechaEntry.Text;

                // Validaciones antes de activar el indicador de carga
                if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(descripcion))
                {
                    await DisplayAlert("Error", "El título y la descripción son obligatorios.", "OK");
                    GuardarButton.IsEnabled = true;
                    return;
                }

                // Procesar la imagen
                string imagenBase64 = null;
                if (!string.IsNullOrEmpty(_newImagePath) && File.Exists(_newImagePath))
                {
                    byte[] imageBytes = File.ReadAllBytes(_newImagePath); // Usamos la nueva ruta
                    imagenBase64 = Convert.ToBase64String(imageBytes); // Convertimos a Base64
                    Console.WriteLine($"Nueva imagen Base64: {imagenBase64.Substring(0, 30)}..."); // Log de depuración
                }
                else if (!string.IsNullOrEmpty(_imagePath) && File.Exists(_imagePath))
                {
                    byte[] imageBytes = File.ReadAllBytes(_imagePath); // Fallback por si no se actualizó
                    imagenBase64 = Convert.ToBase64String(imageBytes);
                }
                else
                {
                    Console.WriteLine("No hay imagen válida para guardar en Firebase.");
                }

                // Procesar el audio
                string audioBase64 = null;
                if (!string.IsNullOrEmpty(_audioPath) && File.Exists(_audioPath))
                {
                    byte[] audioBytes = File.ReadAllBytes(_audioPath);
                    audioBase64 = Convert.ToBase64String(audioBytes);
                }

                // Crear la nueva nota
                var nuevaNota = new Nota
                {
                    Titulo = titulo,
                    Descripcion = descripcion,
                    Fecha = fecha,
                    ImagenBase64 = imagenBase64, // Guardar imagen en Firebase
                    AudioBase64 = audioBase64    // Guardar audio en Firebase
                };

                // Activar indicador de carga después de validaciones
                GuardarCargando.IsRunning = true;
                GuardarCargandoContainer.IsVisible = true;

                // Guardar la nota en Firebase
                await _firebaseService.GuardarNotaAsync(nuevaNota);

                await DisplayAlert("Éxito", "Nota guardada correctamente en Firebase.", "OK");

                // Resetear campos
                TituloEntry.Text = "";
                DescripcionEntry.Text = "";
                ImagenPreview.Source = null;
                AudioLabel.Text = "No se ha grabado audio";
                AudioControls.IsVisible = false;
                _audioPath = null;
                _newImagePath = null; // Reseteamos la nueva imagen
                _imagePath = null;    // Reseteamos la imagen actual
                FechaEntry.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al guardar la nota: {ex.Message}", "OK");
            }
            finally
            {
                // Desactivar indicador de carga al finalizar
                GuardarButton.IsEnabled = true;
                GuardarCargando.IsRunning = false;
                GuardarCargandoContainer.IsVisible = false;
            }
        }

    }
}

