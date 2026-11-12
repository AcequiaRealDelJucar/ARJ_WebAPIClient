using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARJ_WebAPIClient.Models
{
    public class Turno
    {
        public string Contador { get; set; }

        public int Dia { get; set; }

        public string HoraInicio { get; set; }

        public int Minutos { get; set; }
    }


    public class TurnoStandard
    {
        
        public int? id { get; set; }
        public string contador { get; set; }

        public int? diaSemana { get; set; }

        public int? numTurno { get; set; }

        public string inicioRiego { get; set; }

        public string finRiego { get; set; }

        public int? minutos { get; set; }

        public long? volumenMax { get; set; }

        public int? estadoProg { get; set; }

        public double? volumen { get; set; }

        public string patron { get; set; }

        public double? superficie_rl { get; set; }

        public string idPeticion { get; set; }

    }

}
