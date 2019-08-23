using System;
using System.Collections.Generic;
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
        static StaticLogger()
        {
            Level = LoggingLevel.None;
            Queue = new StringBuilder();
        }

        static StringBuilder Queue { get; set; }
        /// <summary>
        /// This func should accept the string to be logged, and return whether or not the log was successful.
        /// If it returns false, the message queue will not be cleared. This is incase the message is logged before 
        /// your output is ready to handle the message. The message is returned as a single new-line seperated string
        /// for speed
        /// </summary>
        public static Func<string, bool> Endpoint { get; set; }

        /// <summary>
        /// This should represent how often messages are flushed to the endpoint
        /// </summary>
        public static LoggingLevel Level { get; set; }

        /// <summary>
        /// Queues a message to be sent to the endpoint
        /// </summary>
        /// <param name="toLog">The message to log</param>
        /// <param name="levelToLog">If the log level is higher (less frequent) than the application has set, it will cause the log to flush</param>
        public static void Log(string toLog, LoggingLevel levelToLog)
        {
            if(Level == LoggingLevel.None)
            {
                return;
            }

            Queue.Append(toLog);
            Queue.Append(System.Environment.NewLine);
            if(levelToLog >= Level)
            {
                Flush();
            }
        }

        /// <summary>
        /// Attempts to flush the message queue to the endpoint. returns false if the endpoint reports a failure
        /// </summary>
        /// <returns>false if the endpoint reports a failure</returns>
        public static bool Flush()
        {
            if(Endpoint is null)
            {
                return false;
            }

            bool Success = Endpoint.Invoke(Queue.ToString());

            if(Success)
            {
                Queue.Clear();
            }

            return Success;
        }

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
            /// All messages are flushed to the end point as they are logged. For diagnosing issues within a logging method
            /// </summary>
            Call = 1,

            /// <summary>
            /// All messages are flushed to the end point when a method returns
            /// </summary>
            Method = 2,

            /// <summary>
            /// Flushed messages should only occur on finalization of a process, for when a process is succeeding but its state is not correct
            /// </summary>
            Final = 3
        }
    }
}
