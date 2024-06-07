using System;


namespace LukeBot.Interface.Protocols
{
    public class SessionData
    {
        public string Cookie { get; set; }

        internal SessionData()
        {
            Cookie = "";
        }

        public SessionData(string cookie)
        {
            Cookie = cookie;
        }

        public override string ToString()
        {
            return "Cookie: " + Cookie;
        }
    }

    public class ServerMessage
    {
        public ServerMessageType Type { get; private set; }
        public SessionData Session { get; set; }
        public string MsgID { get; set; }

        public ServerMessage()
        {
            Type = ServerMessageType.None;
            Session = null;
            MsgID = null;
        }

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

        public override string ToString()
        {
            return "Type: " + Type.ToString() + "; Session: { " + (Session != null ? Session.ToString() : "null") + " }; ID: " + MsgID;
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
        public string User { get; set; }
        public string PasswordHashBase64 { get; set; }

        public LoginServerMessage()
            : base(ServerMessageType.Login, null, "")
        {
            User = "";
            PasswordHashBase64 = "";
        }

        public LoginServerMessage(string u, byte[] p)
            : base(ServerMessageType.Login, null)
        {
            User = u;
            PasswordHashBase64 = Convert.ToBase64String(p);
        }

        public override string ToString()
        {
            return base.ToString() + "; User: " + User + "; PwdHashB64: " + PasswordHashBase64;
        }
    }

    // Command message - sent exclusively from client to server. Emits a
    // CLI LukeBot command.
    public class CommandServerMessage: ServerMessage
    {
        public string Command { get; set; }

        public CommandServerMessage()
            : base(ServerMessageType.Command, null, "")
        {
            Command = "";
        }

        public CommandServerMessage(SessionData session, string cmd)
            : base(ServerMessageType.Command, session)
        {
            Command = cmd;
        }

        public override string ToString()
        {
            return base.ToString() + "; Command: " + Command;
        }
    }

    // Notify message - sent when calling Interface's Message() call.
    // Exclusively from server to client to inform about <<something>>
    // with no further interaction needed
    public class NotifyServerMessage: ServerMessage
    {
        public string Message { get; set; }

        public NotifyServerMessage()
            : base(ServerMessageType.Notify, null, "")
        {
            Message = "";
        }

        public NotifyServerMessage(SessionData session, string msg)
            : base(ServerMessageType.Notify, session)
        {
            Message = msg;
        }

        public override string ToString()
        {
            return base.ToString() + "; Message: " + Message;
        }
    }

    // Query message - sent from server to client in order to ask for
    // something. Must be used to form a response to be sent back to
    // the server.
    public class QueryServerMessage: ServerMessage
    {
        public string Query { get; set; }
        public bool IsYesNo { get; set; }

        public QueryServerMessage()
            : base(ServerMessageType.Query, null, "")
        {
            Query = "";
            IsYesNo = false;
        }

        public QueryServerMessage(SessionData session, string q, bool yn)
            : base(ServerMessageType.Query, session)
        {
            Query = q;
            IsYesNo = yn;
        }

        public override string ToString()
        {
            return base.ToString() + "; Query: " + Query + "; YesNo: " + IsYesNo;
        }
    }

    public class PasswordChangeServerMessage: ServerMessage
    {
        public string CurrentPasswordB64 { get; set; }
        public string NewPasswordB64 { get; set; }

        public PasswordChangeServerMessage()
            : base(ServerMessageType.PasswordChange, null, "")
        {
            CurrentPasswordB64 = "";
            NewPasswordB64 = "";
        }

        public PasswordChangeServerMessage(SessionData session, string curPwdB64, string newPwdB64)
            : base(ServerMessageType.PasswordChange, session)
        {
            CurrentPasswordB64 = curPwdB64;
            NewPasswordB64 = newPwdB64;
        }

        public override string ToString()
        {
            return base.ToString() + "; Current: " + CurrentPasswordB64 + "; New: " + NewPasswordB64;
        }
    }

    public class LogoutServerMessage: ServerMessage
    {
        public LogoutServerMessage()
            : base(ServerMessageType.Logout, null, "")
        {
        }

        public LogoutServerMessage(SessionData session)
            : base(ServerMessageType.Logout, session)
        {
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
        public string User { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }

        public LoginResponseServerMessage()
            : base(ServerMessageType.LoginResponse, null, "")
        {
            User = "";
            Success = false;
            Error = "";
        }

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

        public override string ToString()
        {
            return base.ToString() + "; User: " + User + "; Success: " + Success + (Success ? "" : "; Error: " + Error);
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
        public ServerCommandStatus Status { get; set; }

        public CommandResponseServerMessage()
            : base(ServerMessageType.CommandResponse, null, "")
        {
            Status = ServerCommandStatus.UnknownCommand;
        }

        public CommandResponseServerMessage(CommandServerMessage cmd, ServerCommandStatus status)
            : base(ServerMessageType.Query, cmd.Session, cmd.MsgID)
        {
            Status = status;
        }

        public override string ToString()
        {
            return base.ToString() + "; Status: " + Status.ToString();
        }
    }

    // Query response message - sent from client to server in order to ask for
    // something. Must be used to form a response to be sent back to
    // the server.
    public class QueryResponseServerMessage: ServerMessage
    {
        public string Response { get; set; }
        public bool IsYesNo { get; set; }

        public QueryResponseServerMessage()
            : base(ServerMessageType.CommandResponse, null, "")
        {
            Response = "";
            IsYesNo = false;
        }

        public QueryResponseServerMessage(QueryServerMessage q, string r)
            : base(ServerMessageType.Query, q.Session, q.MsgID)
        {
            IsYesNo = q.IsYesNo;
            Response = r;
        }

        public override string ToString()
        {
            return base.ToString() + "; Response: " + Response + "; YesNo: " + IsYesNo;
        }
    }
}
