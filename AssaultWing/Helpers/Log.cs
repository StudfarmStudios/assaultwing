#region Using directives
using System;
using System.IO;
using System.Collections;
using System.ComponentModel;
using System.Threading;
#endregion

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
        /// <summary>
        /// Writer
        /// </summary>
        private static StreamWriter writer = null;

        /// <summary>
        /// Log filename
        /// </summary>
        private const string LogFilename = "Log.txt";
        #endregion

        #region Static constructor to create log file
        /// <summary>
        /// Static constructor
        /// </summary>
        static Log()
        {
            try
            {
                // Open file
                FileStream file = new FileStream(LogFilename, FileMode.OpenOrCreate,
                    FileAccess.Write, FileShare.ReadWrite);

                // Check if file is too big (more than 2 MB),
                // in this case we just kill it and create a new one :)
                if (file.Length > 2 * 1024 * 1024)
                {
                    file.Close();
                    file = new FileStream(LogFilename, FileMode.Create,
                        FileAccess.Write, FileShare.ReadWrite );
                }
                // Associate writer with that, when writing to a new file,
                // make sure UTF-8 sign is written, else don't write it again!
                if (file.Length == 0)
                    writer = new StreamWriter(file,
                        System.Text.Encoding.UTF8);
                else
                    writer = new StreamWriter(file);

                // Go to end of file
                writer.BaseStream.Seek(0, SeekOrigin.End);

                // Enable auto flush (always be up to date when reading!)
                writer.AutoFlush = true;

                // Add some info about this session
                writer.WriteLine("");
                writer.WriteLine("/// Session started at: " +
                    StringHelper.WriteIsoDateAndTime(DateTime.Now));
                writer.WriteLine("///");
                writer.WriteLine("");
            } // try
            catch
            {
                // Ignore any file exceptions, if file is not
                // createable (e.g. on a CD-Rom) it doesn't matter.
            } // catch
        } // Log()
        #endregion

        #region Write log entry
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
                DateTime ct = DateTime.Now;
                string s = "[" + ct.Hour.ToString("00") + ":" +
                    ct.Minute.ToString("00") + ":" +
                    ct.Second.ToString("00") + "] " +
                    message;
                writer.WriteLine(s);

#if DEBUG
                // In debug mode write that message to the console as well!
                System.Console.WriteLine(s);
#endif
            } // try
            catch
            {
                // Ignore any file exceptions, if file is not
                // writable (e.g. on a CD-Rom) it doesn't matter
            } // catch
        } // Write(message)
        #endregion
    } // class Log
} // namespace AW2.Helpers
