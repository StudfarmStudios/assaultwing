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
        public Rectangle ClientBounds { get { return new Rectangle(Bounds.Left, Bounds.Top, Bounds.Width, Bounds.Height); } }
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
                () => _gameView.BeginInvoke((Action)_gameView.Invalidate), // TODO !!! exception on exit: BeginInvoke cannot be called when Handle hasn't been created
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

        protected override void OnClosed(EventArgs e)
        {
            _runner.Exit();
            base.OnClosed(e);
        }
    }
}
