using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using AW2.Core;
using AW2.Helpers;

using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Timer = System.Windows.Forms.Timer;
using System.Text;
using System.Runtime.InteropServices;

namespace AW2.UI
{
    public partial class GameForm : Form
    {
        // Constants from windows.h
        private const int WM_KEYDOWN = 0x100;
        private const int WM_CHAR = 0x102;
        private const int WM_SYSKEYDOWN = 0x104;
        /*
        private const int WM_KEYUP = 0x101;
        private const int WM_DEADCHAR = 0x103;
        private const int WM_SYSKEYUP = 0x105;
        private const int WM_SYSCHAR = 0x106;
        private const int WM_SYSDEADCHAR = 0x107;
        private const int WM_CUT = 0x300;
        private const int WM_COPY = 0x301;
        private const int WM_PASTE = 0x302;
        private const int WM_CLEAR = 0x303;
        private const int WM_UNDO = 0x304;
        */

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

        private bool _isFullScreen;
        private int _isChangingFullScreen;
        private Tuple<int, int> _pendingFullScreenSize;
        private FormParameters _previousWindowedModeParameters;
        private Icon _originalIcon;
        private bool _isCursorHidden;
        private bool _isCursorForcedVisible;

        private Timer _updateTimer;
        private StringBuilder _logCache;

        public Rectangle ClientBoundsMin
        {
            get { return new Rectangle(0, 0, MinimumSize.Width, MinimumSize.Height); }
            set { BeginInvoke((Action)(() => MinimumSize = value.GetSize())); }
        }
        public GraphicsDeviceControl GameView { get { return _gameView; } }
        public AssaultWing Game { get { return _game; } }
        private bool HasFocus { get { return Focused || _gameView.Focused || _logView.Focused || _splitContainer.Focused; } }

        public GameForm(CommandLineOptions commandLineOptions)
        {
            InitializeComponent();
            InitializeGame(Handle, commandLineOptions);
            InitializeGameForm();
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
            if (_game != null) _game.Dispose();
            _game = null;
            if (_updateTimer != null) _updateTimer.Dispose();
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
                if (!HasFocus)
                {
                    // Without focus we may lose the window completely. Wait for GotFocus event.
                    _pendingFullScreenSize = Tuple.Create(width, height);
                    return;
                }
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

        public void EnsureCursorHidden()
        {
            if (_isCursorForcedVisible) return;
            if (!_isCursorHidden) Cursor.Hide();
            _isCursorHidden = true;
        }

        public void EnsureCursorShown()
        {
            if (_isCursorHidden) Cursor.Show();
            _isCursorHidden = false;
        }

        public void ForceCursorShown()
        {
            _isCursorForcedVisible = true;
            EnsureCursorShown();
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            _runner.Run();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (msg.Msg != WM_KEYDOWN && msg.Msg != WM_SYSKEYDOWN) throw new ArgumentException("Unexpected value " + msg.Msg);
            var keyCode = keyData & Keys.KeyCode;
            var modifiers = keyData & Keys.Modifiers;
            if (keyCode == Keys.F5)
            {
                if (!_splitContainer.Panel1Collapsed)
                {
                    _splitContainer.Panel2Collapsed ^= true;
                    _logView.Select(Math.Max(0, _logView.Text.Length - 1), 0);
                    _logView.ScrollToCaret();
                }
            }
            return (keyCode >= Keys.F1 && keyCode <= Keys.F24 && modifiers == 0)
                || (keyCode == Keys.Space && modifiers == Keys.Alt); // prevent window menu from opening
        }

        protected override void WndProc(ref Message msg)
        {
            WndProcImpl(msg);
            base.WndProc(ref msg);
        }

        private void WndProcImpl(Message msg)
        {
            if (msg.Msg == WM_CHAR)
            {
                var chr = char.ConvertFromUtf32((int)msg.WParam)[0];
                _game.Window.OnKeyPress(chr);
            }
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

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // Doing nothing here is supposed to avoid flicker.
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null) components.Dispose();
                _updateTimer.Tick -= Update;
                _updateTimer.Stop();
            }
            base.Dispose(disposing);
        }

        private void InitializeGameForm()
        {
            Size = MinimumSize; // Forms crops MinimumSize automatically down to screen size but not Size
            _previousWindowedModeParameters = GetCurrentFormParameters();
            _originalIcon = Icon;
            AW2.Helpers.Log.Written += AddToLogView;
            _logCache = new StringBuilder();
            _updateTimer = new Timer { Interval = 1000 };
            _updateTimer.Tick += Update;
            _updateTimer.Start();

            // Text entry is handled by WndProcImpl() which is called at a keypress
            // only if this GameForm or _gameView has focus. Initially, this GameForm has
            // focus and we cannot set it to any other control in this method. Later, if
            // some other control has got the focus, it is no longer possible to focus
            // this GameForm. Also, clicking on _gameView doesn't automatically focus it,
            // so we set focus then explicitly. Note that clicking on other controls does
            // set focus to them.
            _gameView.Click += (sender, e) => _gameView.Focus();

            if (_game.CommandLineOptions.DedicatedServer) _splitContainer.Panel1Collapsed = true;
        }

        private void InitializeGame(IntPtr windowHandle, CommandLineOptions commandLineOptions)
        {
            if (!commandLineOptions.DedicatedServer) _graphicsDeviceService = new GraphicsDeviceService(windowHandle);
            _game = new AssaultWing(_graphicsDeviceService, commandLineOptions);
            AssaultWingCore.Instance = _game; // HACK: support older code that uses the static instance
            _game.Window = new Window(new Window.WindowImpl
            {
                GetTitle = () => Text,
                SetTitle = text => BeginInvoke((Action)(() => Text = text)),
                GetClientBounds = () => _isFullScreen ? ClientRectangle.ToXnaRectangle() : _gameView.ClientRectangle.ToXnaRectangle(),
                GetFullScreen = () => _isFullScreen,
                SetWindowed = () => BeginInvoke((Action)SetWindowed),
                SetFullScreen = (width, height) => BeginInvoke((Action<int, int>)SetFullScreen, width, height),
                IsVerticalSynced = () => _graphicsDeviceService.IsVerticalSynced,
                EnableVerticalSync = () => BeginInvoke((Action)_graphicsDeviceService.EnableVerticalSync),
                DisableVerticalSync = () => BeginInvoke((Action)_graphicsDeviceService.DisableVerticalSync),
                EnsureCursorHidden = () => BeginInvoke((Action)EnsureCursorHidden),
                EnsureCursorShown = () => BeginInvoke((Action)EnsureCursorShown),
            });
            _gameView.Draw += _game.Draw;
            _gameView.ExternalWndProc += WndProcImpl;
            _gameView.Resize += (sender, eventArgs) => _game.DataEngine.RearrangeViewports();
        }

        private void InitializeGameView()
        {
            _gameView.GraphicsDeviceService = _graphicsDeviceService;
            _gameView.GetClientSize = () => _game.Window.Impl.GetClientBounds().GetSize();
        }

        private void InitializeRunner()
        {
            _runner = new AWGameRunner(_game,
                invoker: action => Invoke(action),
                exceptionHandler: e => BeginInvoke((Action)(() => { throw new ApplicationException("An exception occurred in a background thread", e); })),
                updateAndDraw: gameTime => BeginInvoke((Action)(() =>
                {
                    _game.Update(gameTime);
                    if (!_runner.IsTimeForNextUpdate)
                        if (_isFullScreen)
                            Invalidate();
                        else
                            _gameView.Invalidate();
                    _runner.UpdateAndDrawFinished();
                })));
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
            if (_logView.IsDisposed) return;
            lock (_logCache) _logCache.Append(text).Append("\r\n");
        }

        private void Update(object sender, EventArgs args)
        {
            FinishPendingFullScreen();
            UpdateLogView();
        }

        private void FinishPendingFullScreen()
        {
            if (_pendingFullScreenSize == null || !HasFocus) return;
            var width = _pendingFullScreenSize.Item1;
            var height = _pendingFullScreenSize.Item2;
            _pendingFullScreenSize = null;
            SetFullScreen(width, height);
        }

        private void UpdateLogView()
        {
            if (_logCache.Length == 0) return;
            lock (_logCache)
            {
                _logView.AppendText(_logCache.ToString());
                _logCache.Clear();
            }
        }
    }
}
