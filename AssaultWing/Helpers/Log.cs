using System;
using System.IO;

namespace AW2.Helpers
{
    /// <summary>
    /// Log will create automatically a log file and write
    /// log/error/debug info for simple runtime error checking, very useful
    /// for minor errors, such as not finding files.
    /// </summary>
    public class Log
    {
        #region Variables
        private static StreamWriter writer = null;
        private const string LogFilenameBase = "Log";
        private const string LogFilenameExtension = ".txt";
        private const int rotateCount = 5;
        #endregion

        /// <summary>
        /// Opens a new log file, rotating old ones.
        /// </summary>
        static Log()
        {
            try
            {
                // Rotate old logs.
                if (File.Exists(GetLogFilename(rotateCount)))
                    File.Delete(GetLogFilename(rotateCount));
                for (int rotation = rotateCount - 1; rotation >= 0; --rotation)
                    if (File.Exists(GetLogFilename(rotation)))
                        File.Move(GetLogFilename(rotation), GetLogFilename(rotation + 1));

                // Open a new log file.
                FileStream file = new FileStream(GetLogFilename(0), FileMode.OpenOrCreate,
                    FileAccess.Write, FileShare.ReadWrite);
                writer = new StreamWriter(file, System.Text.Encoding.UTF8);

                // Enable auto flush (always be up to date when reading!)
                writer.AutoFlush = true;
            }
            catch
            {
                // Ignore any file exceptions, if file is not
                // createable (e.g. on a CD-Rom) it doesn't matter.
            }

            // Add some info about this session.
            Write("Log opened. The date and time is " + DateTime.Now.ToString("o"));
        }

        /// <summary>
        /// Writes a LogType and info/error message string to the Log file
        /// </summary>
        static public void Write(string message)
        {
            // Can't continue without valid writer
            if (writer == null)
                return;

            try
            {
#if DEBUG
                string s = DateTime.Now.ToString("'['HH':'mm':'ss'.'fff'] '") + message;
#else
                string s = DateTime.Now.ToString("'['HH':'mm':'ss'] '") + message;
#endif
                writer.WriteLine(s);
#if DEBUG
                // In debug mode write that message to the console as well!
                System.Console.WriteLine(s);
#endif
            }
            catch
            {
                // Ignore any file exceptions, if file is not
                // writable (e.g. on a CD-Rom) it doesn't matter
            }
        }

        /// <summary>
        /// Returns the filename of the log that has been rotated a number of times.
        /// </summary>
        private static string GetLogFilename(int rotation)
        {
            if (rotation == 0)
                return string.Format("{0}{1}", LogFilenameBase, LogFilenameExtension);
            else
                return string.Format("{0}.{1}{2}", LogFilenameBase, rotation, LogFilenameExtension);
        }
    }
}
