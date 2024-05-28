using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using LukeBot.Common;

namespace LukeBot.Tests
{
    [TestClass]
    public class Setup
    {
        [AssemblyInitialize]
        public static void AssemblySetup(TestContext context)
        {
            FileUtils.SetUnifiedCWD();
            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {

        }
    }
}