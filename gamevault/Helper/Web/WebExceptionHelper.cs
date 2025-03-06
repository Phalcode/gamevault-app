using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace gamevault.Helper
{
    internal class WebExceptionHelper
    {
        private static string GetServerMessage(WebException ex)
        {
            string errMessage = string.Empty;
            try
            {
                var resp = new StreamReader(ex.Response?.GetResponseStream()).ReadToEnd();
                JsonObject obj = JsonNode.Parse(resp).AsObject();
                errMessage = obj["message"].ToString().Replace("\n", " ").Replace("\r", "");
            }
            catch { }
            return errMessage;
        }
        /// <summary>
        /// Tries to get the response message of the server. Else returns exception message
        /// </summary>
        internal static string TryGetServerMessage(Exception ex)
        {
            if (ex is WebException webEx)
            {
                string msg = GetServerMessage(webEx);
                return string.IsNullOrEmpty(msg) ? ex.Message : $"Server responded: {msg}";
            }
            return ex.Message;
        }
        internal static string GetServerStatusCode(Exception ex)
        {
            if (ex is WebException webex)
            {

                string errMessage = string.Empty;
                try
                {
                    if (webex.Response == null)
                        return string.Empty;

                    var resp = new StreamReader(webex.Response.GetResponseStream()).ReadToEnd();
                    JsonObject obj = JsonNode.Parse(resp).AsObject();
                    errMessage = obj["statusCode"].ToString().Replace("\n", " ").Replace("\r", "");
                }
                catch { }
                return errMessage;
            }
            else if (ex is HttpRequestException httpEx && httpEx.StatusCode.HasValue)
            {
                return ((int)httpEx.StatusCode.Value).ToString();
            }
            return string.Empty;
        }
    }
}
