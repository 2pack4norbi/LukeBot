

namespace LukeBot.Interface.Protocols
{
    public enum ServerMessageType
    {
        None = 0,
        Login,
        Ping,
        Command,
        Notify,
        Query,
        PasswordChange,
        Logout,

        NoneResponse = 0x8000,
        LoginResponse,
        PingResponse,
        CommandResponse,
        QueryResponse,
        PasswordChangeResponse,
    }
}
