using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using LukeBot.Common;
using LukeBot.Communication.Common;
using LukeBot.Logging;


namespace LukeBot.Communication
{
    /**
     * Interface that event publishers should inherit from. Provides us
     * with necessary information regarding who publishes events.
     */
    public interface IEventPublisher
    {
        public string GetName();
        public List<EventDescriptor> GetEvents();
    }

    /**
     * A collection of events.
     *
     * This class stores all registered publishers, their events and
     * all dispatchers that were added.
     *
     * By default, an Immediate dispatcher is created, which can
     * be referred to by either not providing any target dispatcher
     * name (null or empty), or specifying "DEFAULT" dispatcher.
     *
     * One Publisher can only register once to one specific collection.
     * Events cannot have duplicate names within one collection, even
     * if they are provided by separate publishers. By design, the "evented"
     * end is not aware who is publishing events. However, this restriction
     * only applies within one collection - separate collections can have
     * same-named events provided by same publishers.
     */
    public class EventCollection
    {
        private string mLBUser;
        private Dictionary<string, IEventPublisher> mPublishers = new();
        private Dictionary<string, EventDispatcher> mDispatchers = new();
        private Dictionary<string, Event> mEvents = new();
        private const string DEFAULT_DISPATCHER_NAME = "DEFAULT";

        internal EventCollection(string lbUser)
        {
            mLBUser = lbUser;

            // Every event collection has a default immediate Dispatcher
            // For any more dispatchers, they need to be manually added
            AddEventDispatcher(DEFAULT_DISPATCHER_NAME, EventDispatcherType.Immediate);
        }

        ~EventCollection()
        {
            foreach (EventDispatcher ed in mDispatchers.Values)
            {
                ed.Stop();
            }
        }

        /**
         * Get registered Event of specified name.
         *
         * To subscribe to events use this function and add your delegate to .Endpoint member.
         */
        public Event Event(string name)
        {
            return mEvents[name];
        }

        /**
         * Get registered Dispatcher of specified name.
         *
         * This allows access to specific Dispatcher's (and its Events) behavior.
         */
        public EventDispatcher Dispatcher(string name)
        {
            return mDispatchers[name];
        }

        private EventCallback CreateEventCallback(string eventName, string dispatcherName)
        {
            Event ev = Event(eventName);
            EventDispatcher disp = Dispatcher(dispatcherName);
            return new EventCallback(eventName, (EventArgsBase args) => disp.Submit(ev, args));
        }

        private EventCallback AddEvent(EventDescriptor ed)
        {
            if (ed.Name == null || ed.Name.Length == 0)
                throw new EventDescriptorInvalidException("Event name is missing");

            if (mEvents.ContainsKey(ed.Name))
                throw new EventDescriptorInvalidException(string.Format("{0} event already exists", ed.Name));

            string disp = ed.Dispatcher;
            if (disp == null || disp.Length == 0)
                disp = DEFAULT_DISPATCHER_NAME;

            if (!mDispatchers.ContainsKey(disp))
                throw new EventDescriptorInvalidException(string.Format("{0}: Dispatcher {1} does not exist", ed.Name, disp));

            // These are just warnings to handle non-critical but still useful fields
            if (ed.Description == null || ed.Description.Length == 0)
                Logger.Log().Warning(
                    "{0}: Description field is null or empty. Consider adding event's description " +
                    "for better user interaction.",
                    ed.Name
                );

            if (ed.TestGenerator != null && ed.TestParams == null)
                Logger.Log().Warning(
                    "{0}: Test Generator was provided, but without specifying test parameters. " +
                    "Consider adding a list of accepted test parameters for better parsing and user information.",
                    ed.Name
                );

            mEvents.Add(ed.Name, new Event(ed));

            return CreateEventCallback(ed.Name, disp);
        }

        /**
         * Register a new Publisher in the collection.
         *
         * This call will query the Publisher for its name and events which are meant
         * to be published. See EventDescriptor class for requested information.
         *
         * Publishers MUST have their own unique name, and provided event names must NOT collide
         * with events already registered by other Publishers.
         */
        public List<EventCallback> RegisterPublisher(IEventPublisher p)
        {
            string pubName = p.GetName();

            if (mPublishers.ContainsKey(pubName))
                throw new PublisherAlreadyRegisteredException(pubName);

            List<EventDescriptor> eventsToAdd = p.GetEvents();
            if (eventsToAdd == null || eventsToAdd.Count == 0)
                throw new NoEventProvidedException();

            List<EventCallback> retCallback = new();

            foreach (var ed in eventsToAdd)
            {
                retCallback.Add(AddEvent(ed));
            }

            mPublishers.Add(pubName, p);

            return retCallback;
        }

        /**
         * Unregister a publisher.
         *
         * This will clear any Events associated with a Publisher.
         *
         * If Publisher's name is not found, returns quietly assuming it was already removed
         * or was not registered in the first place.
         */
        public void UnregisterPublisher(IEventPublisher p)
        {
            string pubName = p.GetName();

            if (!mPublishers.ContainsKey(pubName))
                return;

            List<EventDescriptor> events = p.GetEvents();

            if (events != null)
            {
                foreach (EventDescriptor e in events)
                {
                    if (mEvents.ContainsKey(e.Name))
                        mEvents.Remove(e.Name);
                }
            }

            mPublishers.Remove(pubName);
        }

        /**
         * Add a new Event Dispatcher.
         *
         * This will add an Event Dispatcher of specified name and type. For more information
         * on Dispatcher types, see EventDispatcher abstract class and implementations.
         *
         * Added Event Dispatcher MUST have an unique name, even if it's of different type.
         */
        public void AddEventDispatcher(string dispName, EventDispatcherType type)
        {
            EventDispatcher dispatcher = null;

            switch (type)
            {
            case EventDispatcherType.Immediate:
                dispatcher = new ImmediateEventDispatcher(dispName);
                break;
            case EventDispatcherType.Queued:
                dispatcher = new QueuedEventDispatcher(dispName);
                break;
            default:
                throw new ArgumentException("Invalid event dispatcher type");
            }

            dispatcher.Start();

            mDispatchers.Add(dispName, dispatcher);
        }

        /**
         * Remove an Event Dispatcher.
         *
         * This call will stop an existing dispatcher and remove it from collection.
         *
         * Note that there might be events still using this dispatcher. In such situation
         * there will be an EventStillInUseException thrown. It is best to clean Dispatchers only
         * after the Publisher has been unregistered.
         */
        public void RemoveEventDispatcher(string dispName)
        {
            if (!mDispatchers.ContainsKey(dispName))
                return; // was already removed, quietly exit

            // check if there still are publishers using the Dispatcher
            foreach (Event e in mEvents.Values)
            {
                if (e.Dispatcher == dispName)
                    throw new EventStillInUseException(e.Name, e.Dispatcher);
            }

            // stop the dispatcher; should be blocking
            mDispatchers[dispName].Stop();
            mDispatchers.Remove(dispName);
        }

        /**
         * Get information about a specific event
         */
        public EventInfo GetEventInfo(string eventName)
        {
            return Event(eventName).GetEventInfo();
        }

        public IEnumerable<EventInfo> ListEvents()
        {
            List<EventInfo> events = new();

            foreach (Event e in mEvents.Values)
            {
                events.Add(e.GetEventInfo());
            }

            return events;
        }

        public IEnumerable<EventDispatcherStatus> GetDispatcherStatuses()
        {
            List<EventDispatcherStatus> ret = new();

            foreach (EventDispatcher d in mDispatchers.Values)
            {
                ret.Add(d.Status());
            }

            return ret;
        }

        private void ValidateEventTestArgs(Event ev, IEnumerable<(string, string)> args)
        {
            if (ev.TestParams == null)
                return; // nothing to validate, let's assume this is a responsibilty of the generator

            foreach ((string a, string v) a in args)
            {
                if (!ev.TestParams.Any(param => param.Name == a.a))
                    throw new InvalidTestArgException(a.a);

                EventTestParam testParam = ev.TestParams.Single(param => param.Name == a.a);
                switch (testParam.Type)
                {
                case EventTestParamType.Integer:
                    // check if we can parse the value to int32
                    if (!Int32.TryParse(a.v, out int result))
                        throw new InvalidTestArgException(a.a);
                    break;
                default:
                    break;
                }
            }
        }

        public void TestEvent(string name, IEnumerable<(string, string)> args)
        {
            Event ev = mEvents[name];

            if (ev.TestGenerator == null)
                throw new TestArgGeneratorMissingException(ev.Name);

            string dispatcher = ev.Dispatcher;
            if (dispatcher == null || dispatcher.Length == 0)
                dispatcher = DEFAULT_DISPATCHER_NAME;

            ValidateEventTestArgs(ev, args);
            mDispatchers[dispatcher].Submit(ev, ev.TestGenerator(args));
        }
    }

    /**
     * Event System entry point.
     *
     * This class allows for more sophisticated control over events. The underlying mechanism
     * uses the standard Event Handlers, but allows for some organization in how they're used.
     *
     * EventSystem collects Event Collections. By design, one Event Collection should be assigned
     * to one LukeBot user. In addition to that, there's a special "global" event collection which
     * should contain and manage all LukeBot-wide events.
     */
    public class EventSystem
    {
        private Dictionary<string, EventCollection> mUserToCollection = new();

        public EventSystem()
        {
            // for Global events
            mUserToCollection.Add(Constants.LUKEBOT_USER_ID, new(Constants.LUKEBOT_USER_ID));
        }

        ~EventSystem()
        {
            mUserToCollection.Clear();
        }

        public void AddUser(string lbUser)
        {
            mUserToCollection.Add(lbUser, new EventCollection(lbUser));
        }

        public void RemoveUser(string lbUser)
        {
            mUserToCollection.Remove(lbUser);
        }

        public EventCollection User(string lbUser)
        {
            return mUserToCollection[lbUser];
        }

        public EventCollection Global()
        {
            return mUserToCollection[Constants.LUKEBOT_USER_ID];
        }
    }
}
