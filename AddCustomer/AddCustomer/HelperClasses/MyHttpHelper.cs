using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;

namespace ABC_Retails_Functions.HelperClasses
{
    public static class MyHttpHelper
    {
        // For returning JSON
        public static async Task<HttpResponseData> Json<T>(
            HttpRequestData req, HttpStatusCode status, T body)
        {
            var res = req.CreateResponse(status); // make a response with status (200, 201, 400, etc.)
            await res.WriteAsJsonAsync(body);     // write the body as JSON
            return res;                           // return the response
        }

        // For returning plain text
        public static async Task<HttpResponseData> Text(
            HttpRequestData req, HttpStatusCode status, string message)
        {
            var res = req.CreateResponse(status);  // make a response with given status
            await res.WriteStringAsync(message);   // write plain text instead of JSON
            return res;                            // return the response
        }
    }
}

