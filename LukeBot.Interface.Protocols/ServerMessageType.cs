

namespace LukeBot.Interface.Protocols
{
    public enum ServerMessageType
    {
        None = 0,
        Login,
        Command,
        Notify,
        Query,
        PasswordChange,
        Logout,
        LoginResponse,
        CommandResponse,
        QueryResponse,
        PasswordChangeResponse,
    }
}
