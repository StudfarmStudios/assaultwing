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
using AW2.Graphics.Content;

namespace AW2.UI
{
    public partial class GameForm : Microsoft.Xna.Framework.Game
    {

        private AssaultWing _game;
        private GraphicsDeviceManager _graphics;
        private Stopwatch _stopWatch = new Stopwatch();
        private StringBuilder _logCache;

        public AssaultWing Game { get { return _game; } }

        public GameForm(CommandLineOptions commandLineOptions) : base()
        {
            Window.AllowUserResizing = true;
            _graphics = new GraphicsDeviceManager(this);

            Services.AddService(_graphics);
            var AWContentManager = new AWContentManager(Services, Content.RootDirectory, ignoreGraphicsContent: commandLineOptions.DedicatedServer);
            Content = AWContentManager;
            Services.AddService(AWContentManager);
            InitializeGame(commandLineOptions);
            InitializeGameForm();
        }

        public void FinishGame()
        {
            Game.EndRun();
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
            if(_graphics.IsFullScreen) {
                _graphics.ToggleFullScreen();
            }
        }
        public void SetFullScreen(int width, int height)
        {
            if(!_graphics.IsFullScreen) {
                _graphics.ToggleFullScreen();                
            }
            _graphics.PreferredBackBufferWidth = width;
            _graphics.PreferredBackBufferHeight = height;
            _graphics.ApplyChanges();            
        }
        public void Close() {
            Game.EndRun();
            Exit();
        }
        public void ForceCursorShown()
        {
            IsMouseVisible = true;
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
                GetTitle = () => {return Window.Title;},
                SetTitle = (Action<string>)((text) => {Window.Title = text;}),
                GetClientBounds = () => { return _graphics.IsFullScreen ? Window.ClientBounds : new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height); },
                GetFullScreen = () => _graphics.IsFullScreen,
                SetWindowed = (Action)(SetWindowed),
                SetFullScreen = (Action<int, int>)(SetFullScreen),
                IsVerticalSynced = () => _graphics.SynchronizeWithVerticalRetrace,
                EnableVerticalSync = (Action)(() => {_graphics.SynchronizeWithVerticalRetrace = true;}),
                DisableVerticalSync = (Action)(() => {_graphics.SynchronizeWithVerticalRetrace = false;}),
                EnsureCursorHidden = (Action)(() => {IsMouseVisible = false;}),
                EnsureCursorShown = (Action)(() => {IsMouseVisible = true;}),
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
                _gameInitialized = true;

                ApplyInitialInMenuGraphicsSettings();
                _game.Initialize();
                _game.BeginRun();
            }

            if (_gameInitialized)
            {
                var awGameTime = new AWGameTime(gameTime.TotalGameTime, gameTime.ElapsedGameTime, _stopWatch.Elapsed);

                _game.Update(awGameTime);

                while (_game.LogicStateChanged) {
                    _game.LogicStateChanged = false;
                    // There is at least one case where the logic state change expects update to be called again before calling
                    // paint again or it will crash.
                    _game.Update(awGameTime);
                }

                _game.Draw();

                base.Update(gameTime);
            }
        }

        private void ApplyInitialInMenuGraphicsSettings() {
            var displayMode = _graphics.GraphicsDevice.Adapter.CurrentDisplayMode;
            var gfxSetup = Game.Settings.Graphics;


            _graphics.PreferredBackBufferWidth = Math.Max(800, displayMode.Width / 2);
            _graphics.PreferredBackBufferHeight = Math.Max(600, displayMode.Height / 2);
        
            _graphics.SynchronizeWithVerticalRetrace = gfxSetup.IsVerticalSynced;
            _graphics.ApplyChanges();            
        }


        private void AddToLogView(string text)
        {
            lock (_logCache) _logCache.Append(text).Append("\r\n");
        }

    }
}
