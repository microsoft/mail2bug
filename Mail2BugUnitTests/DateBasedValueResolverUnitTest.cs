using System;
using System.Collections.Generic;
using Mail2Bug.MessageProcessingStrategies;
using Microsoft.Test.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mail2BugUnitTests
{
    [TestClass]
    public class DateBasedValueResolverUnitTest
    {
        [TestMethod]
        public void BasicResolutionFirstItem()
        {
            var entries = BuildEntriesList(_rand.Next(), new[] { -2, 7, 14 });

            // It is important to capture the expected entry before passing 'entries' to the constructor of
            // DateBasedValueResolver, because it alters the list by adding a new item at the beginning
            var expected = entries[entries.Keys[0]]; // We expect to get the value from the first entry

            var resolver = new DateBasedValueResolver("", entries);
            var value = resolver.Resolve(DateTime.Now);

            Assert.AreEqual(expected, value, "Expected value '{0}', received value '{1}'", expected, value);
        }

        [TestMethod]
        public void BasicResolutionLastItem()
        {
            var entries = BuildEntriesList(_rand.Next(), new[] { -6, -5, -4, -3, -2, -1 });
            var expected = entries[entries.Keys[entries.Count - 1]]; // We expect to get the value from the last entry

            var resolver = new DateBasedValueResolver("", entries);
            var value = resolver.Resolve(DateTime.Now);

            Assert.AreEqual(expected, value, "Expected value '{0}', received value '{1}'", expected, value);
        }

        [TestMethod]
        public void BasicResolutionMiddleItem()
        {
            var entries = BuildEntriesList(_rand.Next(), new[] { -6, -5, -4, -3, 3, 50 });
            var expected = entries[entries.Keys[3]]; // We expect to get the value from the fourth entry (-3)

            var resolver = new DateBasedValueResolver("", entries);
            var value = resolver.Resolve(DateTime.Now);

            Assert.AreEqual(expected, value, "Expected value '{0}', received value '{1}'", expected, value);
        }

        [TestMethod]
        public void DefaultResolution()
        {
            var entries = BuildEntriesList(_rand.Next(), new[] { 10, 20, 30, 40 ,50 });
            const string defaultIteration = "Default";
            var resolver = new DateBasedValueResolver(defaultIteration, entries);

            var value = resolver.Resolve(DateTime.Now);

            Assert.AreEqual(defaultIteration, value, "Expected value '{0}', received value '{1}'", defaultIteration, value);
        }

        static SortedList<DateTime, string> BuildEntriesList(int seed, ICollection<int> offsets)
        {
            var sp = new StringProperties { MinNumberOfCodePoints = 0, MaxNumberOfCodePoints = 1000 };

            var iterations = new SortedList<DateTime, string>(offsets.Count);
            foreach (var offset in offsets)
            {
                iterations.Add(GetDateByOffset(offset), StringFactory.GenerateRandomString(sp, seed++));
            }

            return iterations;
        }

        static DateTime GetDateByOffset(int offsetInDays)
        {
            return DateTime.Now + TimeSpan.FromDays(offsetInDays);
        }

        readonly Random _rand = new Random();
    }
}
