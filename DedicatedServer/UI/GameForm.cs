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
using Microsoft.Xna.Framework.Graphics;

namespace AW2.UI
{
    public class DummyGraphicsDeviceService : IGraphicsDeviceService
    {
        public GraphicsDevice GraphicsDevice { get; }
        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceDisposing;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceResetting;
    }

    public partial class GameForm : IDisposable
    {

        private AssaultWing _game;
        private IGraphicsDeviceService _graphics;
        private Stopwatch _stopWatch = new Stopwatch();
        private StringBuilder _logCache;
        private readonly GameServiceContainer Services = new GameServiceContainer();

        public AssaultWing Game { get { return _game; } }

        public GameForm(CommandLineOptions commandLineOptions) : base()
        {

            _graphics = new DummyGraphicsDeviceService();
            Services.AddService(_graphics);
            var AWContentManager = new AWContentManager(Services, rootDirectory: "");
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
            //base.Dispose();
        }
        public void SetWindowed()
        {

        }
        public void SetFullScreen(int width, int height)
        {
        
        }
        public void Close() {
            Game.EndRun();
            //Exit();
        }
        public void ForceCursorShown()
        {
        }
        

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stopWatch.Stop();
            }            
            //base.Dispose(disposing);
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
                GetTitle = () => {return "AssaultWing";},
                SetTitle = (Action<string>)((text) => {;}),
                GetClientBounds = () => { return new Rectangle(0, 0, 800, 600); },
                GetFullScreen = () => false,
                SetWindowed = (Action)(SetWindowed),
                SetFullScreen = (Action<int, int>)(SetFullScreen),
                IsVerticalSynced = () => false,
                EnableVerticalSync = (Action)(() => {}),
                DisableVerticalSync = (Action)(() => {}),
                EnsureCursorHidden = (Action)(() => {}),
                EnsureCursorShown = (Action)(() => {}),
            });
            
            //_gameView.Draw += _game.Draw;
            //_gameView.ExternalWndProc += WndProcImpl;
            //_gameView.Resize += (sender, eventArgs) => _game.DataEngine.RearrangeViewports();
        }

        private bool _gameInitialized = false;
        protected void Update(GameTime gameTime)
        {
            if (!_gameInitialized)
            {
                _gameInitialized = true;

                // ApplyInitialInMenuGraphicsSettings();
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

                //_game.Draw();

                //base.Update(gameTime);
            }
        }

        public void Run()
        {
            // TODO: Peter: AWGameRunner?
            while (true)
            {
                Update(new GameTime());
                Thread.Sleep(1000/60);
            }
        }

        private void AddToLogView(string text)
        {
            lock (_logCache) _logCache.Append(text).Append("\r\n");
        }

    }
}
