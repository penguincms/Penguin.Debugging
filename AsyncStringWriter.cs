using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;

namespace Penguin.Debugging
{
    public class AsyncStringWriter : IDisposable
    {
        private readonly ConcurrentQueue<string> Queue = new ConcurrentQueue<string>();

        private readonly Action<string> Action;

        private readonly BackgroundWorker Worker = new BackgroundWorker();
        private readonly object WorkerLock = new object();
        private readonly object ProcessLock = new object();

        private bool disposedValue;

        private bool WorkerRunning = false;

        public AsyncStringWriter(Action<string> action)
        {
            Action = action;

            Worker.DoWork += (se, e) => { LoopProcess(); };
        }

        public void Enqueue(string toEnque)
        {

            //Dont let the worker check its state while we're mucking with it
            Monitor.Enter(WorkerLock);

            //Add the line to print
            Queue.Enqueue(toEnque);

            //Set internally by the worker before it exits, so more accurate than IsBusy
            if(!WorkerRunning || !Worker.IsBusy)
            {
                do
                {

                //If we're waiting here, thats because the worker has decided its about to exit but hasn't yet. Race
                } while (Worker.IsBusy);
                
                //Now that its exited we start it again
                Worker.RunWorkerAsync();

                //Set this so we dont immediately try and start it again
                WorkerRunning = true;
            }

            //Now the worker can do things
            Monitor.Exit(WorkerLock);
        }

        private void LoopProcess()
        {
            lock (ProcessLock)
            {
                Monitor.Enter(WorkerLock);

                while (!Queue.IsEmpty)
                {
                    Monitor.Exit(WorkerLock);

                    StringBuilder toLog = new StringBuilder();

                    List<string> toWrite = new List<string>();

                    do
                    {
                        if (Queue.TryDequeue(out string line))
                        {
                            toWrite.Add(line);
                        }

                    } while (!Queue.IsEmpty);

                    if (toWrite.Any())
                    {
                        for (int i = 0; i < toWrite.Count; i++)
                        {
                            toLog.Append(toWrite[i]);

                            if (i != toWrite.Count - 1)
                            {
                                toLog.Append(Environment.NewLine);
                            }
                        }

                        Action(toLog.ToString());
                    }

                    Monitor.Enter(WorkerLock);
                }

                WorkerRunning = false;

                Monitor.Exit(WorkerLock);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    LoopProcess();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AsyncStringWriter()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
