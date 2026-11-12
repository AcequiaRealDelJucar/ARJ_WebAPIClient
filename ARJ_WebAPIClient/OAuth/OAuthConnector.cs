using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ARJ_WebApiClient.OAuth
{

    public static class OAuthConnector
    {
        private const string GetTokenUrlSegment = "auth/connect/token";
        private const string ValidateTokenUrlSegment = "auth/connect/accesstokenvalidation";
        private const string RevocateTokenUrlSegment = "auth/connect/revocation";


        public static OAuthResponse GetToken(OAuthRequest r)
        {
            var request = ConfigureRequest(r, GetTokenUrlSegment);
            return Execute<OAuthResponse>(request, r.ServerURL);
        }
        

        public static void RevocateToken(OAuthRequest r)
        {
            var request = ConfigureRequest(r, RevocateTokenUrlSegment);
            Execute<object>(request, r.ServerURL);
        }


        public static TokenInfo ValidateToken(OAuthRequest r)
        {
            var request = ConfigureRequest(r, ValidateTokenUrlSegment);
            return Execute<TokenInfo>(request, r.ServerURL);
        }


        public static TokenInfo ValidateToken(string token, string baseUrl)
        {
            return ValidateToken(new OAuthRequest { Token = token, ServerURL = baseUrl });
        }
        

        private static RestRequest ConfigureRequest(OAuthRequest r, string resourceUrl)
        {
            var request = new RestRequest(Method.POST);

            request.Resource = resourceUrl;
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddHeader("cache-control", "no-cache");
            request.AddParameter("application/x-www-form-urlencoded", r.getUrlEncodedParameters(), ParameterType.RequestBody);

            return request;
        }


        private static T Execute<T>(RestRequest request, string baseUrl) where T : new()
        {
            var client = new RestClient();
            client.BaseUrl = new Uri(baseUrl);

            var response = client.Execute<T>(request);

            if (response.ErrorException != null)
            {
                const string message = 
                    "Se ha producido un error en la llamada. Compruebe la excepción interna para obtener más detalles.";
                var appEx = new ApplicationException(message, response.ErrorException);
                throw appEx;
            }

            return response.Data;
        }

    }

}
