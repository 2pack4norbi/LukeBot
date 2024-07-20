using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using LukeBot.Common;


namespace LukeBot.Tests.Common
{
    [TestClass]
    public class UtilsTests
    {
        [TestMethod]
        public void Utils_ConvertArgStringsToTuples_NormalTest()
        {
            List<string> argNames = new()
            {
                "ThisIsAString",
                "ThisIsAnInteger",
                "ThisIsALongString"
            };

            List<string> argValues = new()
            {
                "testString",
                "342",
                "this is a long string with spaces"
            };

            string[] longStringWords = argValues[2].Split(' ');

            List<string> argStrings = new ()
            {
                argNames[0] + "=" + argValues[0],
                argNames[1] + "=" + argValues[1],
                argNames[2] + "=\"" + longStringWords[0]
            };

            for (int i = 1; i < longStringWords.Length; ++i)
            {
                if (i == longStringWords.Length - 1)
                    argStrings.Add(longStringWords[i] + '"');
                else
                    argStrings.Add(longStringWords[i]);
            }

            IEnumerable<(string, string)> tuples = Utils.ConvertArgStringsToTuples(argStrings);

            int ctr = 0;
            foreach ((string a, string b) t in tuples)
            {
                Assert.AreEqual(argNames[ctr], t.a);
                Assert.AreEqual(argValues[ctr], t.b);
                ctr++;
            }
        }

        [TestMethod]
        public void Utils_ConvertArgStringsToTuples_ParseErrorTest()
        {
            Assert.IsTrue(false, "This test must be implemented!");
        }

        [TestMethod]
        public void Utils_SplitJSONs()
        {
            string testJSON1 = "{\"this_json\"=10,\"is_simple\"=\"haha\"}";
            string testJSON2 = "{\"test\"=10,\"inner\"={\"test\"=\"haha\"}}";

            string fullmsg = testJSON1 + testJSON2;

            List<string> result = Utils.SplitJSONs(fullmsg);

            Assert.AreEqual(testJSON1, result[0]);
            Assert.AreEqual(testJSON2, result[1]);
        }
    }
}
