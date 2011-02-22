using System;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using AW2.Core;
using AW2.Helpers;
using AW2.UI;

namespace AW2
{
#if WINDOWS || XBOX
    public class AssaultWingProgram : IDisposable
    {
        private const string AW_BUG_REPORT_SERVER = "vs1164254.server4you.net";
        private const int AW_BUG_REPORT_PORT = 'A' * 256 + 'W';

        private GameForm _form;
        private static string[] g_errorCaptions = new[]
        {
            "Oops, Assault Wing crashed!",
            "Wait... How did this happen?",
            "You found a bug, congratulations!",
            "We're sorry for the inconvenience",
        };

        public static AssaultWingProgram Instance { get; private set; }

        [STAThread]
        public static void Main(string[] args)
        {
            using (Instance = new AssaultWingProgram(args))
            {
                Instance.Run();
            }
        }

        public AssaultWingProgram(string[] args)
        {
            Log.Write("Assault Wing started");
            Application.ThreadException += ThreadExceptionHandler;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            _form = new GameForm(args);
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
            _form.FinishGame();
            _form.SetWindowed();
            ReportException(e.Exception);
            Exit();
        }

        private static void ReportException(Exception e)
        {
            Log.Write("Assault Wing fatal error! Error details:\n" + e.ToString());
            var caption = g_errorCaptions[RandomHelper.GetRandomInt(g_errorCaptions.Length)];
            var intro = "Want to send this automatic error report to the developers to help solve the problem?";
            var report = string.Format("Assault Wing {0}\nCrashed at {1:u}\nHost {2}\n\n{3}",
                AssaultWing.Instance.Version, DateTime.Now.ToUniversalTime(), Environment.MachineName, e.ToString());
            var result = MessageBox.Show(intro + "\n\n" + report, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Error);
            if (result == DialogResult.Yes) SendMail(report);
        }

        private static void SendMail(string text)
        {
            var udpClient = new UdpClient();
            var data = Encoding.Default.GetBytes(text);
            udpClient.Send(data, data.Length, AW_BUG_REPORT_SERVER, AW_BUG_REPORT_PORT);
        }
    }
#endif
}