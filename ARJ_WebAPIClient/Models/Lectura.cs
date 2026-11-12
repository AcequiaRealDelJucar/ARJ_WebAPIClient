using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARJ_WebAPIClient.Models
{
    public class Lectura
    {
        public string contador { get; set; }

        public string lectura { get; set; }
    }




    public class RespuestaComprobarProgramacionISRLNew
    {
        public int codigo { get; set; }
        
        public Boolean programada { get; set; }

    }
}
