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
using Microsoft.Xna.Framework.Input;
using AW2.Core.GameComponents;

namespace AW2.UI
{
    public partial class GameForm : Microsoft.Xna.Framework.Game
    {

        private AssaultWing<ClientEvent> _game;
        private GraphicsDeviceManager _graphics;
        private AWGameRunner _awGameRunner;
        private StringBuilder _logCache;

        public AssaultWing<ClientEvent> Game { get { return _game; } }

        public GameForm(CommandLineOptions commandLineOptions, SteamApiService steamApiService) : base()
        {
            Services.AddService(steamApiService);

            Window.AllowUserResizing = true;
            _graphics = new GraphicsDeviceManager(this);

            Services.AddService(_graphics);
            var AWContentManager = new AWContentManager(Services, Content.RootDirectory, ignoreGraphicsContent: commandLineOptions.DedicatedServer);
            Content = AWContentManager;
            Services.AddService(AWContentManager);
            InitializeGame(commandLineOptions);
            InitializeGameForm();
            _awGameRunner = new AWGameRunner(_game, ApplyInitialInMenuGraphicsSettings, useParentTime: true, sleepIfEarly: false, graphicsEnabled: !commandLineOptions.DedicatedServer);
        }

        public void FinishGame()
        {
            Game.EndRun();
        }

        public new void Dispose()
        {
            Window.TextInput -= TextInputHandler;
            _awGameRunner.Dispose();
            if (_game != null)
            {
                _game.UnloadContent();
                _game.Dispose();
            }
            _game = null;
            AW2.Helpers.Log.Written -= AddToLogView;

            Services.GetService<SteamApiService>().Dispose();
            
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
            base.Dispose(disposing);
        }

        private void InitializeGameForm()
        {
            Window.TextInput += TextInputHandler;
            AW2.Helpers.Log.Written += AddToLogView;
            _logCache = new StringBuilder();
        }

        private void TextInputHandler(object sender, TextInputEventArgs e)
        {
            _game.Window.OnKeyPress(e.Character);
        }

        private void InitializeGame(CommandLineOptions commandLineOptions)
        {


            //if (!commandLineOptions.DedicatedServer) _graphicsDeviceService = new GraphicsDeviceService(windowHandle);
            _game = new AssaultWing<ClientEvent>(Services, commandLineOptions, game => {
                if (commandLineOptions.DedicatedServer)
                    return new DedicatedServerLogic<ClientEvent>(game, consoleServer: false);
                else if (commandLineOptions.QuickStart != null)
                    return new QuickStartLogic(game, commandLineOptions.QuickStart);
                else
                    return new UserControlledLogic(game);
            });

            AW2.Graphics.PlayerViewport.CustomOverlayCreators.Add(viewport => new SystemStatusOverlay(viewport));
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

        protected override void Update(GameTime gameTime)
        {
            _awGameRunner.Update(gameTime);
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
