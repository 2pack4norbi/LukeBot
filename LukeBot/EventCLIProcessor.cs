using System;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Logging;
using LukeBot.Module;
using LukeBot.Interface;
using CommandLine;

namespace LukeBot
{
    [Verb("test", HelpText = "Emit a test LukeBot event on a queue")]
    internal class EventTestCommand
    {
        [Value(0, MetaName = "eventName", Required = true, HelpText = "Event name")]
        public string Event { get; set; }

        [Value(1)]
        public IEnumerable<string> Args { get; set; }
    }

    [Verb("status", HelpText = "List available events and their statuses.")]
    internal class EventStatusCommand
    {
    }

    [Verb("info", HelpText = "Provide detailed information about selected event")]
    internal class EventInfoCommand
    {
        [Value(0, MetaName = "eventName", Required = true, HelpText = "EventName")]
        public string Event { get; set; }
    }


    internal class EventCommandBase
    {
        [Value(0, MetaName = "dispatcher", Default = "", Required = false, HelpText = "Event dispatcher")]
        public string Dispatcher { get; set; }
    }

    // below verbs all inherit from EventCommandBase
    [Verb("clear", HelpText = "Clear an event queue. Any not-emitted events on the queue will get discarded.")]
    internal class EventClearCommand: EventCommandBase
    {
    }

    [Verb("enable", HelpText = "Enable an event queue after disabling or holding.")]
    internal class EventEnableCommand: EventCommandBase
    {
    }

    [Verb("disable", HelpText = "Disable an event queue. Incoming events will be discarded.")]
    internal class EventDisableCommand: EventCommandBase
    {
    }

    [Verb("hold", HelpText = "Hold an event queue. Events will be queued until the queue is re-enabled via \"event enable\".")]
    internal class EventHoldCommand: EventCommandBase
    {
    }

    [Verb("skip", HelpText = "Skip currently handled event from the Queue.")]
    internal class EventSkipCommand: EventCommandBase
    {
    }

    internal class EventCLIProcessor: ICLIProcessor
    {
        private const string COMMAND_NAME = "event";
        private LukeBot mLukeBot;

        private string GetDefaultQueuedDispatcher(CLIMessageProxy CLI)
        {
            return "Twitch_QueuedDispatcher_" + CLI.GetCurrentUser();
        }

        void HandleTestCommand(EventTestCommand args, CLIMessageProxy CLI, out string msg)
        {
            try
            {
                // Parse args from command line into key=value tuples
                // See LukeBot.Common.Utils.ConvertArgString() for details
                IEnumerable<(string, string)> eventArgs = Utils.ConvertArgStringsToTuples(args.Args);

                Comms.Event.User(CLI.GetCurrentUser()).TestEvent(args.Event, eventArgs);
                msg = "Test event " + args.Event + " emitted";
            }
            catch (System.Exception e)
            {
                msg = "Failed to emit a test event: " + e.Message;
            }
        }

        void HandleInfoCommand(EventInfoCommand args, CLIMessageProxy CLI, out string msg)
        {
            try
            {
                EventInfo e = Comms.Event.User(CLI.GetCurrentUser()).GetEventInfo(args.Event);
                msg = e.Name + " event:\n";
                msg += "  " + e.Description + "\n";
                msg += "\n";
                msg += "Dispatcher: " + e.Dispatcher + "\n";
                msg += "Testable: " + e.Testable + "\n";
                if (e.Testable)
                {
                    msg += "\n";
                    msg += "Available test parameters:\n";
                    foreach (EventTestParam param in e.TestParams)
                    {
                        msg += "\\_ [" + param.Type.ToString() + "] \"" + param.Name + "\" - " + param.Description + "\n";
                    }
                }
            }
            catch (System.Exception e)
            {
                msg = "Failed to list information about event: " + e.Message;
            }
        }

        void HandleStatusCommand(EventStatusCommand args, CLIMessageProxy CLI, out string msg)
        {
            try
            {
                msg = "Dispatchers (name - type):\n";

                IEnumerable<EventDispatcherStatus> statuses = Comms.Event.User(CLI.GetCurrentUser()).GetDispatcherStatuses();

                foreach (EventDispatcherStatus s in statuses)
                {
                    msg += "  " + s.Name + " - " + s.Type.ToString();

                    if (s.Type == EventDispatcherType.Queued)
                    {
                        msg += ":\n";
                        msg += "    State: " + s.State + "\n";
                        msg += "    Events: " + s.EventCount + "\n";
                        msg += "\n";
                    }
                    else
                    {
                        msg += "\n";
                    }
                }

                msg += "Events (name - dispatcher):\n";

                IEnumerable<EventInfo> events = Comms.Event.User(CLI.GetCurrentUser()).ListEvents();

                foreach (EventInfo e in events)
                {
                    msg += "  " + e.Name + " - " + e.Dispatcher;

                    if (e.Testable)
                        msg += ", testable";

                    msg += "\n";
                }

                msg += "\n\"Testable\" events can emit a test event using \"event test <name>\"\n";
            }
            catch (System.Exception e)
            {
                msg = "Failed to query event system status: " + e.Message;
            }
        }

        void HandleClearCommand(EventClearCommand args, CLIMessageProxy CLI, out string msg)
        {
            string dispatcher = args.Dispatcher;

            try
            {
                if (dispatcher == null || dispatcher.Length == 0)
                    dispatcher = GetDefaultQueuedDispatcher(CLI);

                EventDispatcher dispatcherObject = Comms.Event.User(CLI.GetCurrentUser()).Dispatcher(dispatcher);
                dispatcherObject.Clear();
                dispatcherObject.Skip();
                msg = "Events on dispatcher " + dispatcher + " cleared.";
            }
            catch (System.Exception e)
            {
                msg = "Failed to clear " + dispatcher + " dispatcher: " + e.Message;
            }
        }

        void HandleEnableCommand(EventEnableCommand args, CLIMessageProxy CLI, out string msg)
        {
            string dispatcher = args.Dispatcher;

            try
            {
                if (dispatcher == null || dispatcher.Length == 0)
                    dispatcher = GetDefaultQueuedDispatcher(CLI);

                Comms.Event.User(CLI.GetCurrentUser()).Dispatcher(dispatcher).Enable();
                msg = "Dispatcher " + dispatcher + " enabled.";
            }
            catch (System.Exception e)
            {
                msg = "Failed to enable " + dispatcher + " dispatcher: " + e.Message;
            }
        }

        void HandleDisableCommand(EventDisableCommand args, CLIMessageProxy CLI, out string msg)
        {
            string dispatcher = args.Dispatcher;

            try
            {
                if (dispatcher == null || dispatcher.Length == 0)
                    dispatcher = GetDefaultQueuedDispatcher(CLI);

                Comms.Event.User(CLI.GetCurrentUser()).Dispatcher(dispatcher).Disable();
                msg = "Dispatcher " + dispatcher + " disabled.";
            }
            catch (System.Exception e)
            {
                msg = "Failed to disable " + dispatcher + " dispatcher: " + e.Message;
            }
        }

        void HandleHoldCommand(EventHoldCommand args, CLIMessageProxy CLI, out string msg)
        {
            string dispatcher = args.Dispatcher;

            try
            {
                if (dispatcher == null || dispatcher.Length == 0)
                    dispatcher = GetDefaultQueuedDispatcher(CLI);

                Comms.Event.User(CLI.GetCurrentUser()).Dispatcher(dispatcher).Hold();
                msg = "Dispatcher " + dispatcher + " put on hold.";
            }
            catch (System.Exception e)
            {
                msg = "Failed to hold " + dispatcher + " dispatcher: " + e.Message;
            }
        }

        void HandleSkipCommand(EventSkipCommand args, CLIMessageProxy CLI, out string msg)
        {
            string dispatcher = args.Dispatcher;

            try
            {
                if (dispatcher == null || dispatcher.Length == 0)
                    dispatcher = GetDefaultQueuedDispatcher(CLI);

                Comms.Event.User(CLI.GetCurrentUser()).Dispatcher(dispatcher).Skip();
                msg = "Dispatcher " + dispatcher + " event skipped.";
            }
            catch (System.Exception e)
            {
                msg = "Failed to skip event on " + dispatcher + " dispatcher: " + e.Message;
            }
        }

        public void AddCLICommands(LukeBot lb)
        {
            mLukeBot = lb;

            UserInterface.CLI.AddCommand(COMMAND_NAME, UserPermissionLevel.User, (CLIMessageProxy cliProxy, string[] args) =>
            {
                string result = "";

                Parser p = new Parser(with => with.HelpWriter = new CLIUtils.CLIMessageProxyTextWriter(cliProxy));
                p.ParseArguments<EventTestCommand, EventInfoCommand, EventStatusCommand, EventClearCommand, EventEnableCommand, EventDisableCommand, EventHoldCommand, EventSkipCommand>(args)
                    .WithParsed<EventTestCommand>((EventTestCommand args) => HandleTestCommand(args, cliProxy, out result))
                    .WithParsed<EventInfoCommand>((EventInfoCommand args) => HandleInfoCommand(args, cliProxy, out result))
                    .WithParsed<EventStatusCommand>((EventStatusCommand args) => HandleStatusCommand(args, cliProxy, out result))
                    .WithParsed<EventClearCommand>((EventClearCommand args) => HandleClearCommand(args, cliProxy, out result))
                    .WithParsed<EventEnableCommand>((EventEnableCommand args) => HandleEnableCommand(args, cliProxy, out result))
                    .WithParsed<EventDisableCommand>((EventDisableCommand args) => HandleDisableCommand(args, cliProxy, out result))
                    .WithParsed<EventHoldCommand>((EventHoldCommand args) => HandleHoldCommand(args, cliProxy, out result))
                    .WithParsed<EventSkipCommand>((EventSkipCommand args) => HandleSkipCommand(args, cliProxy, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, COMMAND_NAME, out result));
                return result;
            });
        }
    }
}