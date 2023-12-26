using Microsoft.VisualStudio.TestTools.UnitTesting;
using LukeBot.Config;
using System;
using System.Collections.Generic;


namespace LukeBot.Tests.Config
{
    [TestClass]
    public class ConfUtilTests
    {
        private const string CONF_UTIL_TEST_DATA_FILE = "Data/confutiltest.store.lukebot";

        public class Custom
        {
            public int a;
            public float b;

            public Custom(int a, float b)
            {
                this.a = a;
                this.b = b;
            }
        }

        public class CustomComparerA : IComparer<Custom>
        {
            public int Compare(Custom x, Custom y)
            {
                if (x.a > y.a)
                    return 1;
                else if (x.a < y.a)
                    return -1;
                else
                    return 0;
            }
        }

        public class CustomComparerB : IComparer<Custom>
        {
            public int Compare(Custom x, Custom y)
            {
                if (x.b > y.b)
                    return 1;
                else if (x.b < y.b)
                    return -1;
                else
                    return 0;
            }
        }


        [ClassInitialize]
        static public void ConfUtil_TestClassStartup(TestContext context)
        {
            Conf.Initialize(CONF_UTIL_TEST_DATA_FILE);
        }

        [TestInitialize]
        public void ConfUtil_TestInitialize()
        {
            Conf.Clear();
        }

        [TestMethod]
        public void ConfUtil_ArrayAppend_Single()
        {
            int[] numbers = { 1, 5, 3, 2, 4 };

            Path p = Path.Form("test", "numbers");

            foreach (int i in numbers)
                ConfUtil.ArrayAppend(p, i);

            // results should be sorted
            int[] expected = { 1, 2, 3, 4, 5 };
            int[] confNumbers = Conf.Get<int[]>(p);

            Assert.AreEqual(expected.Length, confNumbers.Length);
            for (int i = 0; i < expected.Length; ++i)
                Assert.AreEqual(expected[i], confNumbers[i]);
        }

        [TestMethod]
        public void ConfUtil_ArrayAppend_Array()
        {
            int[] numbers = { 1, 5, 3, 2, 4 };

            Path p = Path.Form("test", "numbers");

            ConfUtil.ArrayAppend(p, numbers);

            // results should be sorted
            int[] expected = { 1, 2, 3, 4, 5 };
            int[] confNumbers = Conf.Get<int[]>(p);

            Assert.AreEqual(expected.Length, confNumbers.Length);
            for (int i = 0; i < expected.Length; ++i)
                Assert.AreEqual(expected[i], confNumbers[i]);
        }

        [TestMethod]
        public void ConfUtil_ArrayAppend_Multiple()
        {
            Path p = Path.Form("test", "numbers");

            int[] numbers = { 1, 5, 3, 2, 4 };
            ConfUtil.ArrayAppend(p, numbers);

            int[] moreNumbers = { 7, 9, 6, 10, 8 };
            foreach (int i in moreNumbers)
                ConfUtil.ArrayAppend(p, i);

            // results should be sorted
            int[] expected = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            int[] confNumbers = Conf.Get<int[]>(p);

            Assert.AreEqual(expected.Length, confNumbers.Length);
            for (int i = 0; i < expected.Length; ++i)
                Assert.AreEqual(expected[i], confNumbers[i]);
        }

        [TestMethod]
        public void ConfUtil_ArrayAppend_Multiple_Array()
        {
            Path p = Path.Form("test", "numbers");

            int[] numbers = { 1, 5, 3, 2, 4 };
            ConfUtil.ArrayAppend(p, numbers);

            int[] moreNumbers = { 7, 9, 6, 10, 8 };
            ConfUtil.ArrayAppend(p, moreNumbers);

            // results should be sorted
            int[] expected = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            int[] confNumbers = Conf.Get<int[]>(p);

            Assert.AreEqual(expected.Length, confNumbers.Length);
            for (int i = 0; i < expected.Length; ++i)
                Assert.AreEqual(expected[i], confNumbers[i]);
        }

        [TestMethod]
        public void ConfUtil_ArrayAppend_Custom()
        {
            Path p = Path.Form("test", "custom");

            Custom[] array = { new Custom(10, 6.66f), new Custom(1, 2.0f), new Custom(5, 3.0f), new Custom(3, 3.14f) };
            foreach (Custom c in array)
                ConfUtil.ArrayAppend(p, c, new CustomComparerA());

            Custom[] expectedA = { new Custom(1, 2.0f), new Custom(3, 3.14f), new Custom(5, 3.0f), new Custom(10, 6.66f) };
            Custom[] confCustoms = Conf.Get<Custom[]>(p);

            Assert.AreEqual(expectedA.Length, confCustoms.Length);
            for (int i = 0; i < expectedA.Length; ++i)
            {
                Assert.AreEqual(expectedA[i].a, confCustoms[i].a);
                Assert.AreEqual(expectedA[i].b, confCustoms[i].b);
            }

            // redo but with comparer B
            Conf.Clear();

            foreach (Custom c in array)
                ConfUtil.ArrayAppend(p, c, new CustomComparerB());

            Custom[] expectedB = { new Custom(1, 2.0f), new Custom(5, 3.0f), new Custom(3, 3.14f), new Custom(10, 6.66f) };
            confCustoms = Conf.Get<Custom[]>(p);

            Assert.AreEqual(expectedB.Length, confCustoms.Length);
            for (int i = 0; i < expectedB.Length; ++i)
            {
                Assert.AreEqual(expectedB[i].a, confCustoms[i].a);
                Assert.AreEqual(expectedB[i].b, confCustoms[i].b);
            }
        }

        [TestMethod]
        public void ConfUtil_ArrayAppend_CustomArray()
        {
            Path p = Path.Form("test", "custom");

            Custom[] array = { new Custom(10, 6.66f), new Custom(1, 2.0f), new Custom(5, 3.0f), new Custom(3, 3.14f) };
            ConfUtil.ArrayAppend(p, array, new CustomComparerA());

            Custom[] expected = { new Custom(1, 2.0f), new Custom(3, 3.14f), new Custom(5, 3.0f), new Custom(10, 6.66f) };
            Custom[] confCustoms = Conf.Get<Custom[]>(p);

            Assert.AreEqual(expected.Length, confCustoms.Length);
            for (int i = 0; i < expected.Length; ++i)
            {
                Assert.AreEqual(expected[i].a, confCustoms[i].a);
                Assert.AreEqual(expected[i].b, confCustoms[i].b);
            }

            // redo but with comparer B
            Conf.Clear();

            ConfUtil.ArrayAppend(p, array, new CustomComparerB());

            Custom[] expectedB = { new Custom(1, 2.0f), new Custom(5, 3.0f), new Custom(3, 3.14f), new Custom(10, 6.66f) };
            confCustoms = Conf.Get<Custom[]>(p);

            Assert.AreEqual(expectedB.Length, confCustoms.Length);
            for (int i = 0; i < expectedB.Length; ++i)
            {
                Assert.AreEqual(expectedB[i].a, confCustoms[i].a);
                Assert.AreEqual(expectedB[i].b, confCustoms[i].b);
            }
        }

        [TestMethod]
        public void ConfUtil_ArrayRemove()
        {
            int[] numbers = { 1, 5, 3, 2, 4 };

            Path p = Path.Form("test", "numbers");

            ConfUtil.ArrayAppend(p, numbers);
            ConfUtil.ArrayRemove(p, 3);

            int[] expected = { 1, 2, 4, 5 };
            int[] confNumbers = Conf.Get<int[]>(p);

            Assert.AreEqual(expected.Length, confNumbers.Length);
            for (int i = 0; i < expected.Length; ++i)
                Assert.AreEqual(expected[i], confNumbers[i]);
        }

        [ClassCleanup]
        static public void ConfUtil_TestClassTeardown()
        {
            Conf.Teardown();
            System.IO.File.Delete(CONF_UTIL_TEST_DATA_FILE);
        }
    }
}
