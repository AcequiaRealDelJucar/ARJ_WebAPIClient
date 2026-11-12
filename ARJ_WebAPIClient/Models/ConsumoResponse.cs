using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARJ_WebAPIClient.Models
{
    public class ConsumoResponse
    {
        public string contador { get; set; }

        public DateTime inicio { get; set; }

        public DateTime fin { get; set; }

        public decimal volumen { get; set; }

        public decimal consumo { get; set; }
    }
}
