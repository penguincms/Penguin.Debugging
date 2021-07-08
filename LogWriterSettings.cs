using Penguin.FileStreams;
using System;

namespace Penguin.Debugging
{
    /// <summary>
    /// Class containing the settings to be used for an instance of the log writer
    /// </summary>
    public class LogWriterSettings
    {
        /// <summary>
        /// Compression to use when writing log files to disk
        /// </summary>
        public FileStreamCompression Compression { get; set; } = FileStreamCompression.None;

        /// <summary>
        /// Output path for the log files. Defaults to "Logs"
        /// </summary>
        public string Directory { get; set; } = "Logs";

        /// <summary>
        /// The method used to serialized non-string objects. defaults to Auto
        /// </summary>
        public ObjectSerializationMethod ObjectSerializationMethod { get; set; } = ObjectSerializationMethod.Auto;

        /// <summary>
        /// An optional method used to override object serialization if additional logic is required (ex json serialization)
        /// </summary>
        public Func<object, string> ObjectSerializationOverride { get; set; }

        /// <summary>
        /// Location that the log lines should be written to. Defaults to "All"
        /// </summary>
        public LogOutput OutputTarget { get; set; } = LogOutput.All;
    }
}