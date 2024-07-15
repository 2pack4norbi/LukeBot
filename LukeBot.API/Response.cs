using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


// TODO this probably should be moved to separate DLL like LukeBot.Network
namespace LukeBot.API
{
    public class ResponseData
    {
        public HttpResponseMessage httpMessage;
        public string error;
        public string message;

        internal ResponseData(HttpResponseMessage msg)
        {
            httpMessage = msg;
            error = "OK";
            message = "";

            // try and read the content and see if we can parse it
            // if it is a JSON response, it might have error/message fields available
            if (!msg.IsSuccessStatusCode)
            {
                if (msg.Content != null && msg.Content.Headers != null &&
                    msg.Content.Headers.ContentType.MediaType == "application/json")
                {
                    Task<string> respStringTask = msg.Content.ReadAsStringAsync();
                    respStringTask.Wait();

                    JObject errObj = JObject.Parse(respStringTask.Result);
                    if (errObj.ContainsKey("error"))
                        error = errObj["error"].ToString();

                    if (errObj.ContainsKey("message"))
                        message = (string)errObj["message"];
                }
                else
                {
                    error = msg.StatusCode.ToString();
                    message = msg.ReasonPhrase;
                }
            }
        }
    };

    public class Response
    {
        public HttpStatusCode code { get; set; }
        public ResponseData responseData { get; set; }
        public bool IsSuccess
        {
            get
            {
                return responseData != null && responseData.httpMessage.IsSuccessStatusCode;
            }
        }

        public Response()
        {
            code = HttpStatusCode.OK;
        }

        public Response(HttpResponseMessage msg)
        {
            Fill(msg);
        }

        public void Fill(HttpResponseMessage msg)
        {
            code = msg.StatusCode;
            responseData = new ResponseData(msg);
        }
    }

    public class ResponseJObject: Response
    {
        public JObject obj { get; private set; }

        public ResponseJObject(HttpResponseMessage msg)
            : base(msg)
        {
            obj = null;
        }

        public ResponseJObject(HttpResponseMessage msg, string jsonStr)
            : base(msg)
        {
            obj = JObject.Parse(jsonStr);
        }

        public override string ToString()
        {
            string r = "code = " + code.ToString();
            if (code == HttpStatusCode.OK)
            {
                r += ", data = " + obj.ToString();
            }
            return r;
        }
    }

    public class ResponseJArray: Response
    {
        public JArray array { get; private set; }

        public ResponseJArray(HttpResponseMessage msg)
            : base(msg)
        {
            array = null;
        }

        public ResponseJArray(HttpResponseMessage msg, string jsonStr)
            : base(msg)
        {
            array = JArray.Parse(jsonStr);
        }

        public override string ToString()
        {
            string r = "code = " + code.ToString();
            if (code == HttpStatusCode.OK)
            {
                r += ", data = " + array.ToString();
            }
            return r;
        }
    }
}
