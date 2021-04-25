using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Penguin.Debugging
{
    /// <summary>
    /// Logs everything, everywhere
    /// </summary>
    public class LogWriter : IDisposable
    {
        private readonly StreamWriter streamWriter;

        private bool disposedValue;

        /// <summary>
        /// The path that the files will be created in
        /// </summary>
        public string LogFilePath { get; set; } = "Logs";

        /// <summary>
        /// The method used to serialized non-string objects. defaults to Auto
        /// </summary>
        public ObjectSerializationMethod ObjectSerializationMethod { get; set; } = ObjectSerializationMethod.Auto;

        /// <summary>
        /// An optional method used to override object serialization if additional logic is required (ex json serialization)
        /// </summary>
        public Func<object, string> ObjectSerializationOverride { get; set; }


        public LogOutput DefaultOutput { get; set; } = LogOutput.All;

        public static readonly AsyncStringWriter DebugQueue = new AsyncStringWriter((s) => Debug.WriteLine(s));
        
        public static readonly AsyncStringWriter ConsoleQueue = new AsyncStringWriter((s) => Console.WriteLine(s));

        public readonly AsyncStringWriter FileQueue;


        /// <summary>
        /// Constructs a new instance of the log writer
        /// </summary>
        /// <param name="logFilePath">Defaults to "Logs"</param>
        /// <param name="output">The location all lines should be written to by default</param>
        public LogWriter(string logFilePath = null, LogOutput output = LogOutput.All)
        {
            this.DefaultOutput = output;
            this.LogFilePath = logFilePath ?? this.LogFilePath;

            string AssemblyName = "Unknown";

            try
            {
                AssemblyName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            }
            catch (Exception ex)
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

            FileQueue = new AsyncStringWriter((s) => this.streamWriter.WriteLine(s));
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
        /// <param name="target">Specific target for this output. If null, uses instance default</param>
        public void WriteLine(object toLog, LogOutput? target = null)
        {
            //Time stamp it
            string prepend = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ";

            //Add the time stamp to the string
            string logString = $"{prepend} {this.SerializeObject(toLog)}";

            target = target ?? this.DefaultOutput;

            void CheckAndAdd(LogOutput flag, ConcurrentQueue<string> queue, BackgroundWorker worker)
            {
                if (target.Value.HasFlag(flag))
                {
                    bool wasEmpty = queue.IsEmpty;

                    queue.Enqueue(logString);

                    if (wasEmpty && !worker.IsBusy)
                    {
                        worker.RunWorkerAsync();
                    }
                }
            }

            if (target.Value.HasFlag(LogOutput.File))
            {
                //To the file
                this.FileQueue.Enqueue(logString);
            }

            if (target.Value.HasFlag(LogOutput.Debug))
            {
                DebugQueue.Enqueue(logString);
            }

            if (target.Value.HasFlag(LogOutput.Console))
            {
                ConsoleQueue.Enqueue(logString);
            }
        }

        public void Flush() => this.streamWriter.Flush();

        /// <summary>
        /// Disposes of the writer and flushes to disk
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    FileQueue.Dispose();
                    ConsoleQueue.Dispose();
                    DebugQueue.Dispose();

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

        private string SerializeObject(object toSerialize)
        {
            if (toSerialize is string s)
            {
                return s;
            }

            if (this.ObjectSerializationOverride is null)
            {
                return $"{toSerialize}";
            }

            switch (this.ObjectSerializationMethod)
            {
                case ObjectSerializationMethod.ToString:
                    return $"{toSerialize}";

                case ObjectSerializationMethod.Override:
                    return this.ObjectSerializationOverride(toSerialize);

                case ObjectSerializationMethod.Auto:
                    if (toSerialize is null)
                    {
                        return string.Empty;
                    }

                    MethodInfo mi = toSerialize.GetType().GetMethod("ToString");

                    return mi.DeclaringType == typeof(object) ? this.ObjectSerializationOverride(toSerialize) : $"{toSerialize}";

                default:
                    throw new NotImplementedException($"{nameof(this.ObjectSerializationMethod)} unimplemented value {this.ObjectSerializationMethod}");
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~LogWriter()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }
    }

    public enum ObjectSerializationMethod
    {
        /// <summary>
        /// Call ToString on objects to serialize
        /// </summary>
        ToString,

        /// <summary>
        /// Call ToString if overridden, otherwise call ObjectSerializationOverride
        /// </summary>
        Auto,

        /// <summary>
        /// Always call ObjectSerializationOverride
        /// </summary>
        Override
    }
}