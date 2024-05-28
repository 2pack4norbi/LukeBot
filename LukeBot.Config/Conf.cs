using LukeBot.Logging;


namespace LukeBot.Config
{
    public class Conf
    {
        private static PropertyStore mStore;

        public static void Initialize(string storeDir)
        {
            mStore = new PropertyStore(storeDir);
        }

        public static void Add(Path path, Property p)
        {
            mStore.Add(path, p);
        }

        public static bool Exists(Path path)
        {
            return mStore.Exists(path);
        }

        public static bool Exists<T>(Path path)
        {
            return mStore.Exists<T>(path);
        }

        public static Property Get(Path path)
        {
            return mStore.Get(path);
        }

        public static T Get<T>(Path path)
        {
            return mStore.Get(path).Get<T>();
        }

        // TODO modify should probably be smarter than that
        // (as in - if Config field does not exist, create it automatically)
        public static void Modify<T>(Path path, T value)
        {
            mStore.Modify<T>(path, value);
        }

        public static void Remove(Path path)
        {
            mStore.Remove(path);
        }

        public static void Save()
        {
            mStore.Save();
        }

        public static void Clear()
        {
            mStore.Clear();
        }

        public static bool TryGet<T>(Path path, out T val)
        {
            try
            {
                val = Get<T>(path);
                return true;
            }
            catch (PropertyNotFoundException)
            {
                // prop not found, just return false;
                val = default(T);
                return false;
            }
            catch
            {
                throw;
            }
        }

        public static void PrintDebug(LogLevel level)
        {
            mStore.PrintDebug(level);
        }

        public static void Teardown()
        {
            Save();
            mStore = null;
        }
    }
}