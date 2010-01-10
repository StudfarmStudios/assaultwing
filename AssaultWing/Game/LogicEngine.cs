using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using AW2.Events;
using AW2.Helpers;
using AW2.Net;
using AW2.Net.Messages;
using AW2.UI;

namespace AW2.Game
{
    /// <summary>
    /// Basic implementation of game logic.
    /// </summary>
    class LogicEngine : GameComponent
    {
        Control fullscreenControl, escapeControl;
#if DEBUG
        Control showOnlyPlayer1Control, showOnlyPlayer2Control, showEverybodyControl;
#endif

        public LogicEngine(Microsoft.Xna.Framework.Game game)
            : base(game)
        {
            escapeControl = new KeyboardKey(Keys.Escape);
            fullscreenControl = new KeyboardKey(Keys.F10);
#if DEBUG
            showOnlyPlayer1Control = new KeyboardKey(Keys.F11);
            showOnlyPlayer2Control = new KeyboardKey(Keys.F12);
            showEverybodyControl = new KeyboardKey(Keys.F9);
#endif
        }

        public override void Initialize()
        {
            Helpers.Log.Write("Loading user-defined types");

            var gobLoader = new TypeLoader(typeof(Gob), Helpers.Paths.Gobs);
            var deviceLoader = new TypeLoader(typeof(ShipDevice), Helpers.Paths.Devices);
            var particleLoader = new TypeLoader(typeof(Gob), Helpers.Paths.Particles);
            var arenaLoader = new ArenaTypeLoader(typeof(Arena), Helpers.Paths.Arenas);
            /*action loader should be enabled if game actions are defined in own XML files*/
            //var actionLoader = new TypeLoader(typeof(GameAction), Helpers.Paths.Actions);

            DeleteTemplates(new TypeLoader[] { gobLoader, deviceLoader, particleLoader, arenaLoader });

            foreach (Gob gob in gobLoader.LoadAllTypes())
                AssaultWing.Instance.DataEngine.AddTypeTemplate(gob.TypeName, gob);
            foreach (ShipDevice device in deviceLoader.LoadAllTypes())
                AssaultWing.Instance.DataEngine.AddTypeTemplate(device.TypeName, device);
            foreach (Gob particleEngine in particleLoader.LoadAllTypes())
                AssaultWing.Instance.DataEngine.AddTypeTemplate(particleEngine.TypeName, particleEngine);
            /*foreach (GameAction action in actionLoader.LoadAllTypes())
                AssaultWing.Instance.DataEngine.AddTypeTemplate(action.TypeName, action);
            */
            AssaultWing.Instance.DataEngine.ArenaInfos = arenaLoader.LoadAllTypes().Cast<Arena>().Select(arena => arena.Info).ToList();
            SaveTemplates(new TypeLoader[] { gobLoader, deviceLoader, particleLoader, arenaLoader });
            FreezeCanonicalStrings();
            base.Initialize();
        }

        /// <summary>
        /// Performs game logic.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update</param>
        public override void Update(GameTime gameTime)
        {
            var data = AssaultWing.Instance.DataEngine;
            UpdateControls();

            // Update gobs, weapons and players.
            foreach (var gob in data.Arena.Gobs) gob.Update();
            foreach (var device in data.Devices) device.Update();
            foreach (var player in data.Spectators) player.Update();

            AssaultWing.Instance.DataEngine.Arena.PerformNonphysicalCollisions();

            // Check for arena end. Network games end when the game server presses Esc.
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Standalone)
            {
                if (data.GameplayMode.ArenaFinished(data.Arena, data.Players))
                    AssaultWing.Instance.FinishArena();
            }
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client)
            {
                var message = AssaultWing.Instance.NetworkEngine.GameServerConnection.Messages.TryDequeue<ArenaFinishMessage>();
                if (message != null)
                    AssaultWing.Instance.FinishArena();
            }
        }

        /// <summary>
        /// Checks general game controls and reacts to them.
        /// </summary>
        private void UpdateControls()
        {
            if (fullscreenControl.Pulse)
                AssaultWing.Instance.ToggleFullscreen();
#if DEBUG
            if (showEverybodyControl.Pulse)
                AssaultWing.Instance.ShowOnlyPlayer(-1);
            if (showOnlyPlayer1Control.Pulse)
                AssaultWing.Instance.ShowOnlyPlayer(0);
            if (showOnlyPlayer2Control.Pulse && AssaultWing.Instance.DataEngine.Spectators.Count > 1)
                AssaultWing.Instance.ShowOnlyPlayer(1);
#endif
            if (escapeControl.Pulse)
            {
                AW2.Graphics.CustomOverlayDialogData dialogData;
                if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
                    dialogData = new AW2.Graphics.CustomOverlayDialogData(
                        "Finish Arena? (Yes/No)",
                        new TriggeredCallback(TriggeredCallback.GetYesControl(), AssaultWing.Instance.FinishArena),
                        new TriggeredCallback(TriggeredCallback.GetNoControl(), AssaultWing.Instance.ResumePlay));
                else
                    dialogData = new AW2.Graphics.CustomOverlayDialogData(
                        "Quit to Main Menu? (Yes/No)",
                        new TriggeredCallback(TriggeredCallback.GetYesControl(), AssaultWing.Instance.ShowMenu),
                        new TriggeredCallback(TriggeredCallback.GetNoControl(), AssaultWing.Instance.ResumePlay));

                AssaultWing.Instance.ShowDialog(dialogData);
            }
        }

        /// <summary>
        /// Freezes <see cref="CanonicalString"/> instances to enable sharing them over a network.
        /// </summary>
        private static void FreezeCanonicalStrings()
        {
            // Type names of gobs, ship devices and particle engines are registered implicitly
            // above while loading the types. Graphics and ShipDeviceCollection need separate handling.
            // TODO: Loop through all textures and all 3D models available in the ContentManager.
            var content = (AW2.Graphics.AWContentManager)AssaultWing.Instance.Content;
            foreach (var assetName in content.GetAssetNames()) CanonicalString.Register(assetName);
            var temp = new AW2.Game.ShipDeviceCollection(null);
            CanonicalString.DisableRegistering();
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void DeleteTemplates(IEnumerable<TypeLoader> typeLoaders)
        {
            if (!AssaultWing.Instance.CommandLineArgs.Contains("-deletetemplates")) return;
            Log.Write("Parameter -deletetemplates given, deleting templates now...");
            foreach (var typeLoader in typeLoaders) typeLoader.DeleteTemplates();
            Log.Write("...templates deleted");
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void SaveTemplates(IEnumerable<TypeLoader> typeLoaders)
        {
            if (!AssaultWing.Instance.CommandLineArgs.Contains("-savetemplates")) return;
            Log.Write("Parameter -savetemplates given, saving templates now...");
            foreach (var typeLoader in typeLoaders) typeLoader.SaveTemplates();
            Log.Write("...templates saved");
        }
    }
}
