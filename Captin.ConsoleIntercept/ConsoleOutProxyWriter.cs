using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Captin.ConsoleIntercept
{
    internal class ConsoleOutProxyWriter : TextWriter
    {
        #pragma warning disable CA2213
        private readonly TextWriter _consoleOut;
        #pragma warning restore CA2213

        private readonly List<TextWriter> _observers = new List<TextWriter>();

        public event EventHandler<int> OnObserversChanged;
        public override Encoding Encoding => Encoding.Default;

        public ConsoleOutProxyWriter(TextWriter originalConsoleOut)
        {
            _consoleOut = originalConsoleOut;
        }

        public void Unsubscribe(TextWriter writer)
        {
            _observers.Remove(writer);
            OnObserversChanged?.Invoke(this, _observers.Count);
        }

        public Observer Subscribe(TextWriter writer)
        {
            _observers.Add(writer);
            OnObserversChanged?.Invoke(this, _observers.Count);
            return new Observer(writer, this);
        }

        #region interface implementations
        public override void Write(char value)
        {
            foreach(var observer in _observers) { observer.Write(value); }
            _consoleOut.Write(value);
        }
        #endregion
    }
}
