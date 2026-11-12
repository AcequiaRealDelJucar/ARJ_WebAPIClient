using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARJ_WebAPIClient.Models
{
    public class PeticionProgramacion
    {
            public string idPeticion { get; set; }

            public int idEmpresa { get; set; }

            public List<TurnoStandard> listaTurnos { get; set; }

    }
}
