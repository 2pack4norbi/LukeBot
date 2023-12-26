

namespace LukeBot.Interface.Protocols
{
    public enum ServerMessageType
    {
        None = 0,
        Login,
        Command,
        Notify,
        Query,
        LoginResponse,
        CommandResponse,
        QueryResponse,
    }
}
