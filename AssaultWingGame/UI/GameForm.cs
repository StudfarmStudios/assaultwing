using System;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Graphics;
using AW2.Menu;

namespace AW2.UI
{
    public partial class GameForm : Form, IWindow
    {
        private AWGameRunner _runner;

        public string Title
        {
            get { return Text; }
            set { BeginInvoke((Action)(() => Text = value)); }
        }
        public Rectangle ClientBounds { get { return new Rectangle(ClientRectangle.Left, ClientRectangle.Top, ClientRectangle.Width, ClientRectangle.Height); } }
        public Rectangle ClientBoundsMin
        {
            get { return new Rectangle(0, 0, MinimumSize.Width, MinimumSize.Height); }
            set { BeginInvoke((Action)(() => MinimumSize = new System.Drawing.Size(value.Width, value.Height))); }
        }
        public GraphicsDeviceControl GameView { get { return _gameView; } }

        public GameForm(string[] args)
        {
            InitializeComponent();
            GraphicsDeviceService.Instance.SetWindow(Handle);
            AssaultWingCore.MenuEngineInitializing += () => new MenuEngineImpl();
            AssaultWingCore.WindowInitializing += game => new AWGameWindow(this);
            AssaultWing.Instance = new AssaultWing();
            AssaultWingCore.Instance.CommandLineArgs = args;
            _runner = new AWGameRunner(AssaultWingCore.Instance,
                () => _gameView.BeginInvoke((Action)_gameView.Invalidate),
                gameTime => _gameView.BeginInvoke((Action)(() => AssaultWingCore.Instance.Update(gameTime))));
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            _runner.Run();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
//!!!            AssaultWingCore.Instance.MenuEngine.WindowResize();
//!!!            AssaultWingCore.Instance.DataEngine.RearrangeViewports();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Constants from windows.h
            const int WM_KEYDOWN = 0x100;
            const int WM_SYSKEYDOWN = 0x104;
            /*
            const int WM_KEYUP = 0x101;
            const int WM_CHAR = 0x102;
            const int WM_DEADCHAR = 0x103;
            const int WM_SYSKEYUP = 0x105;
            const int WM_SYSCHAR = 0x106;
            const int WM_SYSDEADCHAR = 0x107;
            */
            if (msg.Msg != WM_KEYDOWN && msg.Msg != WM_SYSKEYDOWN) throw new ArgumentException("Unexpected value " + msg.Msg);
            var keyCode = keyData & Keys.KeyCode;
            var modifiers = keyData & Keys.Modifiers;
            if (keyCode == Keys.PageUp) GraphicsDeviceService.Instance.SetFullScreen(1280, 1024);
            if (keyCode == Keys.PageDown) GraphicsDeviceService.Instance.SetWindowed(1000, 800);
            return true; // the message won't be processed further; prevents window menu from opening
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _runner.Exit();
            Application.DoEvents(); // finish processing BeginInvoke()d Update() and Draw() calls
            base.OnClosing(e);
        }
    }
}
