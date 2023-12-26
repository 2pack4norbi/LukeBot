using System;


namespace LukeBot.Interface.Protocols
{
    public class SessionData
    {
        public string Cookie { get; private set; }

        internal SessionData()
        {
            Cookie = "";
        }

        public SessionData(string cookie)
        {
            Cookie = cookie;
        }
    }

    public class ServerMessage
    {
        public ServerMessageType Type { get; private set; }
        public SessionData Session { get; private set; }
        public string MsgID { get; private set; }

        protected ServerMessage(ServerMessageType t, SessionData session)
        {
            Type = t;
            Session = session;
            MsgID = Guid.NewGuid().ToString();
        }

        protected ServerMessage(ServerMessageType t, SessionData session, string id)
        {
            Type = t;
            Session = session;
            MsgID = id;
        }
    }

    // Login message - sent from client to server to start the connection
    // and access a LukeBot account. Contains username to log into and
    // SHA-512 hashed password encoded in base64.
    //
    // Because this message is used to start the session, it has no Session
    // data attached to it.
    public class LoginServerMessage: ServerMessage
    {
        public string User { get; private set; }
        public string PasswordHashBase64 { get; private set; }

        public LoginServerMessage(string u, byte[] p)
            : base(ServerMessageType.Login, new SessionData())
        {

        }
    }

    // Command message - sent exclusively from client to server. Emits a
    // CLI LukeBot command.
    public class CommandServerMessage: ServerMessage
    {
        public string Command { get; private set; }

        public CommandServerMessage(SessionData session, string cmd)
            : base(ServerMessageType.Command, session)
        {
            Command = cmd;
        }
    }

    // Notify message - sent when calling Interface's Message() call.
    // Exclusively from server to client to inform about <<something>>
    // with no further interaction needed
    public class NotifyServerMessage: ServerMessage
    {
        public string Message { get; private set; }

        public NotifyServerMessage(SessionData session, string msg)
            : base(ServerMessageType.Notify, session)
        {
            Message = msg;
        }
    }

    // Query message - sent from server to client in order to ask for
    // something. Must be used to form a response to be sent back to
    // the server.
    public class QueryServerMessage: ServerMessage
    {
        public string Query { get; private set; }
        public bool IsYesNo { get; private set; }

        public QueryServerMessage(SessionData session, string q, bool yn)
            : base(ServerMessageType.Query, session)
        {
            Query = q;
            IsYesNo = yn;
        }
    }

    // Login response message - sent from server to client to inform about
    // login process. If successful, Session contains session data needed
    // for further communication with the server, Success is true and Error
    // simply states "OK".
    //
    // If login failed, server will inform about the reason via Error field
    // and close the connection.
    public class LoginResponseServerMessage: ServerMessage
    {
        public string User { get; private set; }
        public bool Success { get; private set; }
        public string Error { get; private set; }

        // successful login response - attaches session data
        public LoginResponseServerMessage(LoginServerMessage login, SessionData session)
            : base(ServerMessageType.LoginResponse, session, login.MsgID)
        {
            User = login.User;
            Success = true;
            Error = "OK";
        }

        // failed login response - attaches error reason
        public LoginResponseServerMessage(LoginServerMessage login, string error)
            : base(ServerMessageType.LoginResponse, new SessionData(), login.MsgID)
        {
            User = login.User;
            Success = false;
            Error = error;
        }
    }

    // Command response message - sent from server to client to inform about command
    // status.
    //
    // Note that this message will be sent back only after the command fully completes
    // execution. In parallel, server can sent Notify or Query messages which were
    // caused by the command being executed.
    //
    // Once the Command is fully executed successfully, this response is sent and marks the
    // end of command execution.
    public class CommandResponseServerMessage: ServerMessage
    {
        public ServerCommandStatus Status { get; private set; }

        public CommandResponseServerMessage(CommandServerMessage cmd, ServerCommandStatus status)
            : base(ServerMessageType.Query, cmd.Session, cmd.MsgID)
        {
            Status = status;
        }
    }

    // Query response message - sent from client to server in order to ask for
    // something. Must be used to form a response to be sent back to
    // the server.
    public class QueryResponseServerMessage: ServerMessage
    {
        public string Response { get; private set; }
        public bool IsYesNo { get; private set; }

        public QueryResponseServerMessage(QueryServerMessage q, string r)
            : base(ServerMessageType.Query, q.Session, q.MsgID)
        {
            IsYesNo = q.IsYesNo;
            Response = r;
        }
    }
}
