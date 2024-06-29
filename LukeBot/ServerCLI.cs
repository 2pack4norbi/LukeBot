using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using LukeBot.Logging;
using LukeBot.Interface.Protocols;
using Newtonsoft.Json;
using System.Timers;


namespace LukeBot
{
    internal class ServerCLI: CLIBase
    {
        private class ClientContext: CLIMessageProxy
        {
            public delegate void OnClientDoneDelegate(string cookie);

            private const int COOKIE_SIZE = 32;
            private const int PING_TIMER_THRESHOLD = 120 * 1000; // 2 minutes = 120 seconds in miliseconds

            public string mUsername = "";
            public string mCookie = ""; // full ID of this connection context
            public SessionData mSessionData = null;
            private UserPermissionLevel mPermissionLevel = UserPermissionLevel.None;
            private OnClientDoneDelegate mClientDoneDelegate = null;

            private string mCookieShorthand = "";
            private string mLogPreamble = "";
            private TcpClient mClient = null;
            private NetworkStream mStream = null;
            private byte[] mRecvBuffer = new byte[4096];
            private Thread mRecvThread = null;
            private bool mRecvThreadDone = false;
            private System.Timers.Timer mPingTimer = null;
            private string mCurrentPingChallenge = "";

            private Dictionary<string, Command> mCommands = new();

            private void OnTimer(object source, ElapsedEventArgs args)
            {
                PingServerMessage ping = new PingServerMessage(mSessionData);
                mCurrentPingChallenge = ping.Test;
                SendObject(ping);
            }

            private void LogClientContext(LogLevel level, string msg, params string[] args)
            {
                Logger.Log().Message(level, mLogPreamble + msg, args);
            }

            private bool ValidateMessage(ServerMessage msg)
            {
                return
                    msg != null && // message cannot be null
                    msg.Session != null && // message's Session section cannot be null either
                    msg.Session.Cookie == mSessionData.Cookie && // cookie must match the one we have
                    msg.Type != ServerMessageType.Login; // Login messages are not accepted at this point
            }

            public ClientContext(TcpClient client, OnClientDoneDelegate clientDoneDelegate)
            {
                mClient = client;
                mStream = mClient.GetStream();
                mStream.ReadTimeout = 125 * 1000; // 2 minutes
                mPermissionLevel = UserPermissionLevel.None;
                mClientDoneDelegate = clientDoneDelegate;

                RandomNumberGenerator rng = RandomNumberGenerator.Create();
                byte[] cookieBuffer = new byte[COOKIE_SIZE];
                rng.GetBytes(cookieBuffer);
                mCookie = Convert.ToHexString(cookieBuffer);
                mCookieShorthand = mCookie.Substring(0, 8);
                mLogPreamble = "ClientContext[" + mCookieShorthand + "]: ";

                mSessionData = new(mCookie);
                mRecvThread = new(ReceiveThreadMain);
                mRecvThread.Name = String.Format("ClientContext[{0}] Thread", mCookieShorthand);

                mPingTimer = new(120 * 1000);
                mPingTimer.Elapsed += OnTimer;
                mPingTimer.AutoReset = false;
            }

            public string Receive()
            {
                int read = 0;
                string msg = "";

                do
                {
                    read = mStream.Read(mRecvBuffer, 0, 4096);
                    msg += Encoding.UTF8.GetString(mRecvBuffer, 0, read);
                }
                while (read == 4096);

                return msg;
            }

            public T ReceiveObject<T>()
                where T: ServerMessage
            {
                return JsonConvert.DeserializeObject<T>(Receive(), new ServerMessageDeserializer());
            }

            public void Send(string msg)
            {
                if (mStream == null)
                    return;

                byte[] sendBuf = Encoding.UTF8.GetBytes(msg);
                mStream.Write(sendBuf, 0, sendBuf.Length);
                mStream.Flush();
            }

            public void SendObject<T>(T obj)
            {
                Send(JsonConvert.SerializeObject(obj));
            }

            private void ReceiveThreadMain()
            {
                LogClientContext(LogLevel.Info, "Connected");
                mPingTimer.Start();

                while (!mRecvThreadDone)
                {
                    try
                    {
                        ServerMessage msg = ReceiveObject<ServerMessage>();
                        LogClientContext(LogLevel.Info, "Mesage: {0}", msg.Type.ToString());
                        if (!ValidateMessage(msg))
                        {
                            // cut the connection, something was not correct
                            LogClientContext(LogLevel.Error, "Got invalid message - disconnecting");
                            mRecvThreadDone = true;
                            break;
                        }

                        switch (msg.Type)
                        {
                        case ServerMessageType.Logout:
                            LogClientContext(LogLevel.Info, "Logout message received - disconnecting");
                            mRecvThreadDone = true;
                            break;
                        case ServerMessageType.PingResponse:
                            if (mCurrentPingChallenge == "")
                            {
                                LogClientContext(LogLevel.Error, "Received ping response with no challenge in progress - dropping connection");
                                mRecvThreadDone = true;
                            }

                            LogClientContext(LogLevel.Debug, "Received ping response");
                            PingResponseServerMessage pingMsg = msg as PingResponseServerMessage;
                            if (pingMsg.Test != mCurrentPingChallenge)
                            {
                                LogClientContext(LogLevel.Error, "Ping challenge failed - disconnecting");
                                LogClientContext(LogLevel.Debug, "Ping challenge expected {0}, got {1}", mCurrentPingChallenge, pingMsg.Test);
                                mRecvThreadDone = true;
                            }

                            LogClientContext(LogLevel.Debug, "Ping challenge successful");
                            mCurrentPingChallenge = "";
                            mPingTimer.Start();
                            break;
                        case ServerMessageType.Command:
                        {
                            CommandServerMessage cmd = msg as CommandServerMessage;
                            string[] cmdTokens = cmd.Command.Split(' ');
                            if (mCommands.TryGetValue(cmdTokens[0], out Command c))
                            {
                                // TODO Command.Execute() should not return a string, but rather
                                // communicate its outcome and such via CLIMessageProxy
                                // Instead we could return something like a boolean value to
                                // check if we succeeded or not
                                string result = c.Execute(this, cmdTokens.Skip(1).ToArray());
                                CommandResponseServerMessage resp = new(cmd, ServerCommandStatus.Success, result);
                                SendObject<CommandResponseServerMessage>(resp);
                            }
                            else
                            {
                                CommandResponseServerMessage resp = new(cmd, ServerCommandStatus.UnknownCommand, "");
                                SendObject<CommandResponseServerMessage>(resp);
                            }
                            LogClientContext(LogLevel.Debug, "Processing Command message done");
                            break;
                        }
                        default:
                            LogClientContext(LogLevel.Debug, "Received message of type {0}", msg.Type.ToString());
                            break;
                        }
                    }
                    catch (System.IO.IOException)
                    {
                        LogClientContext(LogLevel.Debug, "Connection interrupted");
                        mRecvThreadDone = true;
                    }
                    catch (Exception e)
                    {
                        LogClientContext(LogLevel.Error, "Caught exception: {0}", e.Message);
                        LogClientContext(LogLevel.Trace, "Stack trace:\n{0}", e.StackTrace);
                        mRecvThreadDone = true;
                    }
                }

                mStream.Close();
                mClient.Close();
                mClientDoneDelegate(mUsername);
            }

            public void StartThread()
            {
                mRecvThread.Start();
            }

            public void ElevatePermissionLevel(UserPermissionLevel level)
            {
                mPermissionLevel = level;
            }

            public void SelectCommands(Dictionary<string, Command> commands)
            {
                // we iterate over all Commands that are available and pick the ones that we
                // can add, based on users' permission level
                foreach (KeyValuePair<string, Command> it in commands)
                {
                    if (it.Value.PermissionLevel <= mPermissionLevel)
                    {
                        mCommands.Add(it.Key, it.Value);
                    }
                }
            }

            public void ForceDisconnect()
            {
                mRecvThreadDone = true;
                mStream.Close();
                mClient.Close();
            }

            public void WaitForShutdown()
            {
                if (mRecvThread != null)
                    mRecvThread.Join();
            }


            // CLIMessageProxy implementation

            public void Message(string msg)
            {
                NotifyServerMessage m = new(mSessionData, msg);
                SendObject<NotifyServerMessage>(m);
            }

            public bool Ask(string msg)
            {
                NotifyServerMessage m = new(mSessionData, "Incoming question (not implemented yet): " + msg);
                SendObject<NotifyServerMessage>(m);
                return false;
            }

            public string Query(bool maskAnswer, string msg)
            {
                NotifyServerMessage m = new(mSessionData, "Incoming query (not implemented yet): " + msg);
                SendObject<NotifyServerMessage>(m);
                return "";
            }
        }

        private enum InterruptReason
        {
            Unknown,
            Shutdown,
            ClientToClear,
        };

        private Dictionary<string, Command> mCommands = new();
        private Dictionary<string, ClientContext> mClients = new();
        private InterruptReason mInterruptReason = InterruptReason.Unknown;
        private Queue<string> mClientsToClear = new();
        private Mutex mInterruptMutex = new();
        private IUserManager mUserManager = null;
        private TcpListener mServer;
        private string mAddress;
        private int mPort;

        public void OnClientRecvThreadDone(string username)
        {
            mInterruptMutex.WaitOne();

            mInterruptReason = InterruptReason.ClientToClear;
            mClientsToClear.Enqueue(username);
            mServer.Stop();

            mInterruptMutex.ReleaseMutex();
        }

        private void AcceptNewConnection()
        {
            // this blocks until a new connection comes in
            TcpClient client = mServer.AcceptTcpClient();

            // At first ClientContext is created with no permission level, which grants
            // no access to any LukeBot commands.
            // Technically it doesn't matter (no commands on ClientContext's side yet, plus
            // Receive Thread is not yet running), but it puts an extra layer of protection
            // in case we need to change the above.
            ClientContext context = new(client, OnClientRecvThreadDone);

            LoginServerMessage loginMsg = context.ReceiveObject<LoginServerMessage>();
            Logger.Log().Secure("Received login message: {0}", loginMsg.ToString());
            if (loginMsg.Type != ServerMessageType.Login ||
                loginMsg.Session != null ||
                Guid.TryParse(loginMsg.MsgID, out Guid result) == false ||
                loginMsg.User == null ||
                loginMsg.PasswordHashBase64 == null)
            {
                Logger.Log().Error("Malformed login message received");
                client.Close();
                return;
            }

            if (mClients.ContainsKey(loginMsg.User))
            {
                Logger.Log().Error("Attempted login at already logged in user");
                context.SendObject<LoginResponseServerMessage>(
                    new LoginResponseServerMessage(loginMsg, "Already logged in")
                );
                client.Close();
                return;
            }

            context.mUsername = loginMsg.User;

            byte[] pwdBuf = Convert.FromBase64String(loginMsg.PasswordHashBase64);
            UserPermissionLevel permLevel = mUserManager.AuthenticateUser(loginMsg.User, pwdBuf, out string reason);
            if (permLevel == UserPermissionLevel.None)
            {
                Logger.Log().Error("Login failed for user {0} - {1}", loginMsg.User, reason);
                context.SendObject<LoginResponseServerMessage>(
                    new LoginResponseServerMessage(loginMsg, reason)
                );
                client.Close();
                return;
            }

            // auth succeeded, give proper permission level and select allowed commands from the pool
            context.ElevatePermissionLevel(permLevel);
            context.SelectCommands(mCommands);

            // send back confirmation that all is well
            context.SendObject<LoginResponseServerMessage>(
                new LoginResponseServerMessage(loginMsg, context.mSessionData)
            );

            mClients.Add(context.mUsername, context);

            context.StartThread();
        }

        private void ClearDoneClients()
        {
            mInterruptMutex.WaitOne();

            while (mClientsToClear.Count > 0)
            {
                string client = mClientsToClear.Dequeue();
                Logger.Log().Debug("Clearing {0}", client);
                mClients[client].WaitForShutdown();
                mClients.Remove(client);
                Logger.Log().Debug("{0} removed", client);
            }

            mInterruptReason = InterruptReason.Unknown;

            mInterruptMutex.ReleaseMutex();
        }

        private void ClearClients()
        {
            foreach (ClientContext context in mClients.Values)
            {
                context.ForceDisconnect();
                context.WaitForShutdown();
            }

            mClients.Clear();
        }

        public ServerCLI(string address, int port, IUserManager userManager)
        {
            if (userManager == null)
                throw new ArgumentException("User manager is required for Server CLI to work.");

            mAddress = address;
            mPort = port;
            mUserManager = userManager;

            mServer = new TcpListener(IPAddress.Parse(address), port);
        }

        ~ServerCLI()
        {
        }

        public void AddCommand(string cmd, Command c)
        {
            if (!mCommands.TryAdd(cmd, c))
            {
                Logger.Log().Error("Failed to add command - " + cmd + " already exists");
            }
        }

        public void AddCommand(string cmd, UserPermissionLevel permissionLevel, CLIBase.CmdDelegate d)
        {
            AddCommand(cmd, new LambdaCommand(permissionLevel, d));
        }

        public void MainLoop()
        {
            bool done = false;

            try
            {
                mServer.Start();

                Console.CancelKeyPress += delegate {
                    mServer.Stop();
                    mInterruptReason = InterruptReason.Shutdown;
                    done = true;
                };

                Logger.Log().Info("Server started, awaiting connections.");

                while (!done)
                {
                    try
                    {
                        AcceptNewConnection();
                    }
                    catch (SocketException)
                    {
                        switch (mInterruptReason)
                        {
                        case InterruptReason.Unknown:
                        {
                            Logger.Log().Warning("SocketException caught - server was interrupted for unknown reason.");
                            done = true;
                            break;
                        }
                        case InterruptReason.Shutdown:
                        {
                            Logger.Log().Debug("SocketException caught - shutting down");
                            ClearClients();
                            done = true;
                            break;
                        }
                        case InterruptReason.ClientToClear:
                        {
                            Logger.Log().Debug("SocketException caught - there are Clients to clear");
                            mServer.Start();
                            ClearDoneClients();
                            break;
                        }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("{0} caught during ServerCLI operation: {1}", e.ToString(), e.Message);
                Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
            }
        }

        public void Teardown()
        {
        }

        public void SetPromptPrefix(string prefix)
        {
            // noop
        }
    }
}
