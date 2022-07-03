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

        private AssaultWing<DedicatedServerEvent> _game;
        private IGraphicsDeviceService _graphics;
        private AWGameRunner _awGameRunner;
        private StringBuilder _logCache;
        private readonly GameServiceContainer Services = new GameServiceContainer();

        public AssaultWing<DedicatedServerEvent> Game { get { return _game; } }

        public GameForm(CommandLineOptions commandLineOptions) : base()
        {
            _graphics = new DummyGraphicsDeviceService();
            Services.AddService(_graphics);
            var AWContentManager = new AWContentManager(Services, rootDirectory: "", ignoreGraphicsContent: true);
            Services.AddService(AWContentManager);

            Services.AddService(new SteamApiService());

            InitializeGame(commandLineOptions);
            InitializeGameForm();
            _awGameRunner = new AWGameRunner(_game, () => {}, useParentTime: false, sleepIfEarly: true, graphicsEnabled: false);
        }

        public void FinishGame()
        {
            Game.EndRun();
        }

        public void Dispose()
        {
            _awGameRunner.Dispose();

            if (_game != null)
            {
                _game.UnloadContent();
                _game.Dispose();
            }
            _game = null;
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
        }

        private void InitializeGameForm()
        {
            AW2.Helpers.Log.Written += AddToLogView;
            _logCache = new StringBuilder();
        }

        private void InitializeGame(CommandLineOptions commandLineOptions)
        {
            //if (!commandLineOptions.DedicatedServer) _graphicsDeviceService = new GraphicsDeviceService(windowHandle);
            _game = new AssaultWing<DedicatedServerEvent>(Services, commandLineOptions, game => new DedicatedServerLogic<DedicatedServerEvent>(game));
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

        public void Run()
        {
            while (true) // TODO: Peter: exit logic
            {
                _awGameRunner.Update(new GameTime());
            }
        }

        private void AddToLogView(string text)
        {
            lock (_logCache) _logCache.Append(text).Append("\r\n");
        }

    }
}
