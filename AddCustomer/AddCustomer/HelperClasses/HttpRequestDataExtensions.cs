using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;



namespace ABC_Retails_Functions.HelperClasses

{
    public static class HttpRequestDataExtensions
    {
        public static async Task<Dictionary<string, string>> ReadFormAsync(this HttpRequestData req)
        {
            var dict = new Dictionary<string, string>();
            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();

            foreach (var kv in body.Split('&'))
            {
                var parts = kv.Split('=');
                if (parts.Length == 2)
                {
                    dict[System.Net.WebUtility.UrlDecode(parts[0])] = System.Net.WebUtility.UrlDecode(parts[1]);
                }
            }
            return dict;
        }
    }
}