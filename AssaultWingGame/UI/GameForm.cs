﻿using System;
using System.Drawing;
using System.Windows.Forms;
using AW2.Core;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace AW2.UI
{
    public partial class GameForm : Form
    {
        private struct FormParameters
        {
            public FormWindowState WindowState { get; private set; }
            public FormBorderStyle BorderStyle { get; private set; }
            public Point Location { get; private set; }
            public Size Size { get; private set; }

            public FormParameters(FormWindowState windowState, FormBorderStyle borderStyle, Point location, Size size)
                : this()
            {
                WindowState = windowState;
                BorderStyle = borderStyle;
                Location = location;
                Size = size;
            }
        }

        private AssaultWing _game;
        private AWGameRunner _runner;
        private GraphicsDeviceService _graphicsDeviceService;
        
        private bool _isFullScreen;
        private FormParameters _previousWindowedModeParameters;

        public Rectangle ClientBoundsMin
        {
            get { return new Rectangle(0, 0, MinimumSize.Width, MinimumSize.Height); }
            set { BeginInvoke((Action)(() => MinimumSize = new System.Drawing.Size(value.Width, value.Height))); }
        }
        public GraphicsDeviceControl GameView { get { return _gameView; } }

        public GameForm(string[] args)
        {
            InitializeComponent();
            InitializeGameForm();
            InitializeGraphicsDeviceService();
            InitializeGame(args);
            InitializeGameView();
            InitializeRunner();
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
#if !DEBUG
            SetFullScreen();
#endif
            _runner.Run();
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
            if (keyCode == Keys.PageUp) SetFullScreen(); // HACK !!!
            if (keyCode == Keys.PageDown) SetWindowed(); // HACK !!!
            if (keyCode == Keys.Oem5) splitContainer1.Panel2Collapsed ^= true; // the § key on a Finnish keyboard
            return true; // the message won't be processed further; prevents window menu from opening
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _runner.Exit();
            Application.DoEvents(); // finish processing BeginInvoke()d Update() and Draw() calls
            _graphicsDeviceService.Dispose();
            _graphicsDeviceService = null;
            base.OnClosing(e);
        }

        private void InitializeGameForm()
        {
            Size = MinimumSize; // Forms crops MinimumSize automatically down to screen size but not Size
            _previousWindowedModeParameters = GetCurrentFormParameters();
            AW2.Helpers.Log.Written += text => _logView.BeginInvoke((Action<string>)(_logView.AppendText), text + "\r\n");
        }

        private void InitializeGraphicsDeviceService()
        {
            _graphicsDeviceService = new GraphicsDeviceService(Handle);
        }

        private void InitializeGame(string[] args)
        {
            _game = new AssaultWing(_graphicsDeviceService);
            AssaultWing.Instance = _game; // HACK: support older code that uses the static instance
            _game.CommandLineArgs = args;
            _game.StatusTextChanged += text => BeginInvoke((Action)(() => Text = "Assault Wing " + text));
            _gameView.Draw += _game.Draw;
            _gameView.Resize += (sender, eventArgs) => _game.DataEngine.RearrangeViewports();
            _graphicsDeviceService.DeviceResetting += (sender, eventArgs) => _game.UnloadContent();
            _graphicsDeviceService.DeviceReset += (sender, eventArgs) => _game.LoadContent();
        }

        private void InitializeGameView()
        {
            _gameView.GraphicsDeviceService = _graphicsDeviceService;
        }

        private void InitializeRunner()
        {
            // FIXME: Game update delegate is run in the Forms thread only because Keyboard update won't work otherwise. This should be fixed later.
            _runner = new AWGameRunner(_game,
                () => _gameView.BeginInvoke((Action)_gameView.Invalidate),
                gameTime => _gameView.BeginInvoke((Action)(() => _game.Update(gameTime))));
        }

        private void SetWindowed()
        {
            if (!_isFullScreen) return;
            _isFullScreen = false;
            SetFormParameters(_previousWindowedModeParameters);
        }

        private void SetFullScreen()
        {
            if (_isFullScreen) return;
            _isFullScreen = true;
            _previousWindowedModeParameters = GetCurrentFormParameters();
            SetFormParameters(GetFullScreenFormParameters());
        }

        private static FormParameters GetFullScreenFormParameters()
        {
            var screenArea = Screen.PrimaryScreen.Bounds;
            return new FormParameters(FormWindowState.Normal, FormBorderStyle.None, Point.Empty, new Size(screenArea.Width, screenArea.Height));
        }

        private FormParameters GetCurrentFormParameters()
        {
            return new FormParameters(WindowState, FormBorderStyle, Location, ClientSize);
        }

        private void SetFormParameters(FormParameters parameters)
        {
            WindowState = parameters.WindowState;
            FormBorderStyle = parameters.BorderStyle;
            Location = parameters.Location;
            ClientSize = parameters.Size;
        }
    }
}
