using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Penguin.Debugging
{
    /// <summary>
    /// An asyncronous string writer class. Accepts text into a queue, and uses a background thread to flush the text to a provided func for processing
    /// </summary>
    public class AsyncStringWriter : IDisposable
    {
        private readonly Action<string> Action;
        private readonly ConcurrentQueue<string> Queue = new();
        private readonly BackgroundWorker Worker = new();
        private readonly AutoResetEvent QueueGate = new(false);
        private readonly AutoResetEvent DisposeGate = new(false);
        internal readonly ManualResetEvent FlushGate = new(true);

        private bool disposedValue;

        /// <summary>
        /// Creates a new instance of the string writer class
        /// </summary>
        /// <param name="action">A function that should be the target of the processing background thread.</param>
        public AsyncStringWriter(Action<string> action)
        {
            Action = action;

            Worker.DoWork += (se, e) => LoopProcess();

            Worker.RunWorkerAsync();
        }

        /// <summary>
        /// Disposes of the class, and flushes the queue
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Adds a new line of text to the internal queue, to be flushed by the background thread
        /// </summary>
        /// <param name="toEnque">The string to Enqueue</param>
        public void Enqueue(string toEnque)
        {
            //Add the line to print
            Queue.Enqueue(toEnque);

            _ = QueueGate.Set();
        }

        /// <summary>
        /// Disposes of the class, and flushes the queue
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;

                _ = QueueGate.Set();

                _ = DisposeGate.WaitOne();
            }
            else
            {
                Debug.WriteLine($"Attempting to dispose of already disposed {typeof(AsyncStringWriter)}");
            }
        }

        private void LoopProcess()
        {
            StringBuilder toLog = new();

            void Flush()
            {
                Action(toLog.ToString());

                _ = toLog.Clear();
            }

            while (QueueGate.WaitOne() && !disposedValue)
            {
                _ = FlushGate.Reset();

                bool flush = false;

                while (Queue.TryDequeue(out string line))
                {
                    flush = true;

                    if (toLog.MaxCapacity <= toLog.Length + line.Length + Environment.NewLine.Length)
                    {
                        Flush();
                    }
                    else if (toLog.Length > 0)
                    {
                        _ = toLog.Append(Environment.NewLine);
                    }

                    _ = toLog.Append(line);
                }

                if (flush)
                {
                    Flush();
                }

                _ = FlushGate.Set();
            }

            _ = FlushGate.Set();
            _ = DisposeGate.Set();
        }
    }
}