using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace gamevault.Helper
{
    internal class WebExceptionHelper
    {
        internal static string GetServerMessage(WebException ex)
        {
            string errMessage = string.Empty;
            try
            {
                var resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                JsonObject obj = JsonNode.Parse(resp).AsObject();
                errMessage = obj["message"].ToString().Replace("\n", " ").Replace("\r", "");
            }
            catch { }
            return errMessage;
        }
        internal static string GetServerStatusCode(WebException ex)
        {
            string errMessage = string.Empty;
            try
            {
                if (ex.Response == null)
                    return string.Empty;

                var resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                JsonObject obj = JsonNode.Parse(resp).AsObject();
                errMessage = obj["statusCode"].ToString().Replace("\n", " ").Replace("\r", "");
            }
            catch { }
            return errMessage;
        }
    }
}
