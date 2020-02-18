using System;
using System.IO;

namespace Captin.ConsoleIntercept
{
    /// <summary>
    /// Contains methods to begin capturing <see cref="Console.Out"/> into a variable.
    /// </summary>
    public static class ConsoleOut
    {
        private static ConsoleOutProxyWriter writerNotifier;
        private static readonly TextWriter consoleOut = Console.Out;

        /// <summary>
        /// Start observing changes to <see cref="Console.Out"/>.
        ///
        /// <para>This leaves the original console out intact.</para>
        /// </summary>
        /// <returns></returns>
        public static Observer Observe()
        {
            InitNotifier();
            var subscription = writerNotifier.Subscribe(new StringWriter());
            return subscription;
        }

        private static void InitNotifier()
        {
            if(writerNotifier == null)
            {
                writerNotifier = new ConsoleOutProxyWriter(consoleOut);
                writerNotifier.OnObserversChanged += (sender, activeObservers) =>
                {
                    if(activeObservers == 0) {
                        Console.SetOut(consoleOut);
                    }
                    else {
                        Console.SetOut(writerNotifier);
                    }
                };
            }
        }
    }

}