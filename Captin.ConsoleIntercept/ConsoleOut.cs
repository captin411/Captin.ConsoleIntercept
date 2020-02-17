using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Captin.ConsoleIntercept
{
    public static class ConsoleOut
    {
        private static readonly ConcurrentDictionary<int, AsyncLocal<TextWriterContextStack>> ContextStacksByThreadId = new ConcurrentDictionary<int, System.Threading.AsyncLocal<TextWriterContextStack>>();
        private static readonly AsyncLocal<int> CurrentThreadId = new AsyncLocal<int>();
        private static readonly TextWriter OriginalOut = System.Console.Out;

        /// <summary>
        /// Start capturing Console output into the provided <see cref="TextWriter"/>.
        /// <para>
        /// Output will also be written to the original console and any previously
        /// created and undisposed captures.
        /// </para>
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> that you want captured output to go.</param>
        /// <returns><see cref="IDisposable"/></returns>
        /// <exception cref="ArgumentException">If the <see cref="TextWriter"/> is not valid</exception>
        public static IDisposable BeginIntercept(TextWriter writer)
        {
            // if this is the first time we enter "BeginCapture" within this thread
            // then assign an identifier -- leave it alone if we are entering subsequent times
            // since we manage our own stack of begins
            if(CurrentThreadId.Value == 0)
            {
                CurrentThreadId.Value = (new object()).GetHashCode();
            }

            var parent = GetContextStackByThreadId(CurrentThreadId.Value);
            if(parent != null)
            {
                if(parent.GetWriters().Any((a) => Object.ReferenceEquals(a, writer)))
                {
                    throw new ArgumentException("You are already intercepting into this writer", nameof(writer));
                }
            }

            var current = new TextWriterContextStack(parent, writer);
            SetContextStackByThreadId(CurrentThreadId.Value, current);
            return current;
        }

        private static TextWriter EndIntercept(TextWriterContextStack endingStack)
        {
            var current = GetContextStackByThreadId(CurrentThreadId.Value);
            // do not support disposing of the contexts out of stack order
            if(!Object.ReferenceEquals(current, endingStack))
            {
                throw new InvalidOperationException("Disposing of contexts out of order is not supported");
            }
            SetContextStackByThreadId(CurrentThreadId.Value, current.Parent);

            // this is the last "pop" off of our stack, clear the current thread id
            if(current.Parent == null)
            {
                CurrentThreadId.Value = 0;
            }

            return current?.Value;
        }

        private static TextWriterContextStack GetContextStackByThreadId(int id)
        {
            return ContextStacksByThreadId.GetOrAdd(id, new AsyncLocal<TextWriterContextStack>()).Value;
        }

        private static TextWriterContextStack SetContextStackByThreadId(int id, TextWriterContextStack newValue)
        {
            if(newValue != null)
            {
                var stack = ContextStacksByThreadId.AddOrUpdate(id,
                    new AsyncLocal<TextWriterContextStack>() { Value = newValue },
                    (key, existing) =>
                    {
                        existing.Value = newValue;
                        return existing;
                    }
                );
                Console.SetOut(TextWriter.Synchronized(new ScopedTextWriterProxy(ContextStacksByThreadId)));
                return stack.Value;
            }
            else
            {
                ContextStacksByThreadId.TryRemove(id, out var _);
                Console.SetOut(TextWriter.Synchronized(OriginalOut));
                return null;
            }
        }

        class TextWriterContextStack : IDisposable
        {
            public TextWriterContextStack Parent { get; }
            public TextWriter Value { get; }

            private int _disposed;

            public TextWriterContextStack(TextWriterContextStack parent, TextWriter writer)
            {
                Parent = parent;
                Value = writer;
            }

            public IEnumerable<TextWriter> GetWriters()
            {
                var current = this;
                while (current != null)
                {
                    if (current.Value != null)
                    {
                        yield return current.Value;
                    }
                    current = current.Parent;
                }
                yield return OriginalOut;
            }

            void IDisposable.Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 1)
                {
                    EndIntercept(this);
                }
            }
        }

        class ScopedTextWriterProxy : TextWriter
        {
            private ConcurrentDictionary<int, AsyncLocal<TextWriterContextStack>> Context { get; }
            public ScopedTextWriterProxy(ConcurrentDictionary<int, AsyncLocal<TextWriterContextStack>> context)
            {
                Context = context; 
            }

            public override void Write(char value)
            {
                var threadId = CurrentThreadId.Value;
                if(Context.TryGetValue(threadId, out var asyncStack))
                {
                    var stack = asyncStack.Value;
                    if(stack != null)
                    {
                        foreach(var writer in stack.GetWriters())
                        {
                            writer?.Write(value);
                        }
                    }
                }
            }
            public override Encoding Encoding => Encoding.Default;
        }
    }
}
