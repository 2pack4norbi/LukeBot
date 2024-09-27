namespace LukeBot
{
    /**
     * Main access point to User Interface implementations in LukeBot.
     *
     * It is assumed that only one type of UI is active at LukeBot's run.
     */
    internal class UserInterface
    {
        private static InterfaceType mType = InterfaceType.none;
        private static CLIBase mInterface = null;
        private static readonly object mLock = new();

        public delegate bool AuthorizeUserDelegate(string user, byte[] passwordHash, out string reason);

        /**
         * Returns a User Interface instance.
         *
         * Returned Interface will implement necessary bits like Ask/Query/Message/MainLoop calls.
         *
         * If there is a need to perform some CLI-specific or GUI-specific operations, it is
         * recommended to use CommandLine or Graphical Properties.
         */
        public static CLIBase CLI
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

        public InterfaceType Type
        {
            get
            {
                return mType;
            }
        }

        public static void Initialize(InterfaceType type, IUserManager userManager)
        {
            mType = type;

            switch (mType)
            {
            case InterfaceType.basic:
                mInterface = new BasicCLI(userManager);
                break;
            case InterfaceType.server:
                mInterface = new ServerCLI(userManager);
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