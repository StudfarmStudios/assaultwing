using System;
using System.Threading;
using AW2.Core;
using AW2.Helpers;
using AW2.UI;

namespace AW2
{
    public class AssaultWingProgram : IDisposable
    {
        private static CommandLineOptions g_commandLineOptions;
        private static string[] g_errorCaptions = new[]
        {
            "Oops, Assault Wing crashed!",
            "Wait... How did this happen?",
            "You found a bug, congratulations!",
            "We're sorry for the inconvenience",
        };
        private GameForm _form;
        private static bool g_reportingException;

        public static AssaultWingProgram Instance { get; private set; }

        [STAThread]
        public static void Main(string[] args)
        {
            Thread.CurrentThread.Name = "MainThread";

            g_commandLineOptions = new CommandLineOptions(Environment.GetCommandLineArgs());

            using (Instance = new AssaultWingProgram())
            {
                Instance.Run();
            }
        }

        public AssaultWingProgram()
        {
            Log.Write("Assault Wing started");
            _form = new GameForm(g_commandLineOptions);
        }

        public void Run()
        {
            _form.Run();

        }

        public void Exit()
        {
            _form.Close();
        }

        public void Dispose()
        {
            if (_form != null) _form.Dispose();
            _form = null;
        }

        private void ThreadExceptionHandler(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            _form.FinishGame();
            ReportException(e.Exception);
            Exit();
        }

        private static void ReportException(Exception e)
        {
            Log.Write("Assault Wing fatal error! Error details:\n" + e.ToString());
            if (g_reportingException) return; // Don't report if sending the report failed.
            g_reportingException = true;
            var caption = g_errorCaptions[RandomHelper.GetRandomInt(g_errorCaptions.Length)];
            var intro = "Would you please help solve the problem by sending the developers this error information" +
                " and the Assault Wing run log \"" + Log.LogFileName + "\"?";
            var report = string.Format("Assault Wing {0}\nCrashed on {1:u}\nHost {2}\n\n{3}",
                MiscHelper.Version, DateTime.Now.ToUniversalTime(), Environment.MachineName, e.ToString());

            g_reportingException = false;
        }
    }
}
