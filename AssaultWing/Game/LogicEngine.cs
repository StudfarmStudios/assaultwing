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

            TypeLoader gobLoader = new TypeLoader(typeof(Gob), Helpers.Paths.Gobs);
            Gob[] gobs = (Gob[])gobLoader.LoadAllTypes();
            foreach (Gob gob in gobs)
                AssaultWing.Instance.DataEngine.AddTypeTemplate(TypeTemplateType.Gob, gob.TypeName, gob);

            TypeLoader weaponLoader = new TypeLoader(typeof(Weapon), Helpers.Paths.Weapons);
            Weapon[] weapons = (Weapon[])weaponLoader.LoadAllTypes();
            foreach (Weapon weapon in weapons)
                AssaultWing.Instance.DataEngine.AddTypeTemplate(TypeTemplateType.Weapon, weapon.TypeName, weapon);

            TypeLoader particleLoader = new TypeLoader(typeof(Gob), Helpers.Paths.Particles);
            Gob[] particleEngines = (Gob[])particleLoader.LoadAllTypes();
            foreach (Gob particleEngine in particleEngines)
                AssaultWing.Instance.DataEngine.AddTypeTemplate(TypeTemplateType.Gob, particleEngine.TypeName, particleEngine);

            ArenaTypeLoader arenaLoader = new ArenaTypeLoader(typeof(Arena), Helpers.Paths.Arenas);
            IEnumerable<Arena> arenas = (Arena[])arenaLoader.LoadAllTypes();
            Dictionary<string, string> arenaFileNames = new Dictionary<string, string>();
            arenas = arenas.Where(arena => arena.Name != "dummyarena"); // HACK: avoiding the automatically generated arena template
            AssaultWing.Instance.DataEngine.ArenaInfos = arenas.Select(arena => arena.Info).ToList();

            SaveAndDeleteTemplates(new TypeLoader[] { gobLoader, weaponLoader, particleLoader, arenaLoader });

            // Freeze CanonicalStrings to enable sharing them over a network.
            // Type names of gobs, weapons and particle engines are registered implicitly
            // above while loading the types. Graphics and ShipDeviceCollection need separate handling.
            // TODO: Loop through all textures and all 3D models available in the ContentManager.
            var content = (AW2.Graphics.AWContentManager)AssaultWing.Instance.Content;
            foreach (var assetName in content.GetAssetNames()) CanonicalString.Register(assetName);
            var temp = new AW2.Game.ShipDeviceCollection(null);
            CanonicalString.DisableRegistering();

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
            foreach (var weapon in data.Weapons) weapon.Update();
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

        [System.Diagnostics.Conditional("DEBUG")]
        private void SaveAndDeleteTemplates(IEnumerable<TypeLoader> typeLoaders)
        {
            if (AssaultWing.Instance.CommandLineArgs.Contains("-deletetemplates"))
            {
                Log.Write("Parameter -deletetemplates given, deleting templates now...");
                foreach (var typeLoader in typeLoaders) typeLoader.DeleteTemplates();
                Log.Write("...templates deleted");
            }
            if (AssaultWing.Instance.CommandLineArgs.Contains("-savetemplates"))
            {
                Log.Write("Parameter -savetemplates given, saving templates now...");
                foreach (var typeLoader in typeLoaders) typeLoader.SaveTemplates();
                Log.Write("...templates saved");
            }
        }
    }
}
