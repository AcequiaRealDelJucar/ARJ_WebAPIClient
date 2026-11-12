using Newtonsoft.Json;
using System;

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ARJ_WebAPIClient.Models
{
    public class VolumenCabezalResponse
    {
        [JsonPropertyName("nombre")]
        public string nombre { get; set; }

        // Utilizo decimal por ser la mejor práctica para medidas volumétricas exactas
        [JsonPropertyName("volumen_jornada_anterior")]
        public decimal volumen_jornada_anterior { get; set; }


        [JsonPropertyName("timestamp")]
        public DateTime? timestamp { get; set; }
    }


    public class CabezalesRootResponse
    {
        [JsonPropertyName("cabezales")]
        public List<VolumenCabezalResponse> Cabezales { get; set; } = new List<VolumenCabezalResponse>();
    }
}



