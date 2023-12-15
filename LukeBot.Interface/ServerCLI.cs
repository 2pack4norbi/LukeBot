using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using LukeBot.Logging;


namespace LukeBot.Interface
{
    public class ServerCLI: CLI
    {
        private AutoResetEvent mCloseEvent = new(false);
        private Dictionary<string, Command> mCommands = new Dictionary<string, Command>();
        private TcpListener mServer;
        private string mAddress;
        private int mPort;

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
            try
            {
                mServer.Start();

                Console.CancelKeyPress += delegate {
                    mCloseEvent.Set();
                };

                mCloseEvent.WaitOne();
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
