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
        public bool IsFullscreen { get { return GraphicsDeviceService.Instance.GraphicsDevice.PresentationParameters.IsFullScreen; } }
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
            AssaultWing.MenuEngineInitializing += () => new MenuEngineImpl();
            AssaultWing.WindowInitializing += game => new AWGameWindow(this);
            AssaultWing.Instance.CommandLineArgs = args;
            _runner = new AWGameRunner(AssaultWing.Instance,
                () => _gameView.BeginInvoke((Action)_gameView.Invalidate),
                gameTime => _gameView.BeginInvoke((Action)(() => AssaultWing.Instance.Update(gameTime))));
        }

        [Obsolete("This is duplicate with AWGameWindow.ToggleFullscreen()")]
        public void ToggleFullscreen()
        {
            GraphicsDeviceService.Instance.GraphicsDevice.PresentationParameters.IsFullScreen ^= true;
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            _runner.Run();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
#if false
            // Constants from windows.h
            const int WM_KEYDOWN = 0x100;
            const int WM_KEYUP = 0x101;
            const int WM_CHAR = 0x102;
            const int WM_DEADCHAR = 0x103;
            const int WM_SYSKEYDOWN = 0x104;
            const int WM_SYSKEYUP = 0x105;
            const int WM_SYSCHAR = 0x106;
            const int WM_SYSDEADCHAR = 0x107;
            switch (msg.Msg)
            {
                case WM_KEYDOWN: ...
                // Here we could update our internal keyboard state
            }
#endif
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
