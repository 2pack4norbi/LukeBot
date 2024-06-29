using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LukeBot.Common;
using LukeBot.Interface.Protocols;
using Newtonsoft.Json;


namespace LukeBotClient
{
    internal class LukeBotClient
    {
        enum State
        {
            Init = 0,
            InCLI,
            AwaitingResponse,
            Done,
        };

        private ProgramOptions mOpts;
        private TcpClient mClient = null;
        private NetworkStream mStream = null;
        private SessionData mSessionData = null;
        private byte[] mRecvBuffer = null;
        private Thread mRecvThread = null;
        private bool mRecvThreadDone = false;
        private Mutex mPrintMutex = new();
        private State mState = State.Init;
        private ManualResetEvent mAwaitResponseEvent = new(true);
        private const string PROMPT = "> ";

        public LukeBotClient(ProgramOptions opts)
        {
            mOpts = opts;
        }

        private void Print(string text)
        {
            mPrintMutex.WaitOne();

            Console.Write(text);

            mPrintMutex.ReleaseMutex();
        }

        private void PrintLine(string line)
        {
            Print('\r' + line + '\n' + PROMPT);
        }

        private async Task Send(string cmd)
        {
            if (mStream == null)
            {
                PrintLine("\rStream unavailable");
                return;
            }

            byte[] sendBuffer = Encoding.UTF8.GetBytes(cmd);
            await mStream.WriteAsync(sendBuffer, 0, sendBuffer.Length);
            await mStream.FlushAsync();
        }

        private async Task SendObject<T>(T obj)
        {
            await Send(JsonConvert.SerializeObject(obj));
        }

        private async Task<string> Receive()
        {
            int read = 0;
            string recvString = "";

            do
            {
                read = await mStream.ReadAsync(mRecvBuffer, 0, 4096);
                recvString += Encoding.UTF8.GetString(mRecvBuffer, 0, read);
            }
            while (read == 4096);

            return recvString;
        }

        private async Task<T> ReceiveObject<T>()
            where T: ServerMessage, new()
        {
            string ret = await Receive();

            if (ret.Length > 0)
                return JsonConvert.DeserializeObject<T>(ret, new ServerMessageDeserializer());
            else
                return new ServerMessage() as T;
        }

        public async void ReceiveThreadMain()
        {
            while (!mRecvThreadDone)
            {
                ServerMessage msg = await ReceiveObject<ServerMessage>();

                if (msg == null)
                {
                    PrintLine("Receive thread exiting - received NULL message, probably connection is broken.");
                    mRecvThreadDone = true;
                    continue;
                }

                switch (msg.Type)
                {
                case ServerMessageType.Ping:
                {
                    PrintLine("Received Ping message");
                    PingServerMessage m = msg as PingServerMessage;
                    PingResponseServerMessage response = new(m);
                    await SendObject(response);
                    break;
                }
                case ServerMessageType.Notify:
                {
                    NotifyServerMessage m = msg as NotifyServerMessage;
                    PrintLine(m.Message);
                    break;
                }
                case ServerMessageType.CommandResponse:
                {
                    CommandResponseServerMessage m = msg as CommandResponseServerMessage;
                    switch (m.Status)
                    {
                    case ServerCommandStatus.Success:
                        PrintLine(m.Message);
                        break;
                    case ServerCommandStatus.InvalidArgument:
                        PrintLine("Command failed: Invalid argument");
                        break;
                    case ServerCommandStatus.UnknownCommand:
                        PrintLine("Unknown command");
                        break;
                    default:
                        PrintLine("Unknown command status received");
                        break;
                    }

                    mAwaitResponseEvent.Set();
                    break;
                }
                default:
                    PrintLine("Unrecognized message");
                    break;
                }
            }
        }

        public async Task Login()
        {
            bool loggedIn = false;
            int tries = 0;
            while (!loggedIn)
            {
                Console.Write("Username: ");
                string user = Console.ReadLine();

                Console.Write("Password: ");
                string pwdPlain = LukeBot.Common.Utils.ReadLineMasked(true);

                SHA512 hasher = SHA512.Create();
                byte[] pwdHash = hasher.ComputeHash(Encoding.UTF8.GetBytes(pwdPlain));

                mClient = new TcpClient();
                await mClient.ConnectAsync(mOpts.Address, mOpts.Port);
                mClient.SendBufferSize = Constants.CLIENT_BUFFER_SIZE;
                mClient.ReceiveBufferSize = Constants.CLIENT_BUFFER_SIZE;

                mStream = mClient.GetStream();

                LoginServerMessage msg = new(user, pwdHash);
                await SendObject<LoginServerMessage>(msg);

                LoginResponseServerMessage response = await ReceiveObject<LoginResponseServerMessage>();
                if (response.Type == ServerMessageType.None || !response.Success)
                {
                    // prevents/discourages bruteforcing
                    Thread.Sleep(3000);

                    tries++;
                    if (tries >= 3)
                        throw new SystemException("Failed to login: " + response.Error);

                    PrintLine("Failed to login: " + response.Error);

                }
                else
                {
                    loggedIn = true;
                    mSessionData = response.Session;
                }
            }
        }

        public async Task Run()
        {
            try
            {
                Console.CancelKeyPress += delegate
                {
                    PrintLine("Ctrl+C handled: Requested shutdown");
                    mState = State.Done;
                    Utils.CancelConsoleIO();
                    mAwaitResponseEvent.Set();
                };

                mRecvBuffer = new byte[Constants.CLIENT_BUFFER_SIZE];
                await Login();

                mRecvThread = new Thread(ReceiveThreadMain);
                mRecvThread.Name = "Receive Thread";
                mRecvThread.Start();

                PrintLine("Connected to LukeBot. Press Ctrl+C to close");

                // should be a simple "send command and wait for response" here
                mState = State.InCLI;
                while (mState != State.Done)
                {
                    string msg = "";

                    switch (mState)
                    {
                    case State.InCLI:
                        msg = Console.ReadLine();

                        if (msg == "quit")
                        {
                            LogoutServerMessage logoutMessage = new(mSessionData);
                            await SendObject<LogoutServerMessage>(logoutMessage);
                            mState = State.Done;
                            break;
                        }

                        CommandServerMessage cmdMessage = new(mSessionData, msg);
                        await SendObject<CommandServerMessage>(cmdMessage);
                        mState = State.AwaitingResponse;
                        break;
                    case State.AwaitingResponse:
                        mAwaitResponseEvent.WaitOne();
                        mAwaitResponseEvent.Reset();
                        mState = State.InCLI;
                        break;
                    default:
                        PrintLine("Invalid internal state: " + mState + " -- this should not happen");
                        mState = State.Done;
                        break;
                    }
                }

                mRecvThreadDone = true;
                mClient.Close();

                mRecvThread.Join();
            }
            catch (System.Exception e)
            {
                PrintLine("Exception caught: " + e.Message);
            }
        }
    }
}