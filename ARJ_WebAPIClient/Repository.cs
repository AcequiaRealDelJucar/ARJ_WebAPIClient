using ARJ_WebAPIClient.Properties;
using ARJCore.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARJ_WebAPIClient
{
    internal class Repository
    {

        private const string SELECT_SUPERFICIE_CONTADOR =
            @"SELECT SUM([superficie_rl]) / 831.09 AS sup
            FROM [actual].[dbo].[rl_electrovalvulas]
            WHERE contador = @contador";

        
        internal static decimal getSuperficieContador(string contador)
        {
            MsSQLServerDataProvider actualProvider = new MsSQLServerDataProvider(Settings.Default.actualConnectionString);

            Dictionary<string, object> parametros = new Dictionary<string, object>
            {
                { "@contador", contador.Trim() }
            };

            return actualProvider.ejecutarEscalar<decimal>(SELECT_SUPERFICIE_CONTADOR, parametros);
        }

       
    }
}
