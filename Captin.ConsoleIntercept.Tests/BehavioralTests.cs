using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Captin.ConsoleIntercept.Tests
{
    public class BehavioralTests
    {
        [Fact]
        public void UsingBlock_OneLevel_ObservedConsoleOut_IsAsExpected()
        {
            using (var observed = ConsoleOut.Observe())
            {
                Console.Write("1");
                Assert.Equal("1", observed.ToString());
            }
        }

        [Theory]
        [InlineData("foo", new string[] { "foo" })]
        [InlineData("foo\n", new string[] { "foo" })]
        [InlineData("foo\r", new string[] { "foo" })]
        [InlineData("foo\r\n", new string[] { "foo" })]
        [InlineData("foo\nbar", new string[] { "foo", "bar" })]
        [InlineData("foo\rbar", new string[] { "foo", "bar" })]
        [InlineData("foo\r\nbar", new string[] { "foo", "bar" })]
        [InlineData("foo\nbar\n", new string[] { "foo", "bar" })]
        [InlineData("foo\rbar\r", new string[] { "foo", "bar" })]
        [InlineData("foo\r\nbar\r\n", new string[] { "foo", "bar" })]
        [InlineData("foo\r\nbar\r\n\r\n\r\n", new string[] { "foo", "bar", "", "" })]
        public void Observer_ReadLines_Should_StripNewlinesAfterConsoleWrite(string given, string[] expect)
        {
            var observed = ConsoleOut.Observe();
            Console.Write(given);
            observed.Dispose();
            Assert.Equal(expect, observed.ReadLines().ToArray());
        }

        [Fact]
        public void Observer_Readlines_Should_StripNewlinesAfterConsoleWriteLine()
        {
            var observed = ConsoleOut.Observe();
            Console.WriteLine("foo");
            Console.WriteLine("bar");
            observed.Dispose();
            Assert.Equal(new string[] { "foo", "bar" }, observed.ReadLines().ToArray());
        }

        [Fact]
        public void UsingBlock_TwoLevels_ObservedConsoleOut_IsAsExpected()
        {
            using (var observed1 = ConsoleOut.Observe())
            {
                Console.Write("1");
                using (var observed2 = ConsoleOut.Observe())
                {
                    Console.Write("2");
                }
                Assert.Equal("12", observed1.ToString());
            }
        }

        [Fact]
        public void UsingBlock_Consecutive_ObserveConsoleOut_IsAsExpected()
        {
            using (var observed = ConsoleOut.Observe())
            {
                Console.Write("1");
                Assert.Equal("1", observed.ToString());
            }

            using (var observed = ConsoleOut.Observe())
            {
                Console.Write("2");
                Assert.Equal("2", observed.ToString());
            }
        }

        [Fact]
        public void UsingBlock_ConsecutiveWithOriginalConsoleBetween_ObserveConsoleOut_IsAsExpected()
        {
            using (var observed = ConsoleOut.Observe())
            {
                Console.Write("1");
                Assert.Equal("1", observed.ToString());
            }

            Console.Write("2");

            using (var observed = ConsoleOut.Observe())
            {
                Console.Write("3");
                Assert.Equal("3", observed.ToString());
            }
        }

        [Fact]
        public void NewedUp_One_ObserveConsoleOut_IsAsExpected()
        {
            var context = ConsoleOut.Observe();
            try
            {
                Console.Write("1");
                Assert.Equal("1", context.ToString());
            }
            finally
            {
                context?.Dispose();
            }
        }

        [Fact]
        public void Dispose_OutOfOrder_ObservedConsoleOut_IsAsExpected()
        {
            var observer1 = ConsoleOut.Observe();
            var observer2 = ConsoleOut.Observe();
            Console.Write("1");
            observer1.Dispose();
            Console.Write("2");
            observer2.Dispose();
            Console.Write("3");

            Assert.Equal("1", observer1.ToString());
            Assert.Equal("12", observer2.ToString());
        }

        [Fact]
        public void OriginalConsole_AfterUsing_ShouldBeRestored()
        {
            var originalOut = Console.Out;
            using (var observer = ConsoleOut.Observe())
            {
                Console.Write("1");
                Assert.NotEqual(originalOut, Console.Out);
            }
            Assert.Equal(originalOut, Console.Out);
        }

        [Fact]
        public void OriginalConsoleWrite_AfterUsing_ShouldNotThrowException()
        {
            using (var observer = ConsoleOut.Observe())
            {
                Console.Write("1");
            }
            var exception = Record.Exception(() => Console.Write("2"));
            Assert.Null(exception);
        }

        [Fact]
        public async Task NOT_THREADSAFE()
        {
            async Task<bool> LogMessageIndicatesThreadSafe(string message, int delay)
            {
                using (var observe = ConsoleOut.Observe())
                {
                    if (delay > 0) { await Task.Delay(delay); }
                    Console.Write(message);
                    return message.Equals(observe.ToString());
                }
            }

            const int ITERATIONS = 5;

            var tasks = Enumerable.Range(1, ITERATIONS)
                .Select((i) => LogMessageIndicatesThreadSafe(i.ToString(), i * 100))
                .ToArray();
            var results = await Task.WhenAll(tasks);
            var threadSafe = results.All((result) => result == true);

            Assert.False(threadSafe, "NOTE: this library is not threadsafe");
        }
    }
}
