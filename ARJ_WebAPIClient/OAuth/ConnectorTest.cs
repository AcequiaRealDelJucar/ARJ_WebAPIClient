using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ARJ_WebApiClient.OAuth
{
    class ConnectorTest
    {

        private static void Main(string[] args)
        {

            // Get token
            var requestGetToken = new OAuthRequest() {
                ServerURL = "http://192.168.25.230:83",
                GrantType = GrantTypeOptions.password,
                Username = "dario",
                Password = "ceteck",
                Scopes = new List<string>() { "ARJ_WebAPI" },
                ClientId = "ARJ_AndroidApp",
                ClientSecret = "4RJ_4ndr01d4pp",
                EnableRefresh = true
            };
            var response = OAuthConnector.GetToken(requestGetToken);


            // Refresh token
            //var requestRefereshToken = new OAuthRequest()
            //{
            //    ServerURL = "http://192.168.25.230:83",
            //    GrantType = GrantTypeOptions.refresh_token,
            //    ClientId = "ARJ_AndroidApp",
            //    ClientSecret = "4RJ_4ndr01d4pp",
            //    RefreshToken = response.refresh_token
            //};
            //var response2 = OAuthConnector.GetToken(requestRefereshToken);


            // Validate token
            var requestValidateToken = new OAuthRequest()
            {
                ServerURL = "http://192.168.25.230:83",
                Token = response.access_token
            };
            var tokeninfo = OAuthConnector.ValidateToken(requestValidateToken);


            // Revocate token
            var requestRevocateToken = new OAuthRequest()
            {
                ServerURL = "http://192.168.25.230:83",
                GrantType = GrantTypeOptions.password,
                Username = "dario",
                Password = "ceteck",
                Scopes = new List<string>() { "ARJ_WebAPI" },
                ClientId = "ARJ_AndroidApp",
                ClientSecret = "4RJ_4ndr01d4pp",
                Token = response.access_token,
                TokenTypeHint = TokenTypeHintOptions.access_token
            };
            OAuthConnector.RevocateToken(requestRevocateToken);
        }

    }
}
