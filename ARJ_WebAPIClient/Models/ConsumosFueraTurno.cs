using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARJ_WebAPIClient.Models
{
    public class ConsumosFueraTurno
    {
        public string contador { get; set; }
        public List<Consumos> consumos { get; set; }
    }


    public class Consumos
    {
        public DateTime fecha { get; set; }
        public Decimal total { get; set; }
        public Decimal dentroTurno { get; set; }
        public Decimal fueraTurno { get; set; }

    }

}
