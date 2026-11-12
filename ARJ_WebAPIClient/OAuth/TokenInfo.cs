using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ARJ_WebApiClient.OAuth
{

    public class TokenInfo
    {

        public string aud { get; set; }

        public string iss { get; set; }

        public string nbf { get; set; }

        public string exp { get; set; }

        public string client_id { get; set; }

        public List<string> scope { get; set; }

        public string sub { get; set; }

        public long auth_time { get; set; }

        public string idp { get; set; }

        public string amr { get; set; }

    }

}
