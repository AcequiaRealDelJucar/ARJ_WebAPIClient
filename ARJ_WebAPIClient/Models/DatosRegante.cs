using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ARJ_WebAPIClient.Models
{
    public class DatosRegante
    {
        public int IdRegante { get; set; }
        public string Nombre { get; set; }
        public string Apellidos { get; set; }
        public List<ContadorApp> Contadores { get; set; }

    }
    public class ContadorApp
    {
        public string Contador { get; set; }
        public string Termino { get; set; }
        public string Poligono { get; set; }
        public string Parcelas { get; set; }
        public string Cultivo { get; set; }
        public string Variedad { get; set; }
        public decimal Superficie { get; set; }

    }

}