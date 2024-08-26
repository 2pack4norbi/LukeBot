using System.IO;

namespace LukeBot.Common
{
    public class FileUtils
    {
        public static void SetUnifiedCWD()
        {
            string cwd = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string newCwd = cwd + "/../../..";

            // DEV
            // Changes current directory to repo root to access Data directory
            // Otherwise, after publishing data this won't be accessible so CWD will be the exe location
            if (File.Exists(newCwd + "/LukeBot.sln"))
                Directory.SetCurrentDirectory(newCwd);
        }

        public static bool Exists(string path)
        {
            return Directory.Exists(path) || File.Exists(path);
        }
    }
}
