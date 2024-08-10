using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using LukeBot.Logging;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;


namespace LukeBot.API
{
    internal class Request
    {
        private const int RESPONSE_WAIT_TIMEOUT = 30 * 1000; // ms

        private static HttpRequestMessage FormRequestMessage(HttpMethod method, string uri, Token token, Dictionary<string, string> uriQuery, RequestContent content)
        {
            UriBuilder builder = new UriBuilder(uri);
            if (uriQuery != null && uriQuery.Count > 0)
            {
                builder.Query += string.Join("&", uriQuery.Select(x => x.Key + "=" + x.Value).ToArray());
            }

            HttpRequestMessage request = new HttpRequestMessage(method, builder.ToString());

            if (content != null && content.Type != RequestContentType.None)
                request.Content = content.ToHttpContent();

            if (token != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Get());
                request.Headers.Add("Client-Id", token.ClientID);
            }
            else
            {
                // TEST PATH - this should mostly be used in tests
                // Added because Twitch CLI requires it to properly receive API requests
                // TODO when refactoring this area, might be a good idea to help it in a different way...
                request.Headers.Add("Client-Id", "0lukebot1test2client3id4");
            }

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return request;
        }

        private static HttpResponseMessage Send(HttpMethod method, string uri, Token token, Dictionary<string, string> uriQuery, RequestContent content)
        {
            if (token != null)
                token.EnsureValid();

            HttpRequestMessage message = FormRequestMessage(method, uri, token, uriQuery, content);

            HttpClient client = new HttpClient();
            Task<HttpResponseMessage> responseTask = client.SendAsync(message);
            responseTask.Wait(RESPONSE_WAIT_TIMEOUT);
            HttpResponseMessage response = responseTask.Result;

            // refresh token and retry if we got unauthorized
            if (response.StatusCode == HttpStatusCode.Unauthorized && token != null)
            {
                token.Refresh();

                // recreate HttpClient and form a new message to avoid re-send protection triggering
                client = new HttpClient();
                message = FormRequestMessage(method, uri, token, uriQuery, content);

                responseTask = client.SendAsync(message);
                responseTask.Wait(RESPONSE_WAIT_TIMEOUT);
                response = responseTask.Result;
            }

            return response;
        }

        private static TResp RequestCommon<TResp>(HttpMethod method,
                                                  string uri,
                                                  Token token = null,
                                                  Dictionary<string, string> uriQuery = null,
                                                  RequestContent content = null)
                                                  where TResp: Response, new()
        {
            HttpResponseMessage response = Send(method, uri, token, uriQuery, content);
            if (!response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent)
            {
                TResp r = new TResp();
                r.Fill(response);
                return r;
            }

            Task<string> retContentStrTask = response.Content.ReadAsStringAsync();
            retContentStrTask.Wait();
            //Logger.Log().Secure("{0}: {1}", method.ToString(), retContentStrTask.Result);
            TResp ret = JsonConvert.DeserializeObject<TResp>(retContentStrTask.Result);
            ret.Fill(response);
            return ret;
        }


        /***************************
         * Public APIs
         ***************************/

        /**
         * Call a Get Http Request. Returns a serialized string with response data.
         *
         * @p uri URI to submit a request to
         * @p token Authorization token acquired via AuthManager
         * @p uriQuery Additional queries to be added to @p URI
         * @p content Additional content added to the request (see RequestContent)
         */
        public static string Get(string uri,
                                 Token token = null,
                                 Dictionary<string, string> uriQuery = null,
                                 RequestContent content = null)
        {
            HttpResponseMessage response = Send(HttpMethod.Get, uri, token, uriQuery, content);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return string.Format("Request failed ({0}): {1}", response.StatusCode, response.ReasonPhrase);
            }

            Task<string> retContentStrTask = response.Content.ReadAsStringAsync();
            retContentStrTask.Wait();
            //Logger.Log().Secure("Get: {0}", retContentStrTask.Result);
            return retContentStrTask.Result;
        }

        /**
         * Call a Get Http Request. Returns a JSON object in form of Newtonsoft JObject
         *
         * @p uri URI to submit a request to
         * @p token Authorization token acquired via AuthManager
         * @p uriQuery Additional queries to be added to @p URI
         * @p content Additional content added to the request (see RequestContent)
         */
        public static ResponseJObject GetJObject(string uri,
                                                 Token token = null,
                                                 Dictionary<string, string> uriQuery = null,
                                                 RequestContent content = null)
        {
            HttpResponseMessage response = Send(HttpMethod.Get, uri, token, uriQuery, content);
            if (!response.IsSuccessStatusCode)
            {
                return new ResponseJObject(response);
            }

            Task<string> retContentStrTask = response.Content.ReadAsStringAsync();
            retContentStrTask.Wait();
            return new ResponseJObject(response, retContentStrTask.Result);
        }

        /**
         * Call a Get Http Request. Returns a JArray representing multiple JSON JObject-s
         *
         * @p uri URI to submit a request to
         * @p token Authorization token acquired via AuthManager
         * @p uriQuery Additional queries to be added to @p URI
         * @p content Additional content added to the request (see RequestContent)
         */
        public static ResponseJArray GetJArray(string uri,
                                               Token token = null,
                                               Dictionary<string, string> uriQuery = null,
                                               RequestContent content = null)
        {
            HttpResponseMessage response = Send(HttpMethod.Get, uri, token, uriQuery, content);
            if (!response.IsSuccessStatusCode)
            {
                return new ResponseJArray(response);
            }

            Task<string> retContentStrTask = response.Content.ReadAsStringAsync();
            retContentStrTask.Wait();
            return new ResponseJArray(response, retContentStrTask.Result);
        }

        /**
         * Call a Get Http request and, if successful, deserialize the response to generic type TResp.
         *
         * TResp must be derived from (or be) a Response class.
         *
         * If the call fails, return type will be a base Response class containing HTTP error code and
         * received HttpResponseMessage for further handling. Remember to first check if request
         * was successful before accessing TResp fields from derived class.
         *
         * @p uri URI to submit a request to
         * @p token Authorization token acquired via AuthManager
         * @p uriQuery Additional queries to be added to @p URI
         * @p content Additional content added to the request (see RequestContent)
         */
        public static TResp Get<TResp>(string uri,
                                       Token token = null,
                                       Dictionary<string, string> uriQuery = null,
                                       RequestContent content = null)
                                       where TResp: Response, new()
        {
            return RequestCommon<TResp>(HttpMethod.Get, uri, token, uriQuery, content);
        }

        /**
         * Call a Post Http request and, if successful, deserialize the response to generic type TResp.
         *
         * TResp must be derived from (or be) a Response class.
         *
         * If the call fails, return type will be a base Response class containing HTTP error code and
         * received HttpResponseMessage for further handling. Remember to first check if request
         * was successful before accessing TResp fields from derived class.
         *
         * @p uri URI to submit a request to
         * @p token Authorization token acquired via AuthManager
         * @p uriQuery Additional queries to be added to @p URI
         * @p content Additional content added to the request (see RequestContent)
         */
        public static TResp Post<TResp>(string uri,
                                        Token token = null,
                                        Dictionary<string, string> uriQuery = null,
                                        RequestContent content = null)
                                        where TResp: Response, new()
        {
            return RequestCommon<TResp>(HttpMethod.Post, uri, token, uriQuery, content);
        }

        /**
         * Call a Delete Http request and, if successful, deserialize the response to generic type TResp.
         *
         * TResp must be derived from (or be) a Response class.
         *
         * If the call fails, return type will be a base Response class containing HTTP error code and
         * received HttpResponseMessage for further handling. Remember to first check if request
         * was successful before accessing TResp fields from derived class.
         *
         * @p uri URI to submit a request to
         * @p token Authorization token acquired via AuthManager
         * @p uriQuery Additional queries to be added to @p URI
         * @p content Additional content added to the request (see RequestContent)
         */
        public static TResp Delete<TResp>(string uri,
                                          Token token = null,
                                          Dictionary<string, string> uriQuery = null,
                                          RequestContent content = null)
                                          where TResp: Response, new()
        {
            return RequestCommon<TResp>(HttpMethod.Delete, uri, token, uriQuery, content);
        }
    }
}
