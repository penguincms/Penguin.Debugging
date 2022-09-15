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
        private readonly ConcurrentQueue<string> Queue = new ConcurrentQueue<string>();
        private readonly BackgroundWorker Worker = new BackgroundWorker();
        private readonly AutoResetEvent QueueGate = new AutoResetEvent(false);
        private readonly AutoResetEvent DisposeGate = new AutoResetEvent(false);
        internal readonly ManualResetEvent FlushGate = new ManualResetEvent(true);

        private bool disposedValue;

        /// <summary>
        /// Creates a new instance of the string writer class
        /// </summary>
        /// <param name="action">A function that should be the target of the processing background thread.</param>
        public AsyncStringWriter(Action<string> action)
        {
            this.Action = action;

            this.Worker.DoWork += (se, e) => this.LoopProcess();

            this.Worker.RunWorkerAsync();
        }

        /// <summary>
        /// Disposes of the class, and flushes the queue
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Adds a new line of text to the internal queue, to be flushed by the background thread
        /// </summary>
        /// <param name="toEnque">The string to Enqueue</param>
        public void Enqueue(string toEnque)
        {
            //Add the line to print
            this.Queue.Enqueue(toEnque);

            _ = this.QueueGate.Set();
        }

        /// <summary>
        /// Disposes of the class, and flushes the queue
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                this.disposedValue = true;

                _ = this.QueueGate.Set();

                _ = this.DisposeGate.WaitOne();
            }
            else
            {
                Debug.WriteLine($"Attempting to dispose of already disposed {typeof(AsyncStringWriter)}");
            }
        }

        private void LoopProcess()
        {
            StringBuilder toLog = new StringBuilder();

            void Flush()
            {
                this.Action(toLog.ToString());

                _ = toLog.Clear();
            }

            while (this.QueueGate.WaitOne() && !this.disposedValue)
            {
                FlushGate.Reset();

                bool flush = false;

                while (this.Queue.TryDequeue(out string line))
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

                FlushGate.Set();
            }

            FlushGate.Set();
            _ = this.DisposeGate.Set();
        }
    }
}
