using Penguin.FileStreams;
using Penguin.FileStreams.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Penguin.Debugging
{
    /// <summary>
    /// Logs everything, everywhere
    /// </summary>
    public class LogWriter : IDisposable
    {
        private static void ConsoleWriteLine(string s)
        {
            Console.WriteLine(s);
        }

        private static void DebugWriteLine(string s)
        {
            Debug.WriteLine(s);
        }

        private static readonly AsyncStringWriter ConsoleQueue = new(ConsoleWriteLine);
        private static readonly AsyncStringWriter DebugQueue = new(DebugWriteLine);
        private AsyncStringWriter FileQueue;
        private IFileWriter FileWriter;

        private readonly LogWriterSettings Settings;

        private bool disposedValue;

        /// <summary>
        /// The Directory that any log files are written to
        /// </summary>
        public string Directory => Settings.Directory;

        /// <summary>
        /// Log file full path and file name
        /// </summary>
        public string LogFileFullName => Path.Combine(Settings.Directory, LogFileName);

        /// <summary>
        /// Constructs a new instance of the log writer
        /// </summary>
        /// <summary>
        /// The name of the file the (if any) disk stream is being written to
        /// </summary>
        public string LogFileName { get; private set; } = $"{DateTime.Now:yyyyMMdd_HHmmss}_{AssemblyName}.log";

        /// <summary>
        /// When writing to the debug window, the name of the output that will be used.
        /// </summary>
        public string DebugCategory
        {
            get => _debugCategory ?? LogFileName;
            set => _debugCategory = value;
        }

        private string _debugCategory;

        private static string AssemblyName
        {
            get
            {
                string toReturn = "Unknown";

                try
                {
                    toReturn = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                return toReturn;
            }
        }

        /// <summary>
        /// Constructs a new instance of the log file writer with the given settings
        /// </summary>
        /// <param name="settings">Any settings to overwrite from default</param>
        public LogWriter(LogWriterSettings settings = null)
        {
            Settings = settings ?? new LogWriterSettings();

            LogFileName = Settings.LogFileName ?? LogFileName;

            //Create output directory if it doesn't exist
            if (!System.IO.Directory.Exists(Settings.Directory))
            {
                _ = System.IO.Directory.CreateDirectory(Settings.Directory);
            }

            if (Settings.OutputTarget.HasFlag(LogOutput.File))
            {
                InitFileQueue();
            }

            //https://stackoverflow.com/questions/18020861/how-to-get-notified-before-static-variables-are-finalized
            //Catch domain shutdown (Hack: frantically look for things we can catch)
            if (AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                AppDomain.CurrentDomain.ProcessExit += MyTerminationHandler;
            }
            else
            {
                AppDomain.CurrentDomain.DomainUnload += MyTerminationHandler;
            }
        }

        private void MyTerminationHandler(object sender, EventArgs e)
        {
            Dispose();
        }

        private void InitFileQueue()
        {
            FileWriter = FileWriterFactory.GetFileWriter(LogFileFullName, Settings.Compression);
            FileQueue = new AsyncStringWriter(FileWriter.WriteLine);
        }

        /// <summary>
        /// Disposes of the writer and flushes to disk
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Flushes the disk output streamwriter
        /// </summary>
        public Task Flush()
        {
            return Task.Run(() =>
            {
                if (FileQueue != null)
                {
                    _ = FileQueue.FlushGate.WaitOne();
                    FileWriter?.Flush();
                }
            });
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
            string logString = $"{prepend} {SerializeObject(toLog)}";

            target ??= Settings.OutputTarget;
            if (target.Value.HasFlag(LogOutput.File))
            {
                if (FileQueue is null)
                {
                    InitFileQueue();
                }

                //To the file
                FileQueue.Enqueue(logString);
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

        /// <summary>
        /// Disposes of the writer and flushes to disk
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;

                FileQueue?.Dispose();
                FileWriter?.Dispose();
                ConsoleQueue.Dispose();
                DebugQueue.Dispose();
            }
            else
            {
                Debug.WriteLine($"Attempting to dispose of already disposed {typeof(LogWriter)}");
            }
        }

        private string SerializeObject(object toSerialize)
        {
            if (toSerialize is string s)
            {
                return s;
            }

            if (Settings.ObjectSerializationOverride is null)
            {
                return $"{toSerialize}";
            }

            switch (Settings.ObjectSerializationMethod)
            {
                case ObjectSerializationMethod.ToString:
                    return $"{toSerialize}";

                case ObjectSerializationMethod.Override:
                    return Settings.ObjectSerializationOverride(toSerialize);

                case ObjectSerializationMethod.Auto:
                    if (toSerialize is null)
                    {
                        return string.Empty;
                    }

                    MethodInfo mi = toSerialize.GetType().GetMethod("ToString");

                    return mi.DeclaringType == typeof(object) ? Settings.ObjectSerializationOverride(toSerialize) : $"{toSerialize}";

                default:
                    throw new NotImplementedException($"{nameof(Settings.ObjectSerializationMethod)} unimplemented value {Settings.ObjectSerializationMethod}");
            }
        }
    }
}