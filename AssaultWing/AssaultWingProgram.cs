using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Windows.Forms;
using AW2.Core;
using AW2.Helpers;
using AW2.UI;

namespace AW2
{
#if WINDOWS || XBOX
    public class AssaultWingProgram : IDisposable
    {
        private GameForm _form;
        private static string[] g_errorCaptions = new[]
        {
            "Oops, Assault Wing crashed!",
            "Wait... How did this happen?",
            "You found a bug, congratulations!"
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
            var version = "Assault Wing " + AssaultWing.Instance.Version;
            var dateTime = DateTime.Now.ToUniversalTime().ToString("u");
            var computer = Environment.MachineName;
            var errorInfo = e.ToString();
            var caption = g_errorCaptions[RandomHelper.GetRandomInt(g_errorCaptions.Length)];
            var intro = "Want to send this automatic error report to the developers to help solve the problem?";
            var header = string.Format("{0} {1}", dateTime, computer);
            var report = string.Format("{0}\n{1} {2}\n{3}", version, dateTime, computer, errorInfo);
            var result = MessageBox.Show(intro + "\n\n" + report, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Error);
            if (result == DialogResult.Yes) SendMail(header, report);
        }

        private static void SendMail(string header, string body)
        {
            Console.WriteLine("Initialising data");
            var mail = CreateBugMail(header, body);
            var smtpClient = CreateSMTPClient();
            Console.WriteLine("Sending mail");
            smtpClient.Send(mail);
            Console.WriteLine("Done!");
        }

        private static SmtpClient CreateSMTPClient()
        {
            // See http://mail.google.com/support/bin/answer.py?hl=en&answer=77662
            var smtpClient = new SmtpClient("smtp.gmail.com", 587); // works without SSL
            smtpClient.EnableSsl = false;
            smtpClient.Credentials = new NetworkCredential("assaultwing", "@2-VFpyrB#gr#");
            return smtpClient;
        }

        private static MailMessage CreateBugMail(string header, string body)
        {
            // Note: Gmail changes From address to what the Gmail user settings say.
            var mail = new MailMessage("aw-bug@gmail.com", "assaultwing@gmail.com");
            mail.Subject = "[BUG REPORT] " + header;
            mail.Body = body;
            return mail;
        }
    }
#endif
}
