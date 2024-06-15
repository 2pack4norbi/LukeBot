using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LukeBot.Logging;
using LukeBot.Interface.Protocols;


namespace LukeBot.Interface.Protocols
{
    public abstract class ServerMessageDeserializerBase: JsonConverter
    {
        public override bool CanWrite { get { return false; } }

        public override bool CanConvert(Type objectType)
        {
            return (objectType.Namespace != null) && (objectType.Namespace.Equals("LukeBot.Interface.Protocols"));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("Json Writes are not supported by EventSub Deserializer");
        }
    }


    public class ServerMessageDeserializer: ServerMessageDeserializerBase
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);

            if (!obj.TryGetValue("Type", out JToken typeToken))
            {
                Logger.Log().Error("Failed to find type info");
                return null;
            }

            ServerMessage msg = null;

            // parse base class to fetch the type
            ServerMessageType type = typeToken.ToObject<ServerMessageType>();
            switch (type)
            {
            case ServerMessageType.None: break;
            case ServerMessageType.Login: msg = obj.ToObject<LoginServerMessage>(); break;
            case ServerMessageType.Ping: msg = obj.ToObject<PingServerMessage>(); break;
            case ServerMessageType.Command: msg = obj.ToObject<CommandServerMessage>(); break;
            case ServerMessageType.Notify: msg = obj.ToObject<NotifyServerMessage>(); break;
            case ServerMessageType.Query: msg = obj.ToObject<QueryServerMessage>(); break;
            case ServerMessageType.PasswordChange: msg = obj.ToObject<PasswordChangeServerMessage>(); break;
            case ServerMessageType.Logout: msg = obj.ToObject<LogoutServerMessage>(); break;
            case ServerMessageType.LoginResponse: msg = obj.ToObject<LoginResponseServerMessage>(); break;
            case ServerMessageType.PingResponse: msg = obj.ToObject<PingResponseServerMessage>(); break;
            case ServerMessageType.CommandResponse: msg = obj.ToObject<CommandResponseServerMessage>(); break;
            case ServerMessageType.QueryResponse: msg = obj.ToObject<QueryResponseServerMessage>(); break;
            case ServerMessageType.PasswordChangeResponse: msg = obj.ToObject<PasswordChangeResponseServerMessage>(); break;
            }

            return msg;
        }
    }
}