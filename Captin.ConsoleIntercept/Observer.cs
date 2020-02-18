using System;
using System.Collections.Generic;
using System.IO;

namespace Captin.ConsoleIntercept
{
    /// <summary>
    /// Get access to captured <see cref="Console.Out"/> information.
    /// 
    /// This is <see cref="IDisposable"/>.
    /// 
    /// <para>See also <see cref="ConsoleOut.Observe"/></para>
    /// </summary>
    public class Observer : IDisposable
    {
        private readonly TextWriter _writer;
        private readonly ConsoleOutProxyWriter _notifier;

        private Observer() { }

        internal Observer(TextWriter writer, ConsoleOutProxyWriter notifier)
        {
            _notifier = notifier;
            _writer = writer;
        }

        /// <summary>
        /// Read lines that have been captured from <see cref="Console.Out"/>.
        /// 
        /// <para>
        /// Note: Trailing newlines and carriage returns are removed for you.
        /// </para>
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> ReadLines()
        {
            var reader = new StringReader(ToString());
            string line;
            while((line = reader.ReadLine()) != null) {
                yield return line;
            }
        }

        private string _postDisposeStrValue;
        public override string ToString()
        {
            return _postDisposeStrValue ?? _writer?.ToString();
        }

        #region interface implementations
        private bool disposedValue = false; // To detect redundant calls
        protected virtual void Dispose(bool disposing)
        {
            if(disposedValue) return;

            if(disposing)
            {
                _notifier?.Unsubscribe(_writer);
                _postDisposeStrValue = ToString();
                _writer?.Dispose();
            }

            disposedValue = true;
        }

        ~Observer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
