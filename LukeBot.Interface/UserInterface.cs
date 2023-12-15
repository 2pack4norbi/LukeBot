namespace LukeBot.Interface
{
    /**
     * Main access point to User Interface implementations in LukeBot.
     *
     * It is assumed that only one type of UI is active at LukeBot's run.
     *
     * To make UI support easier in other parts of the code, UserInterface exposes three properties:
     *  - Instance - This returns base Interface object, which provides basic functionalities
     *    like Message/Ask/Query or MainLoop call to fall into when LukeBot starts.
     *  - CommandLine - This returns CLI-derived Interface object, which expands upon Instance's
     *    implementations with CLI-specific calls. If UI is initialized with non-CLI Interface,
     *    returns a Dummy CLI which basically does nothing.
     *  - Graphical - This returns GUI-derived Interface object, which expands upon Instance's
     *    implementations with GUI-specific calls. If UI is initialized with non-GUI Interface,
     *    returns a Dummy GUI which basically does nothing.
     */
    public class UserInterface
    {
        private static InterfaceType mType = InterfaceType.none;
        private static InterfaceBase mInterface = null;
        private static readonly object mLock = new();

        private static DummyCLI mDummyCLI = new();
        private static DummyGUI mDummyGUI = new();

        /**
         * Returns a User Interface instance.
         *
         * Returned Interface will implement necessary bits like Ask/Query/Message/MainLoop calls.
         *
         * If there is a need to perform some CLI-specific or GUI-specific operations, it is
         * recommended to use CommandLine or Graphical Properties.
         */
        public static InterfaceBase Instance
        {
            get
            {
                lock (mLock)
                {
                    if (mInterface == null)
                        throw new InterfaceNotInitializedException();

                    return mInterface;
                }
            }
        }

        /**
         * Returns a CLI-derived User Interface instance.
         *
         * Returned Interface will implement the same calls as Instance property, plus CLI-specific
         * calls (like CLI commands management).
         *
         * In case we currently did not initialize with a CLI-derived Interface, property will
         * return a Dummy CLI which replaces the implementations with noops.
         */
        public static CLI CommandLine
        {
            get
            {
                lock (mLock)
                {
                    if (mInterface == null)
                        throw new InterfaceNotInitializedException();

                    if ((mType & InterfaceType.CommandLine) != InterfaceType.none)
                        return mInterface as CLI;
                    else
                        // Assuming we are right now not working under CLI mode,
                        // we'll return a Dummy CLI which does nothing.
                        return mDummyCLI;
                }
            }
        }

        /**
         * Returns a GUI-derived User Interface instance.
         *
         * Returned Interface will implement the same calls as Instance property, plus GUI-specific
         * calls (like window management).
         *
         * In case we currently did not initialize with a GUI-derived Interface, property will
         * return a Dummy GUI which replaces the implementations with noops.
         */
        public static GUI Graphical
        {
            get
            {
                lock (mLock)
                {
                    if (mInterface == null)
                        throw new InterfaceNotInitializedException();

                    if ((mType & InterfaceType.Graphical) != InterfaceType.none)
                        return mInterface as GUI;
                    else
                        // Assuming we are right now not working under GUI mode,
                        // we'll return a Dummy GUI which does nothing.
                        return mDummyGUI;
                }
            }
        }

        public InterfaceType Type
        {
            get
            {
                return mType;
            }
        }

        public static void Initialize(InterfaceType type)
        {
            mType = type;

            switch (mType)
            {
            case InterfaceType.none:
                mInterface = mDummyCLI;
                break;
            case InterfaceType.basic:
                mInterface = new BasicCLI();
                break;
            case InterfaceType.server:
                mInterface = new ServerCLI("127.0.0.1", 55268);
                break;
            default:
                throw new UnrecognizedInterfaceTypeException(mType);
            }
        }

        public static void Teardown()
        {
            if (mInterface != null)
            {
                mInterface.Teardown();
                mInterface = null;
            }

            mType = InterfaceType.none;
        }
    }
}