using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LukeBot.Common;
using LukeBot.Interface.Protocols;


namespace LukeBotClient
{
    internal class LukeBotClient
    {
        private ProgramOptions mOpts;
        private TcpClient mClient = null;
        private NetworkStream mStream = null;
        private SessionData mSessionData = null;
        private Thread mRecvThread = null;
        private bool mDone;
        private byte[] mRecvBuffer = null;
        private Mutex mPrintMutex = new();
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
            Print(line + '\n');
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
        }

        private async Task SendObject<T>(T obj)
        {
            await Send(JsonSerializer.Serialize<T>(obj));
        }

        private async Task<string> Receive()
        {
            try
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
            catch (System.Exception e)
            {
                PrintLine("\rFailed to receive data: " + e.Message);
                return "";
            }
        }

        private async Task<T> ReceiveObject<T>()
            where T: ServerMessage, new()
        {
            string ret = await Receive();

            if (ret.Length > 0)
                return JsonSerializer.Deserialize<T>(ret);
            else
                return new ServerMessage() as T;
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
                    PrintLine("\rCtrl+C handled: Requested shutdown");
                    mDone = true;
                    Utils.CancelConsoleIO();
                };

                mRecvBuffer = new byte[Constants.CLIENT_BUFFER_SIZE];
                await Login();

                PrintLine("\rConnected to LukeBot. Press Ctrl+C to close");

                // should be a simple "send command and wait for response" here
                mDone = false;
                while (!mDone)
                {
                    Print(PROMPT);
                    string msg = Console.ReadLine();

                    if (msg == "quit")
                    {
                        mDone = true;
                        continue;
                    }

                    CommandServerMessage cmdMessage = new(mSessionData, msg);
                    await SendObject<CommandServerMessage>(cmdMessage);

                    CommandResponseServerMessage resp = await ReceiveObject<CommandResponseServerMessage>();
                    if (resp.Status != ServerCommandStatus.Success)
                    {
                        PrintLine("Command failed on server side: " + resp.Status.ToString());
                    }
                }

                mClient.Close();
            }
            catch (System.Exception e)
            {
                PrintLine("\rException caught: " + e.Message);
            }
        }
    }
}