using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LoggingUtils {
    /// <summary>
    /// A logger object to handle logging errors, warnings, and general debug information to files
    /// </summary>
    public class Logger {
        // At the top for static methods/fields
        private static char _separator = Path.DirectorySeparatorChar;
        public string LogDirectory { get; private set; }

        private static readonly int LogFileRetentionTimeInDays = 30;
        private static readonly int MaximumNumberOfLogs = 15;

        private readonly string OldLogsDirectory;

        private static Logger _logger = new Logger("Logs");


        /// <summary>
        /// Create a logger object that places logs in the application's relative directory. Moves existing logs in this
        /// location to a directory called oldLogs (if they exist)
        /// </summary>
        /// <param name="relativeDirectoryName">The name of the folder to place the logs in</param>
        public Logger(string relativeDirectoryName) {
            LogDirectory = $"{Directory.GetCurrentDirectory()}{_separator}{relativeDirectoryName}";
            OldLogsDirectory = $"{LogDirectory}{_separator}OldLogs";
            Initialize();
        }

        /// <summary>
        /// Creates a logger object that places logs in the exact directory given. Moves existing logs in this
        /// location to a directory called oldLogs (if they exist)
        /// </summary>
        /// <param name="absoluteDirectory">The absolute path of the folder to place logs in</param>
        public Logger(DirectoryInfo absoluteDirectory) {
            LogDirectory = absoluteDirectory.FullName;
            Initialize();
        }

        /// <summary>
        /// An initialize method for all constructors to call (to avoid duplicate code)
        /// </summary>
        private void Initialize() {
            if (Directory.Exists(LogDirectory)) {
                try {
                    MoveOldLogs();

                    if (Directory.Exists(OldLogsDirectory)) {
                        CleanOldLogs();
                    }
                } catch (Exception e) {
                    // Just throw the exception already given to us
                    throw e;
                }
            }

            Directory.CreateDirectory(LogDirectory);
            Application.logMessageReceived += WriteUncaughtException;
            _logger = this;
        }

        /// <summary>
        /// Write an informative log message for general purposes
        /// </summary>
        /// <param name="message">The text to write to the file</param>
        /// <returns>Always returns false (to be used in Exception handling)</returns>
        public static bool Log(string message) {
            WriteToLogFile(message, type: LogType.Log);
            Debug.Log(message);
            return false;
        }

        /// <summary>
        /// Write a warning
        /// </summary>
        /// <param name="message">The text to write to the file</param>
        /// <returns>Always returns false (to be used in Exception handling)</returns>
        public static bool Warning(string message) {
            WriteToLogFile(message, type: LogType.Warning);
            Debug.LogWarning(message);
            return false;
        }

        /// <summary>
        /// Write an error
        /// </summary>
        /// <param name="message">The text to write to the file</param>
        /// <returns>Always returns false (to be used in Exception handling)</returns>
        public static bool Error(string message) {
            WriteToLogFile(message, type: LogType.Error);
            Debug.LogError(message);
            return false;
        }

        /// <summary>
        /// Cleans up the old logs if they're older than the retention rate or if
        /// there are more than the maximum allowed
        /// </summary>
        private void CleanOldLogs() {
            string[] directories = Directory.GetDirectories(OldLogsDirectory);

            for (int i = 0; i < directories.Length; i++) {
                if (Directory.GetCreationTime(directories[i])
                             .AddDays(LogFileRetentionTimeInDays)
                             .CompareTo(DateTime.Now) < 0) {
                    Directory.Delete(directories[i], true);
                }
            }

            DirectoryInfo dirInfo = new DirectoryInfo(OldLogsDirectory);
            int directoryCount = dirInfo.GetDirectories().Length;

            if (directoryCount > MaximumNumberOfLogs) {
                var directoriesToDelete = (from dir in dirInfo.GetDirectories()
                                           orderby dir.CreationTime
                                           select dir)
                                          .Take(directoryCount - MaximumNumberOfLogs);

                for (int i = directoriesToDelete.Count() - 1; i >= 0; i--) {
                    directoriesToDelete.ElementAt(i).Delete(true);
                }
            }
        }

        /// <summary>
        /// If there are log files, move them to an old directory
        /// </summary>
        private void MoveOldLogs() {
            // Backup the old log directory if it exists and contains files in it
            if (Directory.GetFiles(LogDirectory).Length > 0) {
                string newOldLogDirectory = $"{OldLogsDirectory}{_separator}{Path.GetFileName(LogDirectory)}.{DateTime.Now.ToFileTime()}";
                Directory.CreateDirectory(newOldLogDirectory);

                foreach (string file in Directory.GetFiles(LogDirectory)) {
                    File.Move(file, $"{newOldLogDirectory}{_separator}{Path.GetFileName(file)}");
                }
            }
        }

        /// <summary>
        /// Write the actual output message to the given log based on the type
        /// </summary>
        /// <param name="message">The message to write</param>
        /// <param name="type">The type of log (what the filename will be)</param>
        private static void WriteToLogFile(string message, LogType type) {
            File.AppendAllText($"{_logger.LogDirectory}{_separator}{type.ToString()}.txt", $"{DateTime.Now} --- {message} {Environment.NewLine}");
        }

        /// <summary>
        /// Write the actual output message to the given log based on the type
        /// </summary>
        /// <param name="condition">The type of exception being caught</param>
        /// <param name="stackTrace">The stack trace</param>
        /// <param name="type">The type of log to write</param>
        private void WriteUncaughtException(string condition, string stackTrace, LogType type) {
            File.AppendAllText($"{_logger.LogDirectory}{_separator}{type.ToString()}.txt", $"{DateTime.Now} --- {condition} {(string.IsNullOrEmpty(stackTrace) ? "No Stacktrace" : stackTrace)} {Environment.NewLine}");
        }
    }
}