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
            private const int COOKIE_SIZE = 128;

            public string username = "";
            public string cookie = "";
            public TcpClient client = null;
            public NetworkStream stream = null;
            public SessionData sessionData = null;
            public byte[] recvBuffer = new byte[4096];

            public ClientContext()
            {
                RandomNumberGenerator rng = RandomNumberGenerator.Create();
                byte[] cookieBuffer = new byte[COOKIE_SIZE];
                rng.GetBytes(cookieBuffer);
                cookie = Convert.ToHexString(cookieBuffer);
            }

            public string Receive()
            {
                int read = 0;
                string msg = "";

                do
                {
                    read = stream.Read(recvBuffer, 0, 4096);
                    msg += Encoding.UTF8.GetString(recvBuffer, 0, read);
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
                if (stream == null)
                    return;

                byte[] sendBuf = Encoding.UTF8.GetBytes(msg);
                stream.Write(sendBuf, 0, sendBuf.Length);
            }

            public void SendObject<T>(T obj)
            {
                Send(JsonConvert.SerializeObject(obj));
            }
        }

        private Dictionary<string, Command> mCommands = new();
        private Dictionary<string, ClientContext> mClients = new();
        private TcpListener mServer;
        private string mAddress;
        private int mPort;

        private void AcceptNewConnection()
        {
            try
            {
                // this blocks until a new connection comes in
                TcpClient client = mServer.AcceptTcpClient();
                NetworkStream stream = client.GetStream();

                ClientContext context = new();
                context.client = client;
                context.stream = stream;

                LoginServerMessage loginMsg = context.ReceiveObject<LoginServerMessage>();
                context.username = loginMsg.User;

                // TODO check password here

                mClients.Add(context.cookie, context);

                context.SendObject<LoginResponseServerMessage>(
                    new LoginResponseServerMessage(loginMsg, context.sessionData)
                );
            }
            catch (Exception e)
            {
                Logger.Log().Error("Internal error when trying to accept new server connection: {0}", e.Message);
            }
        }

        public ServerCLI(string address, int port)
        {
            mAddress = address;
            mPort = port;

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
            Logger.Log().Error("ServerCLI cannot respond to questions");
            return false;
        }

        public string Query(string message)
        {
            Logger.Log().Error("ServerCLI cannot respond to queries");
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
