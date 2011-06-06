using System;
using System.Linq;
using System.IO;

namespace AW2.Helpers
{
    /// <summary>
    /// Log of error, debug and informational messages.
    /// This class is thread safe.
    /// </summary>
    public static class Log
    {
        private const string FILE_BASENAME = "Log";
        private const string FILE_EXTENSION = ".txt";
        private const string LOG_DATETIME_FORMAT = "yyyyMMddTHHmmss";
        private static readonly TimeSpan LOG_LIFETIME = TimeSpan.FromDays(10);

        private static object g_lock = new object();
        private static StreamWriter g_writer = null;
        private static DateTime g_logOpenDateTime = DateTime.Now;

        /// <summary>
        /// Triggered when something has been written to the log.
        /// </summary>
        public static event Action<string> Written;

        /// <summary>
        /// Opens a new log file, rotating old ones.
        /// </summary>
        static Log()
        {
            OpenNewLog();
            Write("Log opened. The date and time is " + DateTime.Now.ToString("o"));
            DeleteOldLogs();
        }

        public static void Write(string format, params object[] args)
        {
            Write(string.Format(format, args));
        }

        public static void Write(string message)
        {
#if DEBUG
            string s = DateTime.Now.ToString("'['HH':'mm':'ss'.'fff'] '") + message;
#else
            string s = DateTime.Now.ToString("'['HH':'mm':'ss'] '") + message;
#endif
            lock (g_lock)
            {
                if (g_writer != null)
                    try
                    {
                        g_writer.WriteLine(s);
                    }
                    catch (IOException)
                    {
                        // Couldn't write to log file, cannot help it!
                    }
#if DEBUG
                Console.WriteLine(s);
#endif
            }
            if (Written != null) Written(s);
        }

        public static void Write(string message, Exception e)
        {
            Write(string.Format("{0}:\n{1}\n{2}\n{1}", message, new string('-', 40), e));
        }

        public static string CloseAndGetContents()
        {
            g_writer.Close();
            return File.ReadAllText(LogFileName);
        }

        public static string LogFileName
        {
            get
            {
                var filename = string.Format("{0}{1:" + LOG_DATETIME_FORMAT + "}{2}", FILE_BASENAME, g_logOpenDateTime, FILE_EXTENSION);
                return Path.Combine(LogPath, filename);
            }
        }

        public static string LogPath
        {
            get
            {
#if DEBUG
                // Debug build uses always exe directory to avoid several running
                // Assault Wing instances messing up each others' log files.
                return ExeDirectory;
#else
                try
                {
                    var tempDir = Environment.GetEnvironmentVariable("TEMP");
                    if (tempDir == null) return ExeDirectory;
                    var path = Path.Combine(tempDir, "AssaultWing");
                    Directory.CreateDirectory(path);
                    return path;
                }
                catch (Exception)
                {
                    return ExeDirectory;
                }
#endif
            }
        }

        private static string ExeDirectory
        {
            get { return Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location); }
        }

        private static void DeleteOldLogs()
        {
            Func<string, DateTime> tryParseDateTime = dateStr =>
            {
                try
                {
                    return DateTime.ParseExact(dateStr, LOG_DATETIME_FORMAT, System.Globalization.CultureInfo.InvariantCulture);
                }
                catch
                {
                    return DateTime.MinValue;
                }
            };
            try
            {
                var logsRaw = (
                    from log in Directory.GetFiles(LogPath, FILE_BASENAME + "*" + FILE_EXTENSION)
                    let baseName = Path.GetFileNameWithoutExtension(log)
                    let logDateStr = baseName.Substring(FILE_BASENAME.Length, baseName.Length - FILE_BASENAME.Length)
                    let logDate = tryParseDateTime(logDateStr)
                    where logDate != DateTime.MinValue
                    select new { log, logDate }
                    ).ToArray();
                var logs =
                    from log in logsRaw
                    orderby log.logDate descending
                    select new { FileName = log.log, DateTime = log.logDate };
                if (!logs.Any()) return;
                var logsToDelete = logs.Skip(10).Union(logs.SkipWhile(log => log.DateTime > g_logOpenDateTime - LOG_LIFETIME));
                foreach (var log in logsToDelete)
                {
                    File.Delete(log.FileName);
                    Write("Deleted old log file \"{0}\"", log.FileName);
                }
            }
            catch
            {
                // Failed to delete old logs. Not too serious.
            }
        }

        private static void OpenNewLog()
        {
            try
            {
                var file = new FileStream(LogFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                g_writer = new StreamWriter(file, System.Text.Encoding.UTF8);
                g_writer.AutoFlush = true;
            }
            catch
            {
                // Failed to open log. Not too serious.
            }
        }
    }
}
