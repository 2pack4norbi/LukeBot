using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using LukeBot.Communication;
using LukeBot.Communication.Common;


namespace LukeBot.Tests.Communication
{
    [TestClass]
    public class EventDispatcherTests
    {
        private const string TEST_EVENT_NAME = "TestEvent";
        private const string TEST_EVENT_DISPATCHER_NAME = "TestDispatcher";
        private const int TEST_VALUE = 42;

        public class TestArgs: EventArgsBase
        {
            public int testValue;
            public AutoResetEvent testDoneEvent = new(false);

            public TestArgs()
                : base("TestArgs")
            {
            }
        }

        private void TestDispatcher_Simple(EventDispatcher ed)
        {
            ed.Start();

            Event ev = new(new EventDescriptor()
            {
                Name = TEST_EVENT_NAME,
                Dispatcher = TEST_EVENT_DISPATCHER_NAME
            });

            bool eventSet = false;
            ev.Endpoint += (object o, EventArgsBase a) =>
            {
                TestArgs args = a as TestArgs;
                Assert.AreEqual(TEST_VALUE, args.testValue);
                eventSet = true;
                args.testDoneEvent.Set();
            };

            TestArgs args = new TestArgs() { testValue = TEST_VALUE };
            ed.Submit(ev, args);
            args.testDoneEvent.WaitOne();
            Assert.AreEqual(true, eventSet);

            ed.Stop();
        }

        private void TestDispatcher_Multiple(EventDispatcher ed)
        {
            const int EVENT_COUNT = 20;

            ed.Start();

            Event[] events = new Event[EVENT_COUNT];
            TestArgs[] eventArgs = new TestArgs[EVENT_COUNT];

            for (int i = 0; i < EVENT_COUNT; ++i)
            {
                eventArgs[i] = new();

                events[i] = new(new EventDescriptor()
                {
                    Name = TEST_EVENT_NAME + i.ToString(),
                    Dispatcher = TEST_EVENT_DISPATCHER_NAME
                });

                events[i].Endpoint += (object o, EventArgsBase a) =>
                {
                    TestArgs args = a as TestArgs;
                    Assert.AreEqual(TEST_VALUE, args.testValue);
                    Thread.Sleep(100); // imitate some "work" to be done
                    args.testDoneEvent.Set();
                };

                eventArgs[i] = new() { testValue = TEST_VALUE };
            }

            for (int i = 0; i < EVENT_COUNT; ++i)
            {
                ed.Submit(events[i], eventArgs[i]);
            }

            for (int i = 0; i < EVENT_COUNT; ++i)
            {
                eventArgs[i].testDoneEvent.WaitOne();
            }

            ed.Stop();
        }

        [TestMethod]
        public void EventDispatcher_Immediate_Simple()
        {
            TestDispatcher_Simple(new ImmediateEventDispatcher(TEST_EVENT_DISPATCHER_NAME));
        }

        [TestMethod]
        public void EventDispatcher_Queued_Simple()
        {
            TestDispatcher_Simple(new QueuedEventDispatcher(TEST_EVENT_DISPATCHER_NAME));
        }

        [TestMethod]
        public void EventDispatcher_Immediate_Multiple()
        {
            TestDispatcher_Multiple(new ImmediateEventDispatcher(TEST_EVENT_DISPATCHER_NAME));
        }

        [TestMethod]
        public void EventDispatcher_Queued_Multiple()
        {
            TestDispatcher_Multiple(new QueuedEventDispatcher(TEST_EVENT_DISPATCHER_NAME));
        }
    }
}