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
        private AssaultWing _game;
        private AWGameRunner _runner;
        private GraphicsDeviceService _graphicsDeviceService;

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

        public GameForm(GraphicsDeviceService graphicsDeviceService, string[] args)
        {
            InitializeComponent();
            _graphicsDeviceService = graphicsDeviceService;
            _gameView.GraphicsDeviceService = graphicsDeviceService;
            graphicsDeviceService.SetWindow(Handle);

            // Make the device large enough for any conceivable purpose -- avoid unnecessary graphics device resets later
            var screen = Screen.GetWorkingArea(this);
            graphicsDeviceService.ResetDevice(screen.Width, screen.Height);

            AssaultWingCore.WindowInitializing += game => new AWGameWindow(this);
            _game = new AssaultWing(graphicsDeviceService);
            AssaultWing.Instance = _game; // HACK: support older code that uses the static instance
            _game.CommandLineArgs = args;
            graphicsDeviceService.DeviceResetting += (sender, eventArgs) => _game.UnloadContent();
            graphicsDeviceService.DeviceReset += (sender, eventArgs) => _game.LoadContent();
            // FIXME: Game update delegate is run in Forms thread only because Keyboard update won't work otherwise. This should be fixed later.
            _runner = new AWGameRunner(AssaultWingCore.Instance,
                () => _gameView.BeginInvoke((Action)_gameView.Invalidate),
                gameTime => _gameView.BeginInvoke((Action)(() => _game.Update(gameTime))));
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            _runner.Run();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_game != null)
            {
                _game.MenuEngine.WindowResize();
                _game.DataEngine.RearrangeViewports();
            }
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
            if (keyCode == Keys.PageUp) _graphicsDeviceService.SetFullScreen(1280, 1024);
            if (keyCode == Keys.PageDown) _graphicsDeviceService.SetWindowed(1000, 800);
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
