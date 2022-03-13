using System;
using System.Threading;
using AW2.Core;
using AW2.Helpers;

using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Game = Microsoft.Xna.Framework.Game;
using Microsoft.Xna.Framework;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace AW2.UI
{
    public partial class GameForm : Microsoft.Xna.Framework.Game
    {

        private AssaultWing _game;
        private GraphicsDeviceManager _graphics;

        private bool _isFullScreen;
        private int _isChangingFullScreen;
        private Tuple<int, int> _pendingFullScreenSize;
        private bool _isCursorHidden;
        private bool _isCursorForcedVisible;

        private Stopwatch _stopWatch = new Stopwatch();
        private StringBuilder _logCache;

        public AssaultWing Game { get { return _game; } }

        public GameForm(CommandLineOptions commandLineOptions) : base()
        {
            _graphics = new GraphicsDeviceManager(this);

            Services.AddService(_graphics);
            Services.AddService(Content); // ContentMananger
            InitializeGame(commandLineOptions);
            InitializeGameForm();
        }

        public void FinishGame()
        {
        }

        public new void Dispose()
        {
            if (_game != null)
            {
                _game.UnloadContent();
                _game.Dispose();
            }
            _game = null;
            _stopWatch.Stop();
            AW2.Helpers.Log.Written -= AddToLogView;
            base.Dispose();
        }
        public void SetWindowed()
        {
            // TODO: Peter: Do we need this?

        }
        public void Close() {
            // TODO: Peter: do we need this?
        }
        public void ForceCursorShown()
        {
            _isCursorForcedVisible = true;
            // TODO: Peter? Do we need this?
        }
        

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stopWatch.Stop();
            }            
            base.Dispose(disposing);
        }

        private void InitializeGameForm()
        {
            _stopWatch.Start();
            AW2.Helpers.Log.Written += AddToLogView;
            _logCache = new StringBuilder();
        }

        private void InitializeGame(CommandLineOptions commandLineOptions)
        {
            //if (!commandLineOptions.DedicatedServer) _graphicsDeviceService = new GraphicsDeviceService(windowHandle);
            _game = new AssaultWing(Services, commandLineOptions);
            AssaultWingCore.Instance = _game; // HACK: support older code that uses the static instance
            _game.Window = new Window(new Window.WindowImpl
            {
                GetTitle = () => {return "foo";}, // () => Text,
                SetTitle = (Action<string>)((text) => {}), //text => BeginInvoke((Action)(() => Text = text)),
                GetClientBounds = () => { return Window.ClientBounds; }, // _isFullScreen ? ClientRectangle.ToXnaRectangle() : _gameView.ClientRectangle.ToXnaRectangle(),
                GetFullScreen = () => _graphics.IsFullScreen,
                SetWindowed = (Action)(() => {if(_graphics.IsFullScreen) {_graphics.ToggleFullScreen();}}), //() => BeginInvoke((Action)SetWindowed),
                SetFullScreen = (Action<int, int>)((width, height) => {if(!_graphics.IsFullScreen) {_graphics.ToggleFullScreen();}}), //(width, height) => BeginInvoke((Action<int, int>)SetFullScreen, width, height),
                IsVerticalSynced = () => _graphics.SynchronizeWithVerticalRetrace,
                EnableVerticalSync = (Action)(() => {}), // BeginInvoke((Action)_graphicsDeviceService.EnableVerticalSync),
                DisableVerticalSync = (Action)(() => {}),// () => BeginInvoke((Action)_graphicsDeviceService.DisableVerticalSync),
                EnsureCursorHidden = (Action)(() => {}),//() => BeginInvoke((Action)EnsureCursorHidden),
                EnsureCursorShown = (Action)(() => {}),//() => BeginInvoke((Action)EnsureCursorShown),
            });
            
            //_gameView.Draw += _game.Draw;
            //_gameView.ExternalWndProc += WndProcImpl;
            //_gameView.Resize += (sender, eventArgs) => _game.DataEngine.RearrangeViewports();
        }

        private bool _gameInitialized = false;
        protected override void Update(GameTime gameTime)
        {
            if (!_gameInitialized)
            {
                _graphics.PreferredBackBufferWidth = 1920;
                _graphics.PreferredBackBufferHeight = 1080;
                _graphics.ApplyChanges();

                _gameInitialized = true;
                _game.Initialize();
                _game.BeginRun();
            }

            if (_gameInitialized)
            {
                var awGameTime = new AWGameTime(gameTime.TotalGameTime, gameTime.ElapsedGameTime, _stopWatch.Elapsed);

                _game.Update(awGameTime);
                // TODO: PETER: 2nd call is a hack as the game code assumes certain cycle between updates and draws and we are just being too simplistic here
                _game.Update(awGameTime); 

                _game.Draw();

                base.Update(gameTime);
            }
        }


        private void AddToLogView(string text)
        {
            lock (_logCache) _logCache.Append(text).Append("\r\n");
        }

    }
}
