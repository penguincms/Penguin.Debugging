using System;
using System.Collections.Concurrent;
using System.ComponentModel;
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
        private readonly object ProcessLock = new object();
        private readonly ConcurrentQueue<string> Queue = new ConcurrentQueue<string>();
        private readonly BackgroundWorker Worker = new BackgroundWorker();
        private readonly object WorkerLock = new object();
        private bool disposedValue;

        private bool WorkerRunning = false;

        /// <summary>
        /// Creates a new instance of the string writer class
        /// </summary>
        /// <param name="action">A function that should be the target of the processing background thread.</param>
        public AsyncStringWriter(Action<string> action)
        {
            this.Action = action;

            this.Worker.DoWork += (se, e) => this.LoopProcess();
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
            //Dont let the worker check its state while we're mucking with it
            Monitor.Enter(this.WorkerLock);

            //Add the line to print
            this.Queue.Enqueue(toEnque);

            //Set internally by the worker before it exits, so more accurate than IsBusy
            if (!this.WorkerRunning || !this.Worker.IsBusy)
            {
                do
                {
                    //If we're waiting here, thats because the worker has decided its about to exit but hasn't yet. Race
                } while (this.Worker.IsBusy);

                //Now that its exited we start it again
                this.Worker.RunWorkerAsync();

                //Set this so we dont immediately try and start it again
                this.WorkerRunning = true;
            }

            //Now the worker can do things
            Monitor.Exit(this.WorkerLock);
        }

        /// <summary>
        /// Disposes of the class, and flushes the queue
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.LoopProcess();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                this.disposedValue = true;
            }
        }

        private void LoopProcess()
        {
            StringBuilder toLog = new StringBuilder();

            void Flush()
            {
                this.Action(toLog.ToString());

                toLog.Clear();
            }

            lock (this.ProcessLock)
            {
                Monitor.Enter(this.WorkerLock);

                while (!this.Queue.IsEmpty)
                {
                    Monitor.Exit(this.WorkerLock);

                    while (this.Queue.TryDequeue(out string line))
                    {
                        if (toLog.MaxCapacity <= toLog.Length + line.Length + Environment.NewLine.Length)
                        {
                            Flush();
                        }
                        else if (toLog.Length > 0)
                        {
                            toLog.Append(Environment.NewLine);
                        }

                        toLog.Append(line);
                    }

                    Flush();

                    Monitor.Enter(this.WorkerLock);
                }

                this.WorkerRunning = false;

                Monitor.Exit(this.WorkerLock);
            }
        }
    }
}