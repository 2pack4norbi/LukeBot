using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LukeBot.Logging;


namespace LukeBotClient
{
    internal class LukeBotClient
    {
        private ProgramOptions mOpts;
        private TcpClient mClient;
        private NetworkStream mStream;
        private bool mDone;
        private byte[] mRecvBuffer;

        public LukeBotClient(ProgramOptions opts)
        {
            mOpts = opts;

            mClient = new TcpClient();
        }

        private async Task Send(string cmd)
        {
            if (mStream == null)
            {
                Logger.Log().Error("Stream unavailable");
                return;
            }

            byte[] sendBuffer = Encoding.UTF8.GetBytes(cmd);
            await mStream.WriteAsync(sendBuffer, 0, sendBuffer.Length);
        }

        private void Receive(IAsyncResult result)
        {
            try
            {
                int length = mStream.EndRead(result);
                if (length <= 0)
                    return;

                string recvString = Encoding.UTF8.GetString(mRecvBuffer, 0, length);

                Console.WriteLine(recvString);

                mStream.BeginRead(mRecvBuffer, 0, mRecvBuffer.Length, Receive, null);
            }
            catch (Exception e)
            {
                Logger.Log().Error("Failed to receive data: {0}", e.Message);
                return;
            }
        }

        public async Task Run()
        {
            try
            {
                Console.CancelKeyPress += delegate
                {
                    Logger.Log().Info("Ctrl+C handled: Requested shutdown");
                    mDone = true;
                    Utils.CancelConsoleIO();
                };

                await mClient.ConnectAsync(mOpts.Address, mOpts.Port);
                mClient.SendBufferSize = Constants.CLIENT_BUFFER_SIZE;
                mClient.ReceiveBufferSize = Constants.CLIENT_BUFFER_SIZE;

                mStream = mClient.GetStream();
                mRecvBuffer = new byte[Constants.CLIENT_BUFFER_SIZE];
                mStream.BeginRead(mRecvBuffer, 0, Constants.CLIENT_BUFFER_SIZE, Receive, null);

                Console.WriteLine("Connected to LukeBot. Press Ctrl+C to close");

                // should be a simple "send command and wait for response" here
                mDone = false;
                while (!mDone)
                {
                    string msg = Console.ReadLine();

                    if (msg == "quit")
                    {
                        mDone = true;
                        continue;
                    }

                    await Send(msg);
                }

                mClient.Close();
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("Exception caught: {0}", e.Message);
                Logger.Log().Error("Backtrace:\n{0}", e.StackTrace);
            }
        }
    }
}