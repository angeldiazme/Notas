using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notas.Models
{
    public class Nota
    {

        public string Key { get; set; } // Clave de la nota
        public string Titulo { get; set; } // Título de la nota
        public string Descripcion { get; set; } // Descripción de la nota
        public string Fecha { get; set; } // Fecha de creación o modificación
        public string ImagenBase64 { get; set; } // Imagen en formato Base64
        public string AudioBase64 { get; set; }  // Audio en formato Base64
    }
}
