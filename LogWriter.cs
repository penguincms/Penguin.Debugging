﻿using Penguin.FileStreams;
using Penguin.FileStreams.Interfaces;
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
        private static readonly AsyncStringWriter ConsoleQueue = new AsyncStringWriter((s) => Console.WriteLine(s));
        private static readonly AsyncStringWriter DebugQueue = new AsyncStringWriter((s) => Debug.WriteLine(s));
        private readonly AsyncStringWriter FileQueue;
        private readonly IFileWriter FileWriter;

        private readonly LogWriterSettings Settings;
        private bool disposedValue;

        /// <summary>
        /// The Directory that any log files are written to
        /// </summary>
        public string Directory => this.Settings.Directory;

        public string LogFileFullName => Path.Combine(this.Settings.Directory, this.LogFileName);

        /// <summary>
        /// Constructs a new instance of the log writer
        /// </summary>
        /// <summary>
        /// The name of the file the (if any) disk stream is being written to
        /// </summary>
        public string LogFileName { get; private set; } = $"{DateTime.Now:yyyyMMdd_HHmmss}_{AssemblyName}.log";

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
            this.Settings = settings ?? new LogWriterSettings();

            //Create output directory if it doesn't exist
            if (!System.IO.Directory.Exists(this.Settings.Directory))
            {
                System.IO.Directory.CreateDirectory(this.Settings.Directory);
            }

            this.FileWriter = FileWriterFactory.GetFileWriter(this.LogFileFullName, settings.Compression);

            this.FileQueue = new AsyncStringWriter((s) => this.FileWriter.WriteLine(s));
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
        /// Flushes the disk output streamwriter
        /// </summary>
        public void Flush()
        {
            this.FileWriter.Flush();
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

            target = target ?? this.Settings.OutputTarget;
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

        /// <summary>
        /// Disposes of the writer and flushes to disk
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                this.FileQueue.Dispose();
                ConsoleQueue.Dispose();
                DebugQueue.Dispose();

                this.FileWriter.Dispose();

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

            if (this.Settings.ObjectSerializationOverride is null)
            {
                return $"{toSerialize}";
            }

            switch (this.Settings.ObjectSerializationMethod)
            {
                case ObjectSerializationMethod.ToString:
                    return $"{toSerialize}";

                case ObjectSerializationMethod.Override:
                    return this.Settings.ObjectSerializationOverride(toSerialize);

                case ObjectSerializationMethod.Auto:
                    if (toSerialize is null)
                    {
                        return string.Empty;
                    }

                    MethodInfo mi = toSerialize.GetType().GetMethod("ToString");

                    return mi.DeclaringType == typeof(object) ? this.Settings.ObjectSerializationOverride(toSerialize) : $"{toSerialize}";

                default:
                    throw new NotImplementedException($"{nameof(this.Settings.ObjectSerializationMethod)} unimplemented value {this.Settings.ObjectSerializationMethod}");
            }
        }

        ~LogWriter()
        {
            this.Dispose(false);
        }
    }
}