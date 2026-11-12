using ARJ_WebAPIClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARJ_WebAPIClient
{
    public interface IAPIClient
    {
        // Métodos de Lectura
        Task<List<ConsumoResponse>> ObtenerConsumo(string contador, DateTime fechaIni, DateTime fechaFin);
        Task<List<ConsumoResponse>> ObtenerConsumoCabezal(string cabezal, DateTime fechaIni, DateTime fechaFin);

        Task<List<VolumenCabezalResponse>> ObtenerVolumenCabezales();

        // CORRECCIÓN: Ahora es asíncrono y devuelve Task para evitar el bloqueo de CPU
        Task<List<List<decimal>>> ObtenerSerieConsumos(string contador, DateTime fechaIni, DateTime fechaFin);

        Task<Result<List<Lectura>>> ObtenerLecturaContadores(List<string> contadores);

        // Métodos de Escritura / Acción
        Task<Result<int?>> ProgramarTurnosContador(List<Turno> turnos);
        Task<Result<int?>> ProgramarContador(string contador, List<TurnoStandard> turnos);
        Task<Result<int?>> AbrirValvula(string contador);
        Task<Result<int?>> CerrarValvula(string contador);
        Task<Result<int?>> EstadoValvula(string contador);

        // Métodos de Login / Datos
        Task<Result<int>> Login(string usuario, string password);
        Task<Result<DatosRegante>> Datos();
    }
}