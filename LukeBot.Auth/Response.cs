using System.Net;
using Newtonsoft.Json.Linq;


// TODO this probably should be moved to separate DLL like LukeBot.Network
namespace LukeBot.Auth
{
    public class Response
    {
        public HttpStatusCode code { get; set; }

        public Response()
        {
            code = HttpStatusCode.OK;
        }
    }

    public class ResponseJObject
    {
        public HttpStatusCode code { get; private set; }
        public JObject obj { get; private set; }

        public ResponseJObject(HttpStatusCode c)
        {
            code = c;
            obj = null;
        }

        public ResponseJObject(HttpStatusCode c, string jsonStr)
        {
            code = c;
            obj = JObject.Parse(jsonStr);
        }
    }
}
