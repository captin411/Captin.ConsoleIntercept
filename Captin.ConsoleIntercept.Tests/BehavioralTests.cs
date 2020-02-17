using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Captin.ConsoleIntercept.Tests
{
    public class BehavioralTests
    {
        [Fact]
        public void TopmostStringWriger_Should_CaptureConsoleOut_OnlyWhenInsideUsingBlock()
        {
            var givenLogText = "Show me";

            Console.Write("realconsole before");
            var topmostWriter = new StringWriter();
            using (ConsoleOut.BeginIntercept(topmostWriter))
            {
                Console.Write(givenLogText);
            }
            Console.Write("realconsole after");
            Assert.Equal(givenLogText, topmostWriter.ToString());
        }

        [Fact]
        public void TopmostStringWriter_Should_CaptureConsoleOut_ForNestedUsingBlocksToo()
        {
            var givenLog1 = "Show me 1";
            var givenLog2 = "Show me 2";

            var topmostWriter = new StringWriter();
            var nestedWriter = new StringWriter();
            Console.Write("realconsole before");
            using (ConsoleOut.BeginIntercept(topmostWriter))
            {
                Console.Write(givenLog1);
                using (ConsoleOut.BeginIntercept(nestedWriter))
                {
                    Console.Write(givenLog2);
                }
            }
            Console.Write("realconsole after");
            Assert.Equal($"{givenLog1}{givenLog2}", topmostWriter.ToString());
        }

        [Fact]
        public void SecondToplevelStringWriter_Should_CaptureConsoleOut_OnlyWhenInsideSecondUsingBlock()
        {
            var givenLog1 = "Show me 1";
            var givenLog2 = "Show me 2";

            var topmostWriter1 = new StringWriter();
            var topmostWriter2 = new StringWriter();
            Console.Write("realconsole before");
            using (ConsoleOut.BeginIntercept(topmostWriter1))
            {
                Console.Write(givenLog1);
            }
            using (ConsoleOut.BeginIntercept(topmostWriter2))
            {
                Console.Write(givenLog2);
            }
            Console.Write("realconsole after");
            Assert.Equal(givenLog1, topmostWriter1.ToString());
            Assert.Equal(givenLog2, topmostWriter2.ToString());
        }

        [Fact]
        public void ToplevelStringWriter_Should_CaptureConsoleOut_EvenBeforeInterceptDispose()
        {
            var givenLog1 = "Show me 1";
            var topmostWriter1 = new StringWriter();

            Console.Write("realconsole before");

            var context = ConsoleOut.BeginIntercept(topmostWriter1);
            try
            {
                Console.Write(givenLog1);
                Assert.Equal(givenLog1, topmostWriter1.ToString());
            }
            finally
            {
                context?.Dispose();
            }
        }

        [Fact]
        public void BeginInterceptWithAdingWriterTwice_Should_ThrowException()
        {
            var writer = new StringWriter();
            var ex = Assert.Throws<ArgumentException>(() => {
                using(ConsoleOut.BeginIntercept(writer))
                {
                    using(ConsoleOut.BeginIntercept(writer))
                    {
                        Console.WriteLine("foo");
                    }
                }
            });
        }

        [Fact]
        public void DisposeContextOutOfOrder_Should_ThrowException()
        {
            var writer = new StringWriter();
            var ex = Assert.Throws<InvalidOperationException>(() => {
                var scope1 = ConsoleOut.BeginIntercept(new StringWriter());
                var scope2 = ConsoleOut.BeginIntercept(new StringWriter());
                scope1.Dispose();
            });
        }

        [Fact]
        public async Task BeginInterceptInTwoThreads_Should_MaintainSeparatedScopes()
        {
            const int count = 5;

            Task[] tasks = new Task[count];
            StringWriter[] writers = new StringWriter[5];
            for(var i = 0; i < count; i++)
            {
                writers[i] = new StringWriter();
                await Task.Run(async () => {
                    using (ConsoleOut.BeginIntercept(writers[i]))
                    {
                        await Task.Delay((new Random()).Next(50, 100));
                        Console.Write(i);
                        await Task.Delay((new Random()).Next(50, 100));
                    }
                });
            }

            for(int i = 0; i < writers.Length; i++)
            {
                Assert.Equal($"{i}", writers[i].ToString());
            }
        }
    }
}
