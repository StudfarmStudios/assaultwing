using System;
using System.Deployment.Application;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using AW2.Core;
using AW2.Helpers;
using AW2.UI;

namespace AW2
{
#if WINDOWS || XBOX
    public class AssaultWingProgram : IDisposable
    {
        private const string AW_BUG_REPORT_SERVER = "assaultwing.com";
        private const int AW_BUG_REPORT_PORT = 'A' * 256 + 'W' - 1;

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
            if (MiscHelper.IsNetworkDeployed) Log.Write("Activation URI = '{0}'", ApplicationDeployment.CurrentDeployment.ActivationUri);
            g_commandLineOptions = new CommandLineOptions(Environment.GetCommandLineArgs(), MiscHelper.QueryParams, AssaultWingCore.GetArgumentText());
            PostInstall.EnsureDone();
            AccessibilityShortcuts.ToggleAccessibilityShortcutKeys(returnToStarting: false);
            try
            {
                using (Instance = new AssaultWingProgram())
                {
                    Instance.Run();
                }
            }
            finally
            {
                AccessibilityShortcuts.ToggleAccessibilityShortcutKeys(returnToStarting: true);
            }
        }

        public AssaultWingProgram()
        {
            Log.Write("Assault Wing started");
            Application.ThreadException += ThreadExceptionHandler;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            _form = new GameForm(g_commandLineOptions);
        }

        public void Run()
        {
            Application.Run(_form);
        }

        public void Exit()
        {
            _form.Close();
        }

        public void Dispose()
        {
            if (_form != null)
            {
                _form.Dispose();
                _form = null;
            }
            Application.ThreadException -= ThreadExceptionHandler;
        }

        private void ThreadExceptionHandler(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            _form.ForceCursorShown();
            _form.FinishGame();
            _form.SetWindowed();
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
            var result = g_commandLineOptions.DedicatedServer
                ? DialogResult.Yes
                : MessageBox.Show(intro + "\n\n" + report, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Error);
            var logHeader = "\n\n*** Assault Wing run log ***\n\n";
            if (result == DialogResult.Yes) SendMail(report + logHeader + Log.CloseAndGetContents());

            // Raise a Windows event to notify any dedicated server keepalive task to relaunch the server.
            System.Diagnostics.EventLog.WriteEvent("Application Error", new System.Diagnostics.EventInstance(1000, 2), "AssaultWing.exe");

            g_reportingException = false;
        }

        private static void SendMail(string text)
        {
            var tcpClient = new TcpClient();
            tcpClient.Connect(AW_BUG_REPORT_SERVER, AW_BUG_REPORT_PORT);
            var data = Encoding.UTF8.GetBytes(text);
            var tcpStream = tcpClient.GetStream();
            tcpStream.Write(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(data.Length)), 0, sizeof(int));
            tcpStream.Write(data, 0, data.Length);
            tcpClient.Close();
        }
    }
#endif
}
