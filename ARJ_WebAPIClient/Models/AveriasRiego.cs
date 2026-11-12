using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARJ_WebAPIClient.Models
{
    public class AveriasRiego
    {
        public DateTime fecha { get; set; }
        public string contador { get; set; }
        public string tipo_averia { get; set; }
        public Boolean turno { get; set; }
        public Boolean caudal { get; set; }
        public Decimal consumo_real { get; set; }
        public Decimal consumo_teorico { get; set; }
        public DateTime fin_turno { get; set; }
        public DateTime inicio_turno { get; set; }
        public Decimal valor_caudal { get; set; }
    }
}
