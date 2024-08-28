namespace LukeBot.Communication
{
    public class Comms
    {
        static private IntermediarySystem mIntermediarySystem;
        static private EventSystem mEventSystem;
        static private IntercomSystem mIntercomSystem;
        static private bool mInitialized;

        static public IntermediarySystem Intermediary
        {
            get
            {
                return mIntermediarySystem;
            }
        }

        static public EventSystem Event
        {
            get
            {
                return mEventSystem;
            }
        }

        static public IntercomSystem Intercom
        {
            get
            {
                return mIntercomSystem;
            }
        }

        static public bool Initialized
        {
            get
            {
                return mInitialized;
            }
        }

        static public void Initialize()
        {
            if (mInitialized)
                return;

            mEventSystem = new EventSystem();
            mIntercomSystem = new IntercomSystem();

            mIntermediarySystem = new IntermediarySystem();
            mInitialized = true;
        }

        static public void Teardown()
        {
            mIntermediarySystem = null;

            mIntercomSystem = null;
            mEventSystem = null;

            mInitialized = false;
        }
    }
}
