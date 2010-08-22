using System;
using System.IO;

namespace AW2.Helpers
{
    /// <summary>
    /// Log of error, debug and informational messages.
    /// This class is thread safe.
    /// </summary>
    public static class Log
    {
        private static object g_lock = new object();
        private static StreamWriter g_writer = null;
        private const string FILE_BASENAME = "Log";
        private const string FILE_EXTENSION = ".txt";
        private const int ROTATE_COUNT = 5;

        /// <summary>
        /// Opens a new log file, rotating old ones.
        /// </summary>
        static Log()
        {
            try
            {
                // Rotate old logs.
                if (File.Exists(GetLogFilename(ROTATE_COUNT)))
                    File.Delete(GetLogFilename(ROTATE_COUNT));
                for (int rotation = ROTATE_COUNT - 1; rotation >= 0; --rotation)
                    if (File.Exists(GetLogFilename(rotation)))
                        File.Move(GetLogFilename(rotation), GetLogFilename(rotation + 1));
            }
            catch
            {
                // Ignore any file exceptions, if file is not
                // createable (e.g. on a CD-Rom) it doesn't matter.
            }

            try
            {
                // Open a new log file.
                FileStream file = new FileStream(GetLogFilename(0), FileMode.OpenOrCreate,
                    FileAccess.Write, FileShare.ReadWrite);
                g_writer = new StreamWriter(file, System.Text.Encoding.UTF8);

                // Enable auto flush (always be up to date when reading!)
                g_writer.AutoFlush = true;
            }
            catch
            {
                // Ignore any file exceptions, if file is not
                // createable (e.g. on a CD-Rom) it doesn't matter.
            }

            // Add some info about this session.
            Write("Log opened. The date and time is " + DateTime.Now.ToString("o"));
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
        }

        public static void Write(string message, Exception e)
        {
            Log.Write(string.Format("{0}:\n{1}\n{2}\n{1}", message, new string('-', 40), e));
        }

        /// <summary>
        /// Returns the filename of the log that has been rotated a number of times.
        /// </summary>
        private static string GetLogFilename(int rotation)
        {
            var filename = string.Format("{0}{1}{2}", FILE_BASENAME, rotation == 0 ? "" : "." + rotation, FILE_EXTENSION);
            return Path.Combine(LogPath, filename);
        }

        private static string LogPath
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
    }
}
