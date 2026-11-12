using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARJ_WebAPIClient.Models
{
    public class Result<T>
    {

        public int StatusCode { get; set; }

        public string ResponseMessage { get; set; }

        public T Data { get; set; }

        public object RawResponse { get; set; }

    }
}
