using Firebase.Database;
using Firebase.Database.Query;
using Notas.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Notas.Services
{
    public class FirebaseService
    {
        private readonly FirebaseClient _firebaseClient;

        public FirebaseService()
        {
            _firebaseClient = new FirebaseClient("https://examenmiguel-c6ad8-default-rtdb.firebaseio.com/");
        }

        // Método para guardar una nota en Firebase
        public async Task GuardarNotaAsync(Nota nota)
        {
            await _firebaseClient
                .Child("Notas") // Ruta donde se guardarán las notas
                .PostAsync(nota);
        }

        // Método para obtener todas las notas desde Firebase
        public async Task<List<Nota>> ObtenerNotasAsync()
        {
            var notas = await _firebaseClient
                .Child("Notas") // Ruta donde se almacenan las notas
                .OnceAsync<Nota>();

            return notas
                .Select(n => new Nota
                {
                    Key = n.Key, // Asignar la clave de Firebase
                    Titulo = n.Object.Titulo,
                    Descripcion = n.Object.Descripcion,
                    Fecha = n.Object.Fecha,
                    ImagenBase64 = n.Object.ImagenBase64,
                    AudioBase64 = n.Object.AudioBase64
                })
                .OrderByDescending(n => DateTime.Parse(n.Fecha)) // Ordenar de más reciente a más antigua
                .ToList();
        }

        // Método para eliminar una nota en Firebase usando su clave
        public async Task EliminarNotaAsync(string notaKey)
        {
            // Valida que se proporcione una clave válida
            if (string.IsNullOrEmpty(notaKey))
            {
                throw new ArgumentException("La clave de la nota no puede ser nula o vacía.", nameof(notaKey));
            }

            // Elimina la nota específica en Firebase
            await _firebaseClient
                .Child("Notas")
                .Child(notaKey)
                .DeleteAsync();
        }

       
        // Método para actualizar una nota en Firebase
        public async Task ActualizarNotaAsync(Nota nota, string notaKey)
        {
            // Validar que la clave y el objeto de la nota no sean nulos o vacíos
            if (string.IsNullOrEmpty(notaKey))
            {
                throw new ArgumentException("La clave de la nota no puede ser nula o vacía.", nameof(notaKey));
            }

            if (nota == null)
            {
                throw new ArgumentNullException(nameof(nota), "El objeto Nota no puede ser nulo.");
            }

            // Validar que los campos requeridos de la nota no estén vacíos
            if (string.IsNullOrWhiteSpace(nota.Titulo))
            {
                throw new ArgumentException("El título de la nota no puede estar vacío.", nameof(nota.Titulo));
            }

            if (string.IsNullOrWhiteSpace(nota.Descripcion))
            {
                throw new ArgumentException("La descripción de la nota no puede estar vacía.", nameof(nota.Descripcion));
            }

            try
            {
                // Actualiza los datos de la nota en Firebase
                await _firebaseClient
                    .Child("Notas")
                    .Child(notaKey) // Usa la clave específica de la nota
                    .PutAsync(nota);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error al actualizar la nota en Firebase.", ex);
            }
        }

        // Método para obtener una nota específica usando su clave
        public async Task<Nota> ObtenerNotaPorClaveAsync(string notaKey)
        {
            if (string.IsNullOrEmpty(notaKey))
            {
                throw new ArgumentException("La clave de la nota no puede ser nula o vacía.", nameof(notaKey));
            }

            var nota = await _firebaseClient
                .Child("Notas")
                .Child(notaKey)
                .OnceSingleAsync<Nota>();

            if (nota != null)
            {
                nota.Key = notaKey; // Asignar la clave al objeto Nota
            }

            return nota;
        }
    }
}
