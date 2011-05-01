﻿using System;
using System.Drawing;
using System.Threading;
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
            public bool Visible { get; private set; }
            public bool ShowIcon { get; private set; }
            public bool TopMost { get; private set; }

            public FormParameters(FormWindowState windowState, FormBorderStyle borderStyle, Point location, Size size,
                bool visible, bool showIcon, bool topMost)
                : this()
            {
                WindowState = windowState;
                BorderStyle = borderStyle;
                Location = location;
                Size = size;
                Visible = visible;
                ShowIcon = showIcon;
                TopMost = topMost;
            }
        }

        private AssaultWing _game;
        private AWGameRunner _runner;
        private GraphicsDeviceService _graphicsDeviceService;
        private string[] _commandLineArgs;

        private bool _isFullScreen;
        private int _isChangingFullScreen;
        private FormParameters _previousWindowedModeParameters;
        private Icon _originalIcon;

        public Rectangle ClientBoundsMin
        {
            get { return new Rectangle(0, 0, MinimumSize.Width, MinimumSize.Height); }
            set { BeginInvoke((Action)(() => MinimumSize = new System.Drawing.Size(value.Width, value.Height))); }
        }
        public GraphicsDeviceControl GameView { get { return _gameView; } }

        public GameForm(string[] args)
        {
            _commandLineArgs = args;
            InitializeComponent();
            InitializeGameForm();
            InitializeGraphicsDeviceService(Handle);
            InitializeGame(_commandLineArgs);
            InitializeGameView();
            InitializeRunner();
        }

        public void FinishGame()
        {
            _runner.Exit();
            Application.DoEvents(); // finish processing BeginInvoke()d Update() and Draw() calls
        }

        public new void Dispose()
        {
            if (_game != null)
            {
                _game.Dispose();
                _game = null;
            }
            AW2.Helpers.Log.Written -= AddToLogView;
            base.Dispose();
        }

        public void SetWindowed()
        {
            if (_graphicsDeviceService == null) return;
            if (Interlocked.CompareExchange(ref _isChangingFullScreen, 1, 0) != 0) return;
            try
            {
                if (!_isFullScreen) return;
                _runner.Pause();
                Application.DoEvents();
                _isFullScreen = false;
                _graphicsDeviceService.SetWindowed();
                SetFormParameters(_previousWindowedModeParameters);
                _splitContainer.Visible = true;
                _runner.Resume();
            }
            finally
            {
                _isChangingFullScreen = 0;
            }
        }

        public void SetFullScreen(int width, int height)
        {
            if (_graphicsDeviceService == null) return;
            if (Interlocked.CompareExchange(ref _isChangingFullScreen, 1, 0) != 0) return;
            try
            {
                if (_isFullScreen && width == ClientSize.Width && height == ClientSize.Height) return;
                _runner.Pause();
                Application.DoEvents();
                if (!_isFullScreen) _previousWindowedModeParameters = GetCurrentFormParameters();
                _isFullScreen = true;
                _splitContainer.Visible = false;
                SetFormParameters(GetFullScreenFormParameters(width, height));
                _graphicsDeviceService.SetFullScreen(width, height);
                _runner.Resume();
            }
            finally
            {
                _isChangingFullScreen = 0;
            }
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            _runner.Run();
        }

        protected override void OnDeactivate(EventArgs e)
        {
            // Note: If GraphicsDevice is in fullscreen mode but Windows' focus is not on the game,
            // subsequent calls to Gob.LoadContent fail for some reason so that the loaded 3D model
            // is empty. (This happens even if the model is returned straight from the cache!) This
            // seems to be a "feature" in XNA itself, as it is reproducible in a pristine XNA project.
            if (_isFullScreen) SetWindowed();
            base.OnDeactivate(e);
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
            if (keyCode == Keys.PageUp) SetFullScreen(_game.Settings.Graphics.FullscreenWidth, _game.Settings.Graphics.FullscreenHeight); // HACK !!!
            if (keyCode == Keys.PageDown) SetWindowed(); // HACK !!!
            if (keyCode == Keys.F1) _splitContainer.Panel2Collapsed ^= true;
            return (keyCode >= Keys.F1 && keyCode <= Keys.F24)
                || (keyCode == Keys.Space && modifiers == Keys.Alt); // prevent window menu from opening
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            _game.Window.OnKeyPress(e.KeyChar);
            e.Handled = true;
            base.OnKeyPress(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            FinishGame();
            if (_graphicsDeviceService != null) _graphicsDeviceService.Dispose();
            _graphicsDeviceService = null;
            base.OnClosing(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            _graphicsDeviceService.CheckReentrancyBegin();
            try
            {
                base.OnPaint(e);
                if (!_isFullScreen) return;
                var beginDrawError = _graphicsDeviceService.BeginDraw(ClientSize, true);
                if (beginDrawError == null)
                {
                    _game.Draw();
                    _graphicsDeviceService.EndDraw(ClientSize, Handle);
                }
                else
                    GraphicsDeviceService.PaintUsingSystemDrawing(e.Graphics, Font, ClientRectangle, beginDrawError);
            }
            finally
            {
                _graphicsDeviceService.CheckReentrancyEnd();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // Doing nothing here is supposed to avoid flicker.
        }

        private void InitializeGameForm()
        {
            Size = MinimumSize; // Forms crops MinimumSize automatically down to screen size but not Size
            _previousWindowedModeParameters = GetCurrentFormParameters();
            _originalIcon = Icon;
            AW2.Helpers.Log.Written += AddToLogView;
        }

        private void InitializeGraphicsDeviceService(IntPtr windowHandle)
        {
            _graphicsDeviceService = new GraphicsDeviceService(windowHandle);
        }

        private void InitializeGame(string[] args)
        {
            _game = new AssaultWing(_graphicsDeviceService, new CommandLineOptions(args));
            AssaultWingCore.Instance = _game; // HACK: support older code that uses the static instance
            _game.Window = new Window(
                getTitle: () => Text,
                setTitle: text => BeginInvoke((Action)(() => Text = text)),
                getClientBounds: () => new Rectangle(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height),
                getFullScreen: () => _isFullScreen,
                setWindowed: () => BeginInvoke((Action)SetWindowed),
                setFullScreen: (width, height) => BeginInvoke((Action<int, int>)SetFullScreen, width, height),
                isVerticalSynced: () => _graphicsDeviceService.IsVerticalSynced,
                enableVerticalSync: () => BeginInvoke((Action)_graphicsDeviceService.EnableVerticalSync),
                disableVerticalSync: () => BeginInvoke((Action)_graphicsDeviceService.DisableVerticalSync));
            _gameView.Draw += _game.Draw;
            _gameView.Resize += (sender, eventArgs) => _game.DataEngine.RearrangeViewports();
        }

        private void InitializeGameView()
        {
            _gameView.GraphicsDeviceService = _graphicsDeviceService;
        }

        private void InitializeRunner()
        {
            _runner = new AWGameRunner(_game,
                invoker: action => Invoke(action),
                exceptionHandler: e => BeginInvoke((Action)(() => { throw new ApplicationException("An exception occurred in a background thread", e); })),
                draw: () =>
                {
                    if (_isFullScreen)
                        BeginInvoke((Action)Invalidate);
                    else
                        BeginInvoke((Action)_gameView.Invalidate);
                },
                update: gameTime => BeginInvoke((Action<AWGameTime>)_game.Update, gameTime));
        }

        private static FormParameters GetFullScreenFormParameters(int width, int height)
        {
            var screenArea = Screen.PrimaryScreen.Bounds;
            return new FormParameters(FormWindowState.Normal, FormBorderStyle.None, Point.Empty, new Size(width, height),
                visible: false, showIcon: false, topMost: false);
        }

        private FormParameters GetCurrentFormParameters()
        {
            return new FormParameters(WindowState, FormBorderStyle, Location, ClientSize,
                visible: true, showIcon: true, topMost: false);
        }

        private void SetFormParameters(FormParameters parameters)
        {
            WindowState = parameters.WindowState;
            FormBorderStyle = parameters.BorderStyle;
            Location = parameters.Location;
            ShowIcon = parameters.ShowIcon;
            Visible = parameters.Visible; // Control.set_Visible tampers with ClientSize
            TopMost = parameters.TopMost; // Control.set_TopMost tampers with ClientSize
            ClientSize = parameters.Size; // ...therefore set ClientSize last
        }

        private void AddToLogView(string text)
        {
            BeginInvoke((Action<string>)(_logView.AppendText), text + "\r\n");
        }
    }
}
