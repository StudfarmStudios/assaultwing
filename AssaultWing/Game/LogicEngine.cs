using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using AW2.Events;
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
        Control escapeControl;

        public LogicEngine(Microsoft.Xna.Framework.Game game) : base(game)
        {
            escapeControl = new KeyboardKey(Keys.Escape);
        }

        public override void Initialize()
        {
            DataEngine data = (DataEngine)Game.Services.GetService(typeof(DataEngine));
            Helpers.Log.Write("Loading user-defined types");

            TypeLoader gobLoader = new TypeLoader(typeof(Gob), Helpers.Paths.Gobs);
            Gob[] gobs = (Gob[])gobLoader.LoadAllTypes();
            foreach (Gob gob in gobs)
                data.AddTypeTemplate(typeof(Gob), gob.TypeName, gob);
            gobLoader.SaveTemplates();

            TypeLoader weaponLoader = new TypeLoader(typeof(Weapon), Helpers.Paths.Weapons);
            Weapon[] weapons = (Weapon[])weaponLoader.LoadAllTypes();
            foreach (Weapon weapon in weapons)
                data.AddTypeTemplate(typeof(Weapon), weapon.TypeName, weapon);
            weaponLoader.SaveTemplates();

            TypeLoader particleLoader = new TypeLoader(typeof(Gob), Helpers.Paths.Particles);
            Gob[] particleEngines = (Gob[])particleLoader.LoadAllTypes();
            foreach (Gob particleEngine in particleEngines)
                data.AddTypeTemplate(typeof(Gob), particleEngine.TypeName, particleEngine);
            particleLoader.SaveTemplates();

            ArenaTypeLoader arenaLoader = new ArenaTypeLoader(typeof(Arena), Helpers.Paths.Arenas);
            Arena[] arenas = (Arena[])arenaLoader.LoadAllTypes();
            arenaLoader.SaveTemplates();
            List<string> arenaNames = new List<string>();

            Dictionary<string,string> arenaFileNames = new Dictionary<string,string>();
            foreach (Arena arena in arenas)
                if (arena.Name != "dummyarena") // HACK: avoiding the automatically generated arena template
                {
                    arenaNames.Add(arena.Name);
                    arenaFileNames.Add(arena.Name, arena.FileName);
                }
            data.ArenaPlaylist = arenaNames;
            data.ArenaFileNameList = arenaFileNames;
            base.Initialize();
        }

        /// <summary>
        /// Resets the logic engine for a new arena.
        /// </summary>
        public void Reset()
        {
        }

        /// <summary>
        /// Performs game logic.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update</param>
        public override void Update(GameTime gameTime)
        {
            DataEngine data = (DataEngine)Game.Services.GetService(typeof(DataEngine));
            NetworkEngine net = (NetworkEngine)AssaultWing.Instance.Services.GetService(typeof(NetworkEngine));
            PhysicsEngine physics = (PhysicsEngine)Game.Services.GetService(typeof(PhysicsEngine));
            
            UpdateControls();

            // Update gobs, weapons and players.
            foreach (var gob in data.Gobs) gob.Update();
            foreach (var weapon in data.Weapons) weapon.Update();
            foreach (var player in data.Players) player.Update();

            // Check for receptor collisions.
            physics.MovesDone();

            // Check for arena gameplay start if we are a game client.
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client)
                net.ReceiveFromServerWhile<ArenaStartMessage>(message =>
                {
                    data.RefreshArenaRadarSilhouette();
                    return true;
                });

            // Check for arena end. Network games end when the game server presses Esc.
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Standalone)
            {
                int playersAlive = data.Players.Count(player => player.Lives != 0);
                if (playersAlive <= 1)
                    AssaultWing.Instance.FinishArena();
            }
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client)
                net.ReceiveFromServerWhile<ArenaFinishMessage>(message =>
                {
                    AssaultWing.Instance.FinishArena();
                    return false;
                });
        }

        /// <summary>
        /// Checks general game controls and reacts to them.
        /// </summary>
        private void UpdateControls()
        {
            DataEngine data = (DataEngine)Game.Services.GetService(typeof(DataEngine));
            EventEngine eventEngine = (EventEngine)Game.Services.GetService(typeof(EventEngine));

            // Check general game controls.
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
    }
}
