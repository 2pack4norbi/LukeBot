using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using LukeBot.Logging;
using LukeBot.Communication.Common;

[assembly: InternalsVisibleTo("LukeBot.Tests")]

namespace LukeBot.Communication
{
    public enum EventDispatcherType
    {
        Immediate = 0,
        Queued
    }

    public enum EventDispatcherState
    {
        Stopped = 0,
        Running,
        OnHold,
        Disabled,
        Done
    }

    public struct EventDispatcherStatus
    {
        public EventDispatcherType Type;
        public string Name;
        public EventDispatcherState State;
        public int EventCount;
    }

    public abstract class EventDispatcher
    {
        protected string mName;

        protected EventDispatcher(string name)
        {
            mName = name;
        }

        public abstract void Submit(Event ev, EventArgsBase args);
        public abstract void Start();
        public abstract void Stop();
        public abstract void Clear();
        public abstract void Enable();
        public abstract void Disable();
        public abstract void Hold();
        public abstract void Skip();
        public abstract EventDispatcherStatus Status();
    }

    /**
     * Simple event dispatcher, executing events immediately after their arrival
     * and on the same thread.
     *
     * Useful for short and quick events which require immediate reaction from the
     * receiving side (ex. Spotify's NowPlaying service). Note that in this Dispatcher
     * event execution will block the calling thread until it is completed.
     *
     * Because Events are dispatched immediately on the calling thread, there is no way
     * to control this Dispatcher from UI.
     */
    internal class ImmediateEventDispatcher: EventDispatcher
    {
        public ImmediateEventDispatcher(string name)
            : base(name)
        {
        }

        public override void Submit(Event ev, EventArgsBase args)
        {
            // immediately execute an event upon submission
            ev.Raise(args);
        }

        public override void Start()
        {
            // noop
        }

        public override void Stop()
        {
            // noop
        }

        public override void Clear()
        {
            Logger.Log().Warning("Advanced operations not available on Immediate Dispatcher.");
        }

        public override void Enable()
        {
            Logger.Log().Warning("Advanced operations not available on Immediate Dispatcher.");
        }

        public override void Disable()
        {
            Logger.Log().Warning("Advanced operations not available on Immediate Dispatcher.");
        }

        public override void Hold()
        {
            Logger.Log().Warning("Advanced operations not available on Immediate Dispatcher.");
        }

        public override void Skip()
        {
            Logger.Log().Warning("Advanced operations not available on Immediate Dispatcher.");
        }

        public override EventDispatcherStatus Status()
        {
            return new EventDispatcherStatus()
            {
                Name = mName,
                Type = EventDispatcherType.Immediate,
                EventCount = 0,
                State = EventDispatcherState.Running
            };
        }
    }

    /**
     * Dispatches events onto a queue, which are executed sequentially by a separate thread.
     *
     * This Dispatcher allows more control over how events are dispatched. Functionalities
     * include:
     *   - Disabling - ignores incoming events and skips them completely
     *   - Holding - stores incoming events but doesn't raise them
     *   - Skipping - interrupts currently processed event
     *   - Clearing - skips current event and clears all queued events
     *
     * Events from this Dispatcher are enqueued and raised via a separate worker thread.
     * This allows to perform some more time-consuming actions in order and without them
     * possibly happening at the same time.
     *
     * Notable use-case is ex. subscription alerts from Twitch, which play back a TTS with
     * subscription message.
     */
    internal class QueuedEventDispatcher: EventDispatcher
    {
        class EventQueueItem
        {
            public Event ev;
            public EventArgsBase args;

            public EventQueueItem(Event e, EventArgsBase a)
            {
                ev = e;
                args = a;
            }
        }

        private Thread mThread = null;
        private EventDispatcherState mState = EventDispatcherState.Stopped;
        private Queue<EventQueueItem> mEvents = new();
        private ManualResetEvent mThreadStartedEvent = new(false);
        private ManualResetEvent mQueueAvailableEvent = new(false);
        private Mutex mEventQueueMutex = new();
        private EventQueueItem mCurrentEvent = null;

        public QueuedEventDispatcher(string name)
            : base(name)
        {
            mThread = new Thread(WorkerMain);
            mThread.Name = name + " Event Dispatcher Worker";
        }

        private void EnqueueItem(EventQueueItem item)
        {
            if (mState != EventDispatcherState.Running &&
                mState != EventDispatcherState.OnHold)
                return;

            mEventQueueMutex.WaitOne();

            if (mEvents != null)
                mEvents.Enqueue(item);

            mEventQueueMutex.ReleaseMutex();
        }

        private EventQueueItem DequeueItem()
        {
            if (mState != EventDispatcherState.Running)
                return null;

            mEventQueueMutex.WaitOne();
            EventQueueItem item = null;
            if (mEvents.Count > 0)
                item = mEvents.Dequeue();
            mEventQueueMutex.ReleaseMutex();

            return item;
        }

        private void InterruptCurrentEvent()
        {
            if (mCurrentEvent == null)
            {
                Logger.Log().Debug("No current event to skip");
                return;
            }

            mCurrentEvent.ev.Interrupt();
        }

        private void WorkerMain()
        {
            mState = EventDispatcherState.Running;
            mThreadStartedEvent.Set();

            while (true)
            {
                try
                {
                    if (mEvents.Count == 0 || mState == EventDispatcherState.OnHold)
                    {
                        mQueueAvailableEvent.Reset();
                        mQueueAvailableEvent.WaitOne();
                    }

                    if (mState == EventDispatcherState.Done)
                        break;

                    mCurrentEvent = DequeueItem();
                    if (mCurrentEvent != null)
                    {
                        mCurrentEvent.ev.Raise(mCurrentEvent.args);
                        mCurrentEvent = null;
                    }
                }
                catch (ThreadInterruptedException)
                {
                    // Thread was interrupted - handle restoring after skipping current event
                    mCurrentEvent = null;
                }
                catch (System.Exception e)
                {
                    Logger.Log().Error("Caught exception on {0} Event Dispatcher: {1}", mName, e.Message);
                    Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
                }
            }

            mState = EventDispatcherState.Stopped;
        }

        public override void Submit(Event ev, EventArgsBase args)
        {
            EnqueueItem(new EventQueueItem(ev, args));
            mQueueAvailableEvent.Set();
        }

        public override void Start()
        {
            if (mState != EventDispatcherState.Stopped)
                return;

            mThread.Start();
            mThreadStartedEvent.WaitOne();
        }

        public override void Stop()
        {
            if (mState != EventDispatcherState.Done && mState != EventDispatcherState.Stopped)
            {
                mState = EventDispatcherState.Done;
                mQueueAvailableEvent.Set();
                mThread.Join();

                // wrapped in mutexes in case an event is submitted at the same time somehow
                mEventQueueMutex.WaitOne();
                mEvents.Clear();
                mEvents = null;
                mEventQueueMutex.ReleaseMutex();

                mThreadStartedEvent.Reset();
            }
        }

        public override void Clear()
        {
            mEventQueueMutex.WaitOne();
            mEvents.Clear();
            mEventQueueMutex.ReleaseMutex();
        }

        public override void Enable()
        {
            if (mState != EventDispatcherState.Disabled &&
                mState != EventDispatcherState.OnHold)
            {
                Logger.Log().Warning("{0} Dispatcher: Not expected state {1}! Ignoring Enable() call", mName, mState);
                return;
            }

            mState = EventDispatcherState.Running;
            mQueueAvailableEvent.Set();
        }

        public override void Disable()
        {
            if (mState != EventDispatcherState.Running)
            {
                Logger.Log().Warning("{0} Dispatcher: Not expected state {1}! Ignoring Disable() call", mName, mState);
                return;
            }

            mState = EventDispatcherState.Disabled;
            Clear();
        }

        public override void Hold()
        {
            if (mState != EventDispatcherState.Running)
            {
                Logger.Log().Warning("{0} Dispatcher: Not expected state {1}! Ignoring Hold() call", mName, mState);
                return;
            }

            mState = EventDispatcherState.OnHold;
        }

        public override void Skip()
        {
            if (mState != EventDispatcherState.Running)
            {
                Logger.Log().Warning("{0} Dispatcher: Not expected state {1}! Ignoring Hold() call", mName, mState);
                return;
            }

            InterruptCurrentEvent();
        }

        public override EventDispatcherStatus Status()
        {
            mEventQueueMutex.WaitOne();

            EventDispatcherStatus status = new EventDispatcherStatus()
            {
                Name = mName,
                Type = EventDispatcherType.Queued,
                EventCount = mEvents.Count,
                State = mState
            };

            mEventQueueMutex.ReleaseMutex();

            return status;
        }
    }
}