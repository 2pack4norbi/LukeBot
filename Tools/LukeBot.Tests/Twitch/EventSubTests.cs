using Microsoft.VisualStudio.TestTools.UnitTesting;
using LukeBot.Config;
using LukeBot.Communication;
using LukeBot.Twitch;
using LukeBot.Twitch.Common;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Sockets;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Diagnostics;
using System.Reflection;
using System.Threading;


namespace LukeBot.Tests.Twitch
{
    /**
     * Additional attribute for EventSub tests. Will attempt to find twitch.exe in PATH and, if
     * located, will attempt to call `twitch help` to make sure this is the tool we need.
     *
     * Failure to locate Twitch CLI in PATH (or a different unrelated binary) will cause the test
     * method to be skipped.
     */
    public class TestMethodSkippedWithoutTwitchCLIAttribute: IgnorableTestMethodAtribute
    {
        private static string mTwitchCLIFullPath = null;

        public static string GetCLIPath()
        {
            return mTwitchCLIFullPath;
        }

        private bool IsBinaryActuallyTwitchCLI(string path)
        {
            ProcessStartInfo startInfo = new();
            startInfo.FileName = path;
            startInfo.Arguments = "help";
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;

            Process twitchCLI = new Process();
            twitchCLI.StartInfo = startInfo;

            try
            {
                twitchCLI.Start();
                string firstLine = twitchCLI.StandardOutput.ReadLine();
                if (!firstLine.Contains("A simple CLI tool for the New Twitch API"))
                    return false;
                else
                    return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected override bool ShouldIgnore(ITestMethod testMethod)
        {
            if (mTwitchCLIFullPath == null)
            {
                // test if twitch binary exists in PATH
                // TODO multi-platform...
                string str = Environment.GetEnvironmentVariable("PATH");
                if (str == null)
                    return true;

                string[] paths = str.Split(System.IO.Path.PathSeparator);
                foreach (string path in paths)
                {
                    string fullPath = System.IO.Path.Combine(path, "twitch.exe");
                    if (File.Exists(fullPath) && IsBinaryActuallyTwitchCLI(fullPath))
                    {
                        mTwitchCLIFullPath = fullPath;
                        return false;
                    }
                }

                // not found, fill in empty string
                mTwitchCLIFullPath = "";
                return true;
            }

            if (mTwitchCLIFullPath.Length > 0)
                return false;
            else
                return true;
        }
    }

    /**
     * EventSub tests class.
     *
     * These tests check basic functionalities of EventSub part in LukeBot.Twitch module.
     *
     * Tests *heavily* rely on Twitch CLI tool, which must be present in the system inside PATH.
     * Instructions to download the tool can be found here:
     *   https://github.com/twitchdev/twitch-cli/tree/main#download
     *
     * If Twitch CLI tool is not found in PATH at test runtime, all tests will be skipped.
     *
     * Tests can run in two ways, which is chosen automatically by the fixture:
     *  - As-is - Test class will setup its own mock Twitch WebSocket server and use it for testing.
     *    While this method is good for regular day-to-day test runs, it also means server's output
     *    is consumed by the tests.
     *  - With own mock EventSub WebSocket instance - manually-started EventSub WS server can be
     *    used by the fixture. To do that, run following line in a separate CLI window:
     *       `twitch event websocket start-server`
     *    This will start a mock websocket server and output its logs on the console window. Tests
     *    should recognize this (by connecting to default WS server endpoint) and not start its own
     *    instance. Might come in handy when debugging.
     *
     * In both cases Twitch CLI is still required in PATH, as some actions are triggered by tests
     * calling Twitch CLI (ex. reconnect or specific events).
     */
    [TestClass]
    public class EventSubTests
    {
        private enum TwitchWSStatus
        {
            Unknown = 0,
            Own,
            External
        }

        private static Process mTwitchWSProcess;
        private static TwitchWSStatus mTwitchWSStatus = TwitchWSStatus.Unknown;
        private static readonly string EVENT_SUB_TEST_USER = "testUserEventSub";
        private static readonly string TWITCH_MOCK_USERID = "420691234";
        private static readonly string TWITCH_MOCK_URI = "ws://127.0.0.1:8080/ws";

        private EventSubClient es = null;

        private TestContext testContext;
        public TestContext TestContext
        {
            get
            {
                return testContext;
            }
            set
            {
                testContext = value;
            }
        }

        private Process CallTwitchCLI(params string[] args)
        {
            ProcessStartInfo startInfo = new();
            startInfo.FileName = TestMethodSkippedWithoutTwitchCLIAttribute.GetCLIPath();
            foreach (string a in args)
                startInfo.ArgumentList.Add(a);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;

            Process ret = new Process();
            ret.StartInfo = startInfo;
            ret.Start();

            return ret;
        }

        private async Task EnsureTwitchCLIStarted()
        {
            if (mTwitchWSStatus == TwitchWSStatus.Unknown)
            {
                // check if user is running twitch mock manually
                // if they did, leave like nothing happened - probably they want to see what happens
                // on its side
                try
                {
                    ClientWebSocket socket = new ClientWebSocket();
                    await socket.ConnectAsync(new Uri(TWITCH_MOCK_URI), new CancellationTokenSource(10000).Token);

                    // we connected successfully so the server is already up
                    // setup a fake mTwitchWSProcess to not come back here this test run and leave
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    mTwitchWSStatus = TwitchWSStatus.External;
                    return;
                }
                catch (Exception)
                {
                    // noop, there is no server so we'll setup our own
                }

                mTwitchWSProcess = CallTwitchCLI("event", "websocket", "start-server", "--require-subscription");

                while (true)
                {
                    string line = await mTwitchWSProcess.StandardError.ReadLineAsync();
                    Assert.IsNotNull(line);
                    if (line.Contains("Connect to the WebSocket server at:"))
                        break;
                }

                mTwitchWSStatus = TwitchWSStatus.Own;
            }
        }

        [ClassInitialize]
        static public void EventSub_Initialize(TestContext context)
        {
            // Needed to cause LukeBot.Twitch.Common assembly to load, which is
            // required by EventSystem. Warnings suppressed for that part.
            #pragma warning disable 0168
            TwitchChannelPointsRedemptionArgs args;
            #pragma warning restore 0168

            // EventSub needs config for twitch.api_endpoint reference
            // This is to access mock Twitch API set up by Twitch CLI
            // and test subscriptions
            Conf.Initialize(Constants.TEST_PROPS_DATA_FILE);
            Comms.Initialize();
            Comms.Event.AddUser(EVENT_SUB_TEST_USER);
            Comms.Event.User(EVENT_SUB_TEST_USER).AddEventDispatcher(
                global::LukeBot.Twitch.Constants.QueuedDispatcherForUser(EVENT_SUB_TEST_USER), EventDispatcherType.Queued
            );
        }

        [TestInitialize]
        public async Task EventSub_InitializeTest()
        {
            if (es != null)
            {
                EventSub_CleanupTest();
            }

            await EnsureTwitchCLIStarted();

            es = new (EVENT_SUB_TEST_USER);
        }

        [TestCleanup]
        public void EventSub_CleanupTest()
        {
            es.RequestShutdown();
            es.WaitForShutdown();
            es = null;
        }

        [TestMethodSkippedWithoutTwitchCLI]
        public void EventSub_Connect()
        {
            es.Connect(null, TWITCH_MOCK_USERID, TWITCH_MOCK_URI);
        }

        [TestMethodSkippedWithoutTwitchCLI]
        public async Task EventSub_ConnectAsync()
        {
            await es.ConnectAsync(null, TWITCH_MOCK_USERID, TWITCH_MOCK_URI);
        }

        [TestMethodSkippedWithoutTwitchCLI]
        public async Task EventSub_Subscribe()
        {
            await es.ConnectAsync(null, TWITCH_MOCK_USERID, TWITCH_MOCK_URI);

            List<string> events = new();
            events.Add(EventSubClient.SUB_CHANNEL_POINTS_REDEMPTION_ADD);
            events.Add(EventSubClient.SUB_CHANNEL_POINTS_REDEMPTION_UPDATE);
            es.Subscribe(events);
        }

        [TestMethodSkippedWithoutTwitchCLI]
        public async Task EventSub_Reconnect()
        {
            AutoResetEvent reconnectedEvent = new(false);

            es.Reconnected += (e, args) =>
            {
                reconnectedEvent.Set();
            };

            await es.ConnectAsync(null, TWITCH_MOCK_USERID, TWITCH_MOCK_URI);

            Assert.AreNotEqual(TwitchWSStatus.Unknown, mTwitchWSStatus);

            // NOTE - reconnect testing can only happen once every 30 seconds.
            // Technically only one test should call this funciton per run. But,
            // if you're running your own Twitch CLI mock server, make sure to NOT
            // run tests too frequently (there's no way to detect this situation).
            Process reconnectCall = CallTwitchCLI("event", "websocket", "reconnect");
            await reconnectCall.WaitForExitAsync();
            Assert.AreEqual(0, reconnectCall.ExitCode);

            reconnectedEvent.WaitOne(5 * 1000);
        }

        [TestMethodSkippedWithoutTwitchCLI]
        public async Task EventSub_Notification()
        {
            AutoResetEvent notificationReceivedEvent = new(false);
            bool castedSuccessfully = false;
            Comms.Event.User(EVENT_SUB_TEST_USER).Event(Events.TWITCH_CHANNEL_POINTS_REDEMPTION).Endpoint += (e, a) =>
            {
                TwitchChannelPointsRedemptionArgs args = a as TwitchChannelPointsRedemptionArgs;
                castedSuccessfully = (args != null);
                Console.Error.WriteLine(String.Format("user: {0} name: {1} title: {2}", args.User, args.DisplayName, args.Title));
                notificationReceivedEvent.Set();
            };

            await es.ConnectAsync(null, TWITCH_MOCK_USERID, TWITCH_MOCK_URI);

            Assert.AreNotEqual(TwitchWSStatus.Unknown, mTwitchWSStatus);

            List<string> events = new();
            events.Add(EventSubClient.SUB_CHANNEL_POINTS_REDEMPTION_ADD);
            es.Subscribe(events);

            // testing channel point redemption
            Process reconnectCall = CallTwitchCLI(
                "event", "trigger",
                "channel.channel_points_custom_reward_redemption.add",
                "--transport", "websocket",
                "--session", es.SessionID
            );
            await reconnectCall.WaitForExitAsync();
            Assert.AreEqual(0, reconnectCall.ExitCode);

            notificationReceivedEvent.WaitOne(5 * 1000);

            Assert.IsTrue(castedSuccessfully);
        }

        [ClassCleanup]
        static public void EventSub_Teardown()
        {
            if (mTwitchWSProcess != null && mTwitchWSStatus == TwitchWSStatus.Own)
            {
                mTwitchWSProcess.Kill();
            }

            Comms.Teardown();
        }
    }
}