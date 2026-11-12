using ARJ_Utils.Extensions;
using ARJ_WebApiClient.OAuth;
using ARJ_WebAPIClient.Models;
using ARJ_WebAPIClient.Properties;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ARJ_WebAPIClient
{
    public class APIClient : IAPIClient
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        // Mantenemos una única instancia estática y segura para toda la aplicación
        private static HttpClient client = new HttpClient() { BaseAddress = new Uri(Settings.Default.BaseAddress) };

        private OAuthResponse token;
        private DateTime fechaToken;

        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        public APIClient(string clientId, string clientSecret)
        {
            this.ClientId = clientId;
            this.ClientSecret = clientSecret;
        }

        #region Helpers de Arquitectura (Seguridad de Hilos y Memoria)

        // 1. Evita la condición de carrera creando un Request independiente por cada petición
        private HttpRequestMessage CrearPeticion(HttpMethod metodo, string url)
        {
            var request = new HttpRequestMessage(metodo, url);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (token != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(token.token_type, token.access_token);
            }
            return request;
        }

        // 2. Lee directamente del Stream para evitar colapsar la memoria RAM con strings gigantes
 /*       private async Task<T> LeerYDeserializarAsync<T>(HttpResponseMessage response)
        {
            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var streamReader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                var serializer = new JsonSerializer();
                return serializer.Deserialize<T>(jsonReader);
            }
        }*/

        // Dentro de tu APIClient, tu método de deserialización debería verse así:
        protected async Task<T> LeerYDeserializarAsync<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // Configuración Senior para evitar problemas de fechas (cultura, zonas horarias, etc)
            var settings = new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.DateTime,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc, // Si tus fechas son UTC
                NullValueHandling = NullValueHandling.Ignore
            };

            return JsonConvert.DeserializeObject<T>(content, settings);
        }




        // Clase auxiliar interna para mapear propiedades "Data" anidadas sin usar Dynamics (más rápido y ligero)
        private class RespuestaApi<T>
        {
            public T Data { get; set; }
        }

        #endregion

        public async Task<List<ConsumoResponse>> ObtenerConsumo(string contador, DateTime fechaIni, DateTime fechaFin)
        {
            List<ConsumoResponse> listaConsumos = new List<ConsumoResponse>();
            try
            {
                if (!ClienteAutentificado()) AutentificarCliente();

                var url = string.Format(
                    Settings.Default.ObtenerConsumo,
                    contador,
                    fechaIni.ToString("yyyy-MM-dd"),
                    fechaFin.ToString("yyyy-MM-dd")
                );

                var request = CrearPeticion(HttpMethod.Get, url);

                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();

                    var consumosWraper = await LeerYDeserializarAsync<RespuestaApi<List<ConsumoResponse>>>(response);
                    if (consumosWraper?.Data != null)
                    {
                        listaConsumos = consumosWraper.Data;
                    }
                }

                if (listaConsumos.Count > 0)
                {
                    var superficie = Repository.getSuperficieContador(contador);
                    foreach (var con in listaConsumos)
                    {
                        con.consumo = calcularConsumo(con.volumen, superficie, (con.fin - con.inicio).TotalMinutes);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e, $"Error al ObtenerConsumo para contador {contador}");
            }

            return listaConsumos ?? new List<ConsumoResponse>();
        }

   

        public async Task<List<ConsumoResponse>> ObtenerConsumoCabezal(string cabezal, DateTime fechaIni, DateTime fechaFin)
        {
            try
            {
                if (!ClienteAutentificado()) AutentificarCliente();

                var url = string.Format(
                    Settings.Default.ObtenerConsumoCabezal,
                    cabezal,
                    fechaIni.ToString("yyyy-MM-dd"),
                    fechaFin.ToString("yyyy-MM-dd")
                );

                var request = CrearPeticion(HttpMethod.Get, url);

                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    var consumosWraper = await LeerYDeserializarAsync<RespuestaApi<List<ConsumoResponse>>>(response);
                    return consumosWraper?.Data ?? new List<ConsumoResponse>();
                }
            }
            catch (Exception e)
            {
                logger.Error(e, $"Error al ObtenerConsumoCabezal para cabezal {cabezal}");
                return new List<ConsumoResponse>();
            }
        }

        // CORRECCIÓN: Convertido a async Task para evitar bloqueos (Thread Starvation)
        public async Task<List<List<decimal>>> ObtenerSerieConsumos(string contador, DateTime fechaIni, DateTime fechaFin)
        {
            List<List<decimal>> serieConsumos = new List<List<decimal>>();

            var consumosData = await ObtenerConsumo(contador, fechaIni, fechaFin);
            var superficie = Repository.getSuperficieContador(contador);

            var consumos = consumosData.Select(c => new
            {
                fecha = c.fin,
                consumo = calcularConsumo(c.volumen, superficie, (c.fin - c.inicio).TotalMinutes)
            }).ToList();

            foreach (var consumo in consumos)
            {
                serieConsumos.Add(new List<decimal>() { Convert.ToDecimal(consumo.fecha.Date.GetJavascriptTimestamp()), consumo.consumo });
            }

            return serieConsumos;
        }

        // CORRECCIÓN: Convertido a async Task para evitar bloqueos
        public async Task<List<ConsumoResponse>> ObtenerSerieConsumosParaCorreccionesVT(string contador, DateTime fechaIni, DateTime fechaFin)
        {
            List<ConsumoResponse> serieConsumos = new List<ConsumoResponse>();

            var consumosData = await ObtenerConsumo(contador, fechaIni, fechaFin);
            var superficie = Repository.getSuperficieContador(contador);

            var consumos = consumosData.Select(c => new
            {
                fechaini = c.inicio,
                fechafin = c.fin,
                consumo = calcularConsumo(c.volumen, superficie, (c.fin - c.inicio).TotalMinutes),
                volumen = c.volumen
            }).ToList();

            foreach (var consumo in consumos)
            {
                serieConsumos.Add(new ConsumoResponse { contador = contador, inicio = consumo.fechaini, fin = consumo.fechafin, consumo = consumo.consumo, volumen = consumo.volumen });
            }

            return serieConsumos;
        }

        public async Task<Result<List<Lectura>>> ObtenerLecturaContadores(List<string> contadores)
        {
            try
            {
                if (!ClienteAutentificado()) AutentificarCliente();

                var request = CrearPeticion(HttpMethod.Post, Settings.Default.ObtenerLecturaContadores);
                request.Content = new StringContent(JsonConvert.SerializeObject(contadores), Encoding.UTF8, "application/json");

                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    var lecturas = await LeerYDeserializarAsync<Result<List<Lectura>>>(response);

                    return new Result<List<Lectura>>
                    {
                        StatusCode = 1,
                        Data = lecturas.Data.Select(c => new Lectura()
                        {
                            contador = c.contador.StartsWith("2800") ? "103" + c.contador.Substring(4) : c.contador,
                            lectura = c.lectura
                        }).ToList(),
                        ResponseMessage = "",
                        RawResponse = ""
                    };
                }
            }
            catch (Exception ex)
            {
                return new Result<List<Lectura>> { StatusCode = -1, Data = new List<Lectura>(), ResponseMessage = ex.Message };
            }
        }

        public async Task<Result<int?>> ProgramarTurnosContador(List<Turno> turnos)
        {
            try
            {
                if (!ClienteAutentificado()) AutentificarCliente();

                var request = CrearPeticion(HttpMethod.Post, Settings.Default.ProgramarTurnos);
                request.Content = new StringContent(JsonConvert.SerializeObject(turnos), Encoding.UTF8, "application/json");

                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await LeerYDeserializarAsync<Result<int?>>(response);
                }
            }
            catch (Exception ex)
            {
                return new Result<int?> { StatusCode = -1, Data = -1, ResponseMessage = ex.Message };
            }
        }

        public async Task<Result<int?>> ProgramarContador(string contador, List<TurnoStandard> turnos)
        {
            try
            {
                if (!ClienteAutentificado()) AutentificarCliente();

                var diccionario = new Dictionary<string, List<TurnoStandard>> { { contador, turnos } };
                var request = CrearPeticion(HttpMethod.Post, Settings.Default.ProgramarContador);
                request.Content = new StringContent(JsonConvert.SerializeObject(diccionario), Encoding.UTF8, "application/json");

                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await LeerYDeserializarAsync<Result<int?>>(response);
                }
            }
            catch (Exception ex)
            {
                return new Result<int?> { StatusCode = -1, Data = -1, ResponseMessage = ex.Message };
            }
        }

        public async Task<Result<int?>> ProgramarCabezal(string cabezal, List<TurnoStandard> listaTurnos)
        {
            try
            {
                if (!ClienteAutentificado()) AutentificarCliente();

                var diccionario = new Dictionary<string, List<TurnoStandard>> { { cabezal, listaTurnos } };
                var request = CrearPeticion(HttpMethod.Post, Settings.Default.ProgramarCabezal);
                request.Content = new StringContent(JsonConvert.SerializeObject(diccionario), Encoding.UTF8, "application/json");

                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await LeerYDeserializarAsync<Result<int?>>(response);
                }
            }
            catch (Exception ex)
            {
                return new Result<int?> { StatusCode = -1, Data = -1, ResponseMessage = ex.Message };
            }
        }

        public async Task<Result<int?>> ProgramarCabezalNEW(string cabezal, PeticionProgramacion peticion, int idTraza)
        {
            Console.WriteLine("    En apiclient petición con :" + peticion.listaTurnos.Count() + "num turnos");
            try
            {
                if (!ClienteAutentificado()) AutentificarCliente();

                var objectDataToPost = new DataToPost { IdTraza = idTraza, Peticion = peticion.listaTurnos };
                var request = CrearPeticion(HttpMethod.Post, Settings.Default.ProgramarCabezalNEW);
                request.Content = new StringContent(JsonConvert.SerializeObject(objectDataToPost), Encoding.UTF8, "application/json");

                logger.Debug("    En apiclient, content length: " + request.Content.Headers.ContentLength);

                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await LeerYDeserializarAsync<Result<int?>>(response);
                }
            }
            catch (Exception ex)
            {
                string asunto = "Error al enviar la programación con idTraza: " + idTraza;
                string mensaje = "" + ex.Message;
                //enviarEmail(asunto, mensaje);

                return new Result<int?> { StatusCode = -1, Data = -1, ResponseMessage = ex.Message };
            }
        }

        public static void enviarEmail(string asunto, string mensaje)
        {
            List<string> destinatarios = new List<string> { "mmillet@acequiarj.es" };
            /*destinatarios.Add("centrocontrol@acequiarj.es");
            destinatarios.Add("mercheorts@acequiarj.es");*/
            Servicios.Sender.enviarMensaje(destinatarios, asunto, mensaje);
        }

        public async Task<Result<int?>> EliminarTurnosContador(string contador)
        {
            try
            {
                if (!ClienteAutentificado()) AutentificarCliente();

                var request = CrearPeticion(HttpMethod.Post, Settings.Default.EliminarTurnos);
                request.Content = new StringContent(JsonConvert.SerializeObject(new List<string> { contador }), Encoding.UTF8, "application/json");

                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await LeerYDeserializarAsync<Result<int?>>(response);
                }
            }
            catch (Exception ex)
            {
                return new Result<int?> { StatusCode = -1, Data = -1, ResponseMessage = ex.Message };
            }
        }

        public async Task<Result<int?>> EliminarTurnosCabezal(string cabezal)
        {
            try
            {
                if (!ClienteAutentificado()) AutentificarCliente();

                var request = CrearPeticion(HttpMethod.Post, Settings.Default.EliminarTurnosCabezal);
                request.Content = new StringContent(JsonConvert.SerializeObject(cabezal), Encoding.UTF8, "application/json");

                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await LeerYDeserializarAsync<Result<int?>>(response);
                }
            }
            catch (Exception ex)
            {
                return new Result<int?> { StatusCode = -1, Data = -1, ResponseMessage = ex.Message };
            }
        }

        public async Task<Result<int?>> AbrirValvula(string contador)
        {
            try
            {
                if (!ClienteAutentificado()) AutentificarCliente();

                var url = string.Format(Settings.Default.AbrirValvula, contador);
                var request = CrearPeticion(HttpMethod.Get, url);

                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await LeerYDeserializarAsync<Result<int?>>(response);
                }
            }
            catch (Exception ex)
            {
                return new Result<int?> { StatusCode = -1, Data = -1, ResponseMessage = ex.Message };
            }
        }

        public async Task<Result<int?>> CerrarValvula(string contador)
        {
            try
            {
                if (!ClienteAutentificado()) AutentificarCliente();

                var url = string.Format(Settings.Default.CerrarValvula, contador);
                var request = CrearPeticion(HttpMethod.Get, url);

                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await LeerYDeserializarAsync<Result<int?>>(response);
                }
            }
            catch (Exception ex)
            {
                return new Result<int?> { StatusCode = -1, Data = -1, ResponseMessage = ex.Message };
            }
        }

        public async Task<Result<int?>> EstadoValvula(string contador)
        {
            try
            {
                if (!ClienteAutentificado()) AutentificarCliente();

                var url = string.Format(Settings.Default.EstadoValvula, contador);
                var request = CrearPeticion(HttpMethod.Get, url);

                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await LeerYDeserializarAsync<Result<int?>>(response);
                }
            }
            catch (Exception ex)
            {
                return new Result<int?> { StatusCode = -1, Data = -1, ResponseMessage = ex.Message };
            }
        }

        public async Task<Result<List<AveriasRiego>>> ObtenerAveriasRiego(string fecha)
        {
            try
            {
                if (!ClienteAutentificado()) AutentificarCliente();

                var url = string.Format(Settings.Default.ObtenerAverias, fecha);
                var request = CrearPeticion(HttpMethod.Get, url);

                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await LeerYDeserializarAsync<Result<List<AveriasRiego>>>(response);
                }
            }
            catch (Exception ex)
            {
                return new Result<List<AveriasRiego>> { StatusCode = -1, Data = new List<AveriasRiego>(), ResponseMessage = ex.Message };
            }
        }

        public async Task<Result<List<ConsumosFueraTurno>>> ObtenerConsumosFueraTurnoCabezal(string cabezal, string fechaInicio, string fechaFin)
        {
            try
            {
                if (!ClienteAutentificado()) AutentificarCliente();

                var url = string.Format(Settings.Default.ObtenerConsumosFueraTurnoCabezal, cabezal, fechaInicio, fechaFin);
                var request = CrearPeticion(HttpMethod.Get, url);

                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await LeerYDeserializarAsync<Result<List<ConsumosFueraTurno>>>(response);
                }
            }
            catch (Exception ex)
            {
                return new Result<List<ConsumosFueraTurno>> { StatusCode = -1, Data = new List<ConsumosFueraTurno>(), ResponseMessage = ex.Message };
            }
        }

        public async Task<Result<int?>> ModificarLecturaContador(string contador, int lectura)
        {
            try
            {
                if (!ClienteAutentificado()) AutentificarCliente();

                var url = string.Format(Settings.Default.ModificarLecturaContador, contador, lectura);
                var request = CrearPeticion(HttpMethod.Get, url);

                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await LeerYDeserializarAsync<Result<int?>>(response);
                }
            }
            catch (Exception ex)
            {
                return new Result<int?> { StatusCode = -1, Data = -1, ResponseMessage = ex.Message };
            }
        }

        public async Task<Result<List<RespuestaComprobarProgramacionISRLNew>>> ComprobarEstadoProgramacionContadores(List<string> contadores)
        {
            try
            {
                if (!ClienteAutentificado()) AutentificarCliente();

                var request = CrearPeticion(HttpMethod.Post, Settings.Default.ComprobarEstadoProgramacionContadores);
                request.Content = new StringContent(JsonConvert.SerializeObject(contadores), Encoding.UTF8, "application/json");

                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    var comprobaciones = await LeerYDeserializarAsync<Result<List<RespuestaComprobarProgramacionISRLNew>>>(response);

                    return new Result<List<RespuestaComprobarProgramacionISRLNew>>
                    {
                        StatusCode = 1,
                        Data = comprobaciones.Data.Select(c => new RespuestaComprobarProgramacionISRLNew()
                        {
                            codigo = c.codigo,
                            programada = c.programada
                        }).ToList(),
                        ResponseMessage = "",
                        RawResponse = ""
                    };
                }
            }
            catch (Exception ex)
            {
                return new Result<List<RespuestaComprobarProgramacionISRLNew>> { StatusCode = -1, Data = new List<RespuestaComprobarProgramacionISRLNew>(), ResponseMessage = ex.Message };
            }
        }

        private bool ClienteAutentificado()
        {
            if ((this.token != null) && (this.fechaToken.AddSeconds(this.token.expires_in) > DateTime.Now))
                return true;

            return false;
        }

        private void AutentificarCliente()
        {
            var requestGetToken = new OAuthRequest()
            {
                ServerURL = Settings.Default.ServerURL,
                GrantType = GrantTypeOptions.client_credentials,
                Scopes = new List<string>() { "ARJ_WebAPI" },
                ClientId = this.ClientId,
                ClientSecret = this.ClientSecret,
                EnableRefresh = false
            };

            this.token = OAuthConnector.GetToken(requestGetToken);
            this.fechaToken = DateTime.Now;
        }

        private static decimal calcularConsumo(decimal volumen, decimal superficie, double minutos)
        {
            if ((superficie == 0) || (minutos == 0)) return 0;
            return volumen / superficie / (decimal)(minutos / 60);
        }

        public async Task<Result<int>> Login(string usuario, string password)
        {
            try
            {
                if (!ClienteAutentificado()) AutentificarCliente();

                // Nota: Usamos GET porque así estaba definido en el request original, aunque enviemos credenciales.
                // Lo correcto arquitectónicamente sería un POST, pero mantenemos tu firma
                var request = CrearPeticion(HttpMethod.Get, Settings.Default.Login);

                // Mantenemos la serialización por si la API espera algo (aunque en un GET el content suele ignorarse)
                CredencialesLogin credenciales = new CredencialesLogin { usuario = usuario, password = password };
                request.Content = new StringContent(JsonConvert.SerializeObject(credenciales), Encoding.UTF8, "application/json");

                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();

                    // Como tu código original no devolvía el JSON mapeado, sino que dejaba el try vacío:
                    return new Result<int> { StatusCode = 1, Data = 1, ResponseMessage = "OK" };
                }
            }
            catch (Exception ex)
            {
                return new Result<int> { StatusCode = -1, Data = 1, ResponseMessage = ex.Message };
            }
        }

        public async Task<Result<DatosRegante>> Datos()
        {
            try
            {
                if (!ClienteAutentificado()) AutentificarCliente();

                var request = CrearPeticion(HttpMethod.Get, Settings.Default.Datos);

                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await LeerYDeserializarAsync<Result<DatosRegante>>(response);
                }
            }
            catch (Exception ex)
            {
                return new Result<DatosRegante> { StatusCode = -1, Data = new DatosRegante(), ResponseMessage = ex.Message };
            }
        }


        public async Task<List<VolumenCabezalResponse>> ObtenerVolumenCabezales()
        {
            try
            {
                // 1. Verificación de autenticación
                if (!ClienteAutentificado())
                {
                    AutentificarCliente();
                }

                // 2. Obtención de la URL de configuración
                var url = Settings.Default.ObtenerVolumenCabezales;

                // 3. Sintaxis clásica using para el request (compatible con C# 7.3 y anteriores)
                using (var request = CrearPeticion(HttpMethod.Get, url))
                {
                    // 4. Sintaxis clásica using para el response
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                    {
                        // Lanza excepción si el código de estado no es de éxito (200-299)
                        response.EnsureSuccessStatusCode();

                        // 5. Deserialización usando el Root Object que contiene la propiedad "cabezales"
                        var volumenWraper = await LeerYDeserializarAsync<RespuestaApi<CabezalesRootResponse>>(response);

                        // 6. Navegación segura con null condicionales para evitar NullReferenceException
                        return volumenWraper?.Data?.Cabezales ?? new List<VolumenCabezalResponse>();
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                // Captura específica para errores HTTP (ej. 404, 500, o problemas de red)
                logger.Error(httpEx, "Error de red o de estado HTTP al Obtener Volumen de Cabezales.");
                return new List<VolumenCabezalResponse>();
            }
            catch (Exception e)
            {
                // Captura general para otros errores (ej. fallo en la deserialización del JSON)
                logger.Error(e, "Error interno inesperado al Obtener Volumen de Cabezales.");
                return new List<VolumenCabezalResponse>();
            }
        }

    }

    public class DataToPost
    {
        public int IdTraza { get; set; }
        public List<TurnoStandard> Peticion { get; set; }

        public DataToPost(int idTraza, List<TurnoStandard> peticion)
        {
            this.IdTraza = idTraza;
            this.Peticion = peticion;
        }

        public DataToPost()
        {
            this.IdTraza = 0;
            this.Peticion = new List<TurnoStandard>();
        }
    }
}