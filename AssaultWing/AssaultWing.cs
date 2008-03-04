#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Game;
using AW2.Game.Gobs;
using AW2.Graphics;
using AW2.UI;
using AW2.Sound;
using AW2.Events;
using AW2.Helpers;
using AW2.Game.Particles;

#endregion

namespace AW2
{
    /// <summary>
    /// The main class of the Assault Wing game. A singleton class.
    /// </summary>
    /// Game components can be requested from the AssaultWing.Services property.
    public class AssaultWing : Microsoft.Xna.Framework.Game
    {
        UIEngineImpl uiEngine;
        GraphicsEngineImpl graphicsEngine;
        OverlayDialog overlayDialog;
        MenuEngineImpl menuEngine;
        LogicEngineImpl logicEngine;
        ContentManager content;
        GraphicsDeviceManager graphics;
        int preferredWindowWidth, preferredWindowHeight;
        SurfaceFormat preferredWindowFormat;
        int preferredFullscreenWidth, preferredFullscreenHeight;
        SurfaceFormat preferredFullscreenFormat;
        TimeSpan gameTimeDelay;

        // HACK: Fields for frame stepping (for debugging)
        Control frameStepControl;
        Control frameRunControl;
        bool frameStep;

        /// <summary>
        /// The only existing instance of this class.
        /// </summary>
        static AssaultWing instance;

        GameTime gameTime;

        #region AssaultWing properties

        /// <summary>
        /// The game time on this frame.
        /// </summary>
        public GameTime GameTime { get { return gameTime; } }

        /// <summary>
        /// The screen dimensions of the game window's client rectangle.
        /// </summary>
        public Rectangle ClientBounds { get { return Window.ClientBounds; } }

        #endregion AssaultWing properties

        /// <summary>
        /// Creates a new Assault Wing - Galactic Battlefront game instance.
        /// </summary>
        /// This constructor is not meant to be called from outside this class.
        /// To obtain an AssaultWing instance, use <b>AssaultWing.Instance</b>.
        private AssaultWing()
            : base()
        {
            Log.Write("Creating an Assault Wing instance");

            // Decide on preferred windowed and fullscreen sizes and formats.
            DisplayMode displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            preferredFullscreenWidth = displayMode.Width;
            preferredFullscreenHeight = displayMode.Height;
            preferredFullscreenFormat = displayMode.Format;
            preferredWindowWidth = 1000;
            preferredWindowHeight = 800;
            preferredWindowFormat = displayMode.Format;

            content = new ContentManager(Services);
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = preferredWindowWidth;
            graphics.PreferredBackBufferHeight = preferredWindowHeight;
            // loading the NVIDIA perfHUD device if available
            graphics.PreparingDeviceSettings += new EventHandler<PreparingDeviceSettingsEventArgs>(graphics_PreparingDeviceSettings);
            graphics.IsFullScreen = false;
            Window.ClientSizeChanged += new EventHandler(Window_ClientSizeChanged);
            Window.AllowUserResizing = true;

            frameStepControl = new KeyboardKey(Keys.F8);
            frameRunControl = new KeyboardKey(Keys.F7);
            frameStep = false;
            gameTimeDelay = new TimeSpan(0);
        }

        /// <summary>
        /// If there is an NVIDIA PerfHUD adapter, set the GraphicsDeviceManager to use that adapter, and a Reference Device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void graphics_PreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            e.GraphicsDeviceInformation.PresentationParameters.PresentationInterval = PresentInterval.Immediate;

//            if (e.GraphicsDeviceInformation.PresentationParameters.IsFullScreen)
//            {
//                e.GraphicsDeviceInformation.PresentationParameters.FullScreenRefreshRateInHz = 60;
//            }

            foreach (GraphicsAdapter adapter in GraphicsAdapter.Adapters)
            {
                if (adapter.Description.Equals("NVIDIA PerfHUD"))
                {
                    e.GraphicsDeviceInformation.DeviceType = DeviceType.Reference;
                    e.GraphicsDeviceInformation.Adapter = adapter;
                    Log.Write("Found NVIDIA PerfHUD device, PerfHUD now enabled.");
                    break;
                }
            }
        }

        void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            graphicsEngine.WindowResize();
            menuEngine.WindowResize();
        }

        /// <summary>
        /// Returns (after creating) the only instance of class AssaultWing.
        /// </summary>
        public static AssaultWing Instance
        {
            get
            {
                if (instance == null)
                    instance = new AssaultWing();
                return instance;
            }
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            Log.Write("Assault Wing initializing");

            uiEngine = new UIEngineImpl(this);
            logicEngine = new LogicEngineImpl(this);
            SoundEngineImpl soundEngine = new SoundEngineImpl(this);
            graphicsEngine = new GraphicsEngineImpl(this);
            menuEngine = new MenuEngineImpl(this);
            overlayDialog = new OverlayDialog(this);
            
            uiEngine.UpdateOrder = 1;
            logicEngine.UpdateOrder = 2;
            soundEngine.UpdateOrder = 3;
            graphicsEngine.UpdateOrder = 4;
            overlayDialog.UpdateOrder = 5;
            menuEngine.UpdateOrder = 6;

            Components.Add(logicEngine);
            Components.Add(graphicsEngine);
            Components.Add(overlayDialog);
            Components.Add(uiEngine);
            Components.Add(soundEngine);
            Components.Add(menuEngine);
            DataEngine dataEngine = new DataEngineImpl();
            Services.AddService(typeof(DataEngine), dataEngine);
            Services.AddService(typeof(EventEngine), new EventEngineImpl());
            Services.AddService(typeof(PhysicsEngine), new PhysicsEngineImpl());


            menuEngine.Visible = false; // no visible menu
            menuEngine.Enabled = false; // menu doesn't consume keyboard events
            overlayDialog.Visible = false; // no visible dialog
            overlayDialog.Enabled = false; // dialog doesn't consume keyboard events

#if DEBUG
            SoundEffectEvent eventti = new SoundEffectEvent();
            eventti.setAction(SoundOptions.Action.Artillery);
            eventti.setEffect(SoundOptions.Effect.None);
            EventEngine eventEngine = (EventEngine)this.Services.GetService(typeof(EventEngine));
            eventEngine.SendEvent(eventti);
#endif

            TargetElapsedTime = new TimeSpan((long)(10000000.0 / 60.0)); // 60 frames per second

            base.Initialize();
        }

        #region Methods for game components

        /// <summary>
        /// Switches between displaying the menu and the game view.
        /// </summary>
        public void SwitchMenu()
        {
            if (graphicsEngine.Visible)
            {
                graphicsEngine.Visible = false;
                graphicsEngine.Enabled = false;
                logicEngine.Enabled = false;
                menuEngine.Enabled = true;
                menuEngine.Visible = true;
            }
            else
            {
                graphicsEngine.Visible = true;
                graphicsEngine.Enabled = true;
                logicEngine.Enabled = true;
                menuEngine.Enabled = false;
                menuEngine.Visible = false;
            }

        }

        /// <summary>
        /// Switches between displaying the menu and the game view.
        /// </summary>
        public void ToggleDialog()
        {
            if (!overlayDialog.Visible)
            {
                graphicsEngine.Enabled = false;
                logicEngine.Enabled = false;
                overlayDialog.Enabled = true;
                overlayDialog.Visible = true;
            }
            else
            {
                graphicsEngine.Enabled = true;
                logicEngine.Enabled = true;
                overlayDialog.Enabled = false;
                overlayDialog.Visible = false;
            }

        }

        /// <summary>
        /// Toggles between fullscreen and windowed mode.
        /// </summary>
        public void ToggleFullscreen()
        {
            // Set our window size and format preferences before switching.
            if (graphics.IsFullScreen)
            {
                graphics.PreferredBackBufferFormat = preferredWindowFormat;
                graphics.PreferredBackBufferHeight = preferredWindowHeight;
                graphics.PreferredBackBufferWidth = preferredWindowWidth;
            }
            else
            {
                graphics.PreferredBackBufferFormat = preferredFullscreenFormat;
                graphics.PreferredBackBufferHeight = preferredFullscreenHeight;
                graphics.PreferredBackBufferWidth = preferredFullscreenWidth;
            }
            graphics.ToggleFullScreen();
        }

        /// <summary>
        /// Rearranges player viewports, optionally so that 
        /// the whole screen area is given to only one player.
        /// </summary>
        /// <param name="player">The player to give all the screen space to,
        /// or <b>-1</b> to share the screen equally.</param>
        public void ShowOnlyPlayer(int player)
        {
            if (player < 0)
                graphicsEngine.RearrangeViewports();
            else
                graphicsEngine.RearrangeViewports(player);
        }

        #endregion Methods for game components

        /// <summary>
        /// Called after all components are initialized but before the first update in the game loop. 
        /// </summary>
        protected override void BeginRun()
        {
            Log.Write("Assault Wing begins to run");

            // Hardcoded for now!!!
            DataEngine dataEngine = (DataEngine)Services.GetService(typeof(DataEngine));

            PlayerControls plr1Controls;
            plr1Controls.thrust = new KeyboardKey(Keys.Up);
            plr1Controls.left = new KeyboardKey(Keys.Left);
            plr1Controls.right = new KeyboardKey(Keys.Right);
            plr1Controls.down = new KeyboardKey(Keys.Down);
            plr1Controls.fire1 = new KeyboardKey(Keys.RightControl);
            plr1Controls.fire2 = new KeyboardKey(Keys.RightShift);
            plr1Controls.extra = new KeyboardKey(Keys.Enter);

            PlayerControls plr2Controls;
            plr2Controls.thrust = new KeyboardKey(Keys.W);
            plr2Controls.left = new KeyboardKey(Keys.A);
            plr2Controls.right = new KeyboardKey(Keys.D);
            plr2Controls.down = new KeyboardKey(Keys.X);
            plr2Controls.fire1 = new KeyboardKey(Keys.LeftControl);
            plr2Controls.fire2 = new KeyboardKey(Keys.LeftShift);
            plr2Controls.extra = new KeyboardKey(Keys.CapsLock);

            Player player1 = new Player("Kaiser Lohengramm", "Hyperion", "peashooter", "rockets", plr1Controls);
            Player player2 = new Player("John Crichton", "Prowler", "shotgun", "bazooka", plr2Controls);
            player1.Lives = 3;
            player2.Lives = 3;
            dataEngine.AddPlayer(player1);
            dataEngine.AddPlayer(player2);

            dataEngine.InitializeFromArena("Blood Bowl");
            logicEngine.Reset();

            Ship ship1 = new Ship("Hyperion", player1, new Vector2(100f, 100f), "peashooter", "rockets");
            Ship ship2 = new Ship("Prowler", player2, new Vector2(200f, 200f), "shotgun", "bazooka");
            dataEngine.AddGob(ship1);
            dataEngine.AddGob(ship2);
            player1.Ship = ship1;
            player2.Ship = ship2;

            graphicsEngine.RearrangeViewports();

            base.BeginRun();
        }

        /// <summary>
        /// Called after the game loop has stopped running before exiting. 
        /// </summary>
        protected override void EndRun()
        {
            Log.Write("Assault Wing ends the run");

            // Unregister player controls.
            // !!!This is done here only so that when one finally implements 
            // removing players, he will remember to release removed players' controls.
            DataEngine data = (DataEngine)Services.GetService(typeof(DataEngine));
            Action<Player> controlRelease = delegate(Player plr)
            {
                plr.Controls.thrust.Release();
                plr.Controls.left.Release();
                plr.Controls.right.Release();
                plr.Controls.down.Release();
                plr.Controls.fire1.Release();
                plr.Controls.fire2.Release();
                plr.Controls.extra.Release();
            };
            data.ForEachPlayer(controlRelease);

            base.EndRun();
        }
        
        /// <summary>
        /// Load your graphics content.  If loadAllContent is true, you should
        /// load content from both ResourceManagementMode pools.  Otherwise, just
        /// load ResourceManagementMode.Manual content.
        /// </summary>
        protected override void LoadContent()
        {
            // TODO: Load any ResourceManagementMode.Automatic content
            // TODO: Load any ResourceManagementMode.Manual content
        }


        /// <summary>
        /// Unload your (graphics) content.
        /// </summary>
        protected override void UnloadContent()
        {
                // TODO: Unload any ResourceManagementMode.Automatic content
//                content.Unload();
        }


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            DataEngine data = (DataEngine)Services.GetService(typeof(DataEngine));

            // Frame stepping (for debugging)
            if (frameRunControl.Pulse)
            {
                logicEngine.Enabled = true;
                frameStep = false;
            }
            if (frameStep)
            {
                if (frameStepControl.Pulse)
                    logicEngine.Enabled = true;
                else
                    logicEngine.Enabled = false;
            }
            else if (frameStepControl.Pulse)
            {
                logicEngine.Enabled = false;
                frameStep = true;
            }

            // Take care of game time freezing if game logic is disabled.
            if (!logicEngine.Enabled)
                gameTimeDelay = gameTimeDelay.Add(gameTime.ElapsedGameTime);
            this.gameTime = new GameTime(gameTime.TotalRealTime, gameTime.ElapsedRealTime,
                gameTime.TotalGameTime.Subtract(gameTimeDelay),
                gameTime.ElapsedGameTime);

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            base.Update(this.gameTime);
            data.CommitPending();
        }


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }
    }
}
