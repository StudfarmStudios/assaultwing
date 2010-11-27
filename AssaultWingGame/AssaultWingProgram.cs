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
            ReportException(e.Exception);
            Exit();
        }

        private static void ReportException(Exception e)
        {
            Log.Write("Assault Wing fatal error! Error details:\n" + e.ToString());
            string dateTime = DateTime.Now.ToUniversalTime().ToString("u");
            string computer = Environment.MachineName;
            string errorInfo = e.ToString();
            string caption = "Oops, something went wrong!";
            string intro = "Please send an automatic error report to the developers. "
                + "The contents of the report are below.";
            string header = string.Format("{0} {1}", dateTime, computer);
            string report = string.Format("{0} {1}\n{2}", dateTime, computer, errorInfo);
            var result = MessageBox.Show(intro + "\n\n" + report, caption, MessageBoxButtons.YesNo);
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
            var mail = new MailMessage("aw-bug@gmail.com", "info@assaultwing.com");
            mail.Subject = "[BUG REPORT] " + header;
            mail.Body = body;
            return mail;
        }
    }
#endif
}
