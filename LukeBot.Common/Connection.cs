﻿using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using LukeBot.Logging;

namespace LukeBot.Common
{
    public class Connection
    {
        private TcpClient mClient;
        private SslStream mSSLStream;
        private StreamReader mInput;
        private StreamWriter mOutput;
        private bool mUseSSL;

        public Connection(string address, int port, bool useSSL = false)
        {
            mUseSSL = useSSL;

            // Establish TCP connection to ip/port
            mClient = new TcpClient(address, port);
            Stream stream = mClient.GetStream();

            if (useSSL)
            {
                mSSLStream = new SslStream(mClient.GetStream(), false);
                mSSLStream.AuthenticateAsClient(address);
                stream = mSSLStream;
            }

            mInput = new StreamReader(stream);
            mOutput = new StreamWriter(stream);
        }

        public void Send(string msg)
        {
            if ((mClient == null) || (!mClient.Connected))
            {
                Logger.Log().Error("Connection not established - cannot send message");
                return;
            }

            Logger.Log().Secure("Send: {0}", msg);
            mOutput.WriteLine(msg);
            mOutput.Flush();
        }

        public string Read()
        {
            if ((mClient == null) || (!mClient.Connected))
            {
                return "Connection not established - cannot read message";
            }

            try
            {
                return mInput.ReadLine();
            }
            catch (IOException)
            {
                return "";
            }
            catch (System.Exception ex)
            {
                return "Error while reading message: " + ex.Message;
            }
        }

        public void Close()
        {
            mInput.Close();
            mOutput.Close();
            mClient.Close();
            mClient = null;
            mInput = null;
            mOutput = null;
        }
    }
}
