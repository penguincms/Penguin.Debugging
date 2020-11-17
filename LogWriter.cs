using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Penguin.Debugging
{
    /// <summary>
    /// Logs everything, everywhere
    /// </summary>
    public class LogWriter : IDisposable
    {
        /// <summary>
        /// The path that the files will be created in
        /// </summary>
        public string LogFilePath { get; set; } = "Logs";

        private bool disposedValue;
        
        private readonly StreamWriter streamWriter;

        /// <summary>
        /// Constructs a new instance of the log writer
        /// </summary>
        /// <param name="logFilePath"></param>
        public LogWriter(string logFilePath = null)
        {
            this.LogFilePath = logFilePath ?? this.LogFilePath;

            string AssemblyName = "Unknown";

            try
            {
                AssemblyName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            } catch(Exception ex)
            {

            }
            //New file name based on current time
            string FileName = $"{DateTime.Now:yyyyMMdd_hhMMss}_{AssemblyName}.log";

            //Create output directory if it doesn't exist
            if (!Directory.Exists(this.LogFilePath))
            {
                Directory.CreateDirectory(this.LogFilePath);
            }

            //Wire up the stream writer
            this.streamWriter = new StreamWriter(new FileStream(Path.Combine(this.LogFilePath, FileName), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
        }

        /// <summary>
        /// Disposes of the writer and flushes to disk
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Writes an object or message to the log targets
        /// </summary>
        /// <param name="toLog">The object or message to log</param>
        public void WriteLine(object toLog)
        {
            //Time stamp it
            string prepend = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ";

            //Add the time stamp to the string
            string logString = $"{prepend} {toLog}";

            //To the file
            this.streamWriter.WriteLine(logString);

            //To the debug window
            Debug.WriteLine(logString);

            //To the console
            Console.WriteLine(logString);
        }
        /// <summary>
        /// Disposes of the writer and flushes to disk
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    //Ensure the file is flushed
                    this.streamWriter.Flush();
                    this.streamWriter.Close();
                    this.streamWriter.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                this.disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~LogWriter()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }
    }
}