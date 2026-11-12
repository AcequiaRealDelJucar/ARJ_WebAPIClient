using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARJ_WebApiClient.OAuth
{

    public class OAuthRequest
    {
        public string ServerURL { get; set; }

        public GrantTypeOptions? GrantType { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public List<string> Scopes { get; set; } = new List<string>();

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string Token { get; set; }

        public TokenTypeHintOptions? TokenTypeHint { get; set; }

        public string RefreshToken { get; set; }

        public bool EnableRefresh { get; set; } = false;

        
        public string getUrlEncodedParameters()
        {
            StringBuilder sb = new StringBuilder();
            List<string> urlParams = new List<string>();
            
            if (GrantType.HasValue) urlParams.Add("grant_type=" + GrantType.Value.ToString());
            if (TokenTypeHint.HasValue) urlParams.Add("token_type_hint=" + TokenTypeHint.Value.ToString());

            if (!string.IsNullOrWhiteSpace(Username)) urlParams.Add("username=" + Username);
            if (!string.IsNullOrWhiteSpace(Password)) urlParams.Add("password=" + Password);
            if (!string.IsNullOrWhiteSpace(ClientId)) urlParams.Add("client_id=" + ClientId);
            if (!string.IsNullOrWhiteSpace(ClientSecret)) urlParams.Add("client_secret=" + ClientSecret);
            if (!string.IsNullOrWhiteSpace(RefreshToken)) urlParams.Add("refresh_token=" + RefreshToken);
            if (!string.IsNullOrWhiteSpace(Token)) urlParams.Add("token=" + Token);

            if (Scopes.Count() > 0)
            {
                string scopesStr = string.Join("%20", Scopes);
                if (EnableRefresh) scopesStr += "%20offline_access";
                urlParams.Add("scope=" + scopesStr);
            }

            return string.Join("&", urlParams);
        }
    }


    public enum GrantTypeOptions
    {
        authorization_code, client_credentials, password, refresh_token
    }


    public enum TokenTypeHintOptions
    {
        access_token, refresh_token
    }

}
