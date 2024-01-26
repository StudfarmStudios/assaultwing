using System;
using AW2.Core;

using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Game = Microsoft.Xna.Framework.Game;
using Microsoft.Xna.Framework;
using AW2.Graphics.Content;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core.GameComponents;

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
        private readonly GameServiceContainer Services = new GameServiceContainer();

        public AssaultWing<DedicatedServerEvent> Game { get { return _game; } }

        public GameForm(CommandLineOptions commandLineOptions) : base()
        {
            // Try to initialize steam game server service early on so we know if 
            // we should use NetworkEngineSteam or NetworkEngineRaw.
            var steamApiService = new SteamApiService();
            Services.AddService(steamApiService);

            _graphics = new DummyGraphicsDeviceService();
            Services.AddService(_graphics);
            var AWContentManager = new AWContentManager(Services, rootDirectory: "", ignoreGraphicsContent: true);
            Services.AddService(AWContentManager);

            InitializeGame(commandLineOptions);
            InitializeGameForm();
            _awGameRunner = new AWGameRunner(_game, () => { }, useParentTime: false, sleepIfEarly: true, graphicsEnabled: false);
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
        }
        public void Close()
        {
            Game.EndRun();
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        private void InitializeGameForm()
        {
        }

        private void InitializeGame(CommandLineOptions commandLineOptions)
        {
            _game = new AssaultWing<DedicatedServerEvent>(Services, commandLineOptions, game => new DedicatedServerLogicStandalone(game, consoleServer: true));
            AssaultWingCore.Instance = _game; // HACK: support older code that uses the static instance
            _game.Window = new Window(new Window.WindowImpl
            {
                GetTitle = () => { return "AssaultWing"; },
                SetTitle = (Action<string>)((text) => {; }),
                GetClientBounds = () => { return new Rectangle(0, 0, 800, 600); },
                GetFullScreen = () => false,
                SetWindowed = (Action)(() => { }),
                SetFullScreen = (Action<int, int>)((w, h) => { }),
                IsVerticalSynced = () => false,
                EnableVerticalSync = (Action)(() => { }),
                DisableVerticalSync = (Action)(() => { }),
                EnsureCursorHidden = (Action)(() => { }),
                EnsureCursorShown = (Action)(() => { }),
            });
        }


        public void Run()
        {
            var serverConsole = new ServerConsole(_game);

            serverConsole.Start();

            DateTime? endGameAt = null;
            while (endGameAt is null || endGameAt.Value > DateTime.Now)
            {
                _awGameRunner.Update(new GameTime());

                var command = serverConsole.Update();

                if (command?.Type == DedicatedServerEvent.EventType.Stop)
                {
                    endGameAt = DateTime.Now + (DedicatedServer.ArenaCommandEndGraceTime + TimeSpan.FromSeconds(2));
                }
            }

            serverConsole?.Dispose();
        }
    }
}
