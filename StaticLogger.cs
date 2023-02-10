using Penguin.Extensions.Exceptions;
using System;
using System.Text;

namespace Penguin.Debugging
{
    /// <summary>
    /// A static class designed to be the target of debugging messages.
    /// Contains an endpoint that can be assigned on application start to retrieve any messages that are sent to this class
    /// Enabling this may greatly slow down the application but is useful for logging low level messages to diagnose problems
    /// </summary>
    public static class StaticLogger
    {
        private static readonly object ObjectLock = new();

        private static readonly StringBuilder Queue = new();

        /// <summary>
        /// This func should accept the string to be logged, and return whether or not the log was successful.
        /// If it returns false, the message queue will not be cleared. This is incase the message is logged before
        /// your output is ready to handle the message. The message is returned as a single new-line seperated string
        /// for speed
        /// </summary>
        public static Func<string, bool> Endpoint { get; set; }

        /// <summary>
        /// If this bool is false, theres no point in logging anything since its not going anywhere
        /// </summary>
        public static bool IsListening => Level != LoggingLevel.None;

        /// <summary>
        /// This should represent how often messages are flushed to the endpoint
        /// </summary>
        public static LoggingLevel Level { get; set; } = LoggingLevel.None;

        /// <summary>
        /// Represents the frequency that messages will be send to the logging endpoint
        /// </summary>
        public enum LoggingLevel
        {
            /// <summary>
            /// All messages are abandoned. Nothing is queued
            /// </summary>
            None = 0,

            /// <summary>
            /// Flushed messages should occur when an exception is logged
            /// </summary>
            Exception = 1,

            /// <summary>
            /// All messages are flushed to the end point as they are logged. For diagnosing issues within a logging method
            /// </summary>
            Call = 2,

            /// <summary>
            /// All messages are flushed to the end point when a method returns
            /// </summary>
            Method = 3,

            /// <summary>
            /// Flushed messages should only occur on finalization of a process, for when a process is succeeding but its state is not correct
            /// </summary>
            Final = 4,
        }

        /// <summary>
        /// Attempts to flush the message queue to the endpoint. returns false if the endpoint reports a failure
        /// </summary>
        /// <returns>false if the endpoint reports a failure</returns>
        public static bool Flush()
        {
            if (Endpoint is null)
            {
                return false;
            }

            bool Success = false;

            lock (ObjectLock)
            {
                Success = Endpoint.Invoke(Queue.ToString());

                if (Success)
                {
                    _ = Queue.Clear();
                }
            }

            return Success;
        }

        /// <summary>
        /// Queues a message to be sent to the endpoint
        /// </summary>
        /// <param name="toLog">The message to log</param>
        /// <param name="levelToLog">If the log level is higher (less frequent) than the application has set, it will cause the log to flush</param>
        public static void Log(string toLog, LoggingLevel levelToLog = LoggingLevel.Call)
        {
            if (!IsListening)
            {
                return;
            }

            lock (ObjectLock)
            {
                _ = Queue.Append(toLog);
                _ = Queue.Append(System.Environment.NewLine);
            }

            if (levelToLog >= Level)
            {
                _ = Flush();
            }
        }

        /// <summary>
        /// Logs an exception as an Exception level event. Logs stack trace and message recursively
        /// </summary>
        /// <param name="ex">The exception to log</param>
        /// <param name="levelToLog">Allows the level to be overridden</param>
        public static void Log(Exception ex, LoggingLevel levelToLog = LoggingLevel.Exception)
        {
            if (IsListening)
            {
                try
                {
                    Log(ex.RecursiveMessage(), levelToLog);
                    Log(ex.RecursiveStackTrace(), levelToLog);
                }
                catch (Exception iex)
                {
                    Log("An exception has been encountered while logging the previous exception", LoggingLevel.Exception);
                    Log(iex.Message, LoggingLevel.Exception);
                    Log(iex.StackTrace, LoggingLevel.Exception);
                }
            }
        }
    }
}