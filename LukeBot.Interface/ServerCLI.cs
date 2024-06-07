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


namespace LukeBot.Interface
{
    public class ServerCLI: CLI
    {
        private class ClientContext
        {
            private const int COOKIE_SIZE = 32;

            public string mUsername = "";
            public string mCookie = "";
            public SessionData mSessionData = null;

            private TcpClient mClient = null;
            private NetworkStream mStream = null;
            private byte[] mRecvBuffer = new byte[4096];
            private Thread mRecvThread = null;
            private bool mRecvThreadDone = false;

            public ClientContext(TcpClient client)
            {
                mClient = client;
                mStream = mClient.GetStream();

                RandomNumberGenerator rng = RandomNumberGenerator.Create();
                byte[] cookieBuffer = new byte[COOKIE_SIZE];
                rng.GetBytes(cookieBuffer);
                mCookie = Convert.ToHexString(cookieBuffer);

                mSessionData = new(mCookie);
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
            {
                return JsonConvert.DeserializeObject<T>(Receive());
            }

            public void Send(string msg)
            {
                if (mStream == null)
                    return;

                byte[] sendBuf = Encoding.UTF8.GetBytes(msg);
                mStream.Write(sendBuf, 0, sendBuf.Length);
            }

            public void SendObject<T>(T obj)
            {
                Send(JsonConvert.SerializeObject(obj));
            }

            private void ReceiveThreadMain()
            {
                while (!mRecvThreadDone)
                {
                    try
                    {
                        ServerMessage msg = ReceiveObject<ServerMessage>();
                        if (msg.Session == null || msg.Session.Cookie != mSessionData.Cookie ||
                            msg.Type == ServerMessageType.Login)
                        {
                            // cut the connection, something was not correct
                            mRecvThreadDone = true;
                            break;
                        }

                        switch (msg.Type)
                        {
                        case ServerMessageType.Logout:
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        mRecvThreadDone = true;
                    }
                }

                mStream.Close();
                mClient.Close();
            }
        }

        private Dictionary<string, Command> mCommands = new();
        private Dictionary<string, ClientContext> mClients = new();
        private IUserManager mUserManager = null;
        private TcpListener mServer;
        private string mAddress;
        private int mPort;

        private void AcceptNewConnection()
        {
            try
            {
                // this blocks until a new connection comes in
                TcpClient client = mServer.AcceptTcpClient();

                ClientContext context = new(client);

                LoginServerMessage loginMsg = context.ReceiveObject<LoginServerMessage>();
                Logger.Log().Secure("Received login message: {0}", loginMsg.ToString());
                if (loginMsg.Type != ServerMessageType.Login ||
                    loginMsg.Session != null ||
                    Guid.TryParse(loginMsg.MsgID, out Guid result) == false ||
                    loginMsg.User == null ||
                    loginMsg.PasswordHashBase64 == null)
                {
                    Logger.Log().Error("Malformed login message received");
                    context.SendObject<LoginResponseServerMessage>(
                        new LoginResponseServerMessage(loginMsg, "Malformed login message")
                    );
                    client.Close();
                    return;
                }

                context.mUsername = loginMsg.User;

                byte[] pwdBuf = Convert.FromBase64String(loginMsg.PasswordHashBase64);
                if (!mUserManager.AuthenticateUser(loginMsg.User, pwdBuf, out string reason))
                {
                    Logger.Log().Error("Login failed for user {0} - {1}", loginMsg.User, reason);
                    context.SendObject<LoginResponseServerMessage>(
                        new LoginResponseServerMessage(loginMsg, reason)
                    );
                    client.Close();
                    return;
                }

                context.SendObject<LoginResponseServerMessage>(
                    new LoginResponseServerMessage(loginMsg, context.mSessionData)
                );

                mClients.Add(context.mCookie, context);
            }
            catch (Exception e)
            {
                Logger.Log().Error("Internal error when trying to accept new server connection: {0}", e.Message);
                Logger.Log().Error("Stack trace:\n{0}", e.StackTrace);
            }
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

        public void AddCommand(string cmd, CLI.CmdDelegate d)
        {
            AddCommand(cmd, new LambdaCommand(d));
        }

        public void Message(string message)
        {
            // TODO this looks over-engineered, but I want to improve CLI vastly over the course
            // of some patches (ex. control the Console Buffer directly to create a pseudo-UI)
            // so it's better to use this now than later replace all Console.WriteLine()-s in
            // rest of the project
            Logger.Log().Info(message);
        }

        public bool Ask(string message)
        {
            // TODO it should, send a query to the client and respond
            Logger.Log().Error("ServerCLI cannot respond to questions (yet)");
            return false;
        }

        public string Query(bool maskAnswer, string message)
        {
            Logger.Log().Error("ServerCLI cannot respond to queries (yet)");
            return "";
        }

        public void MainLoop()
        {
            bool done = false;

            try
            {
                mServer.Start();

                Console.CancelKeyPress += delegate {
                    mServer.Stop();
                    done = true;
                };

                Logger.Log().Info("Server started, awaiting connections.");

                while (!done)
                {
                    AcceptNewConnection();
                }
            }
            catch (SocketException)
            {
                Logger.Log().Warning("SocketException caught - server was stopped");
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("{0} caught: {1}", e.ToString(), e.Message);
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
