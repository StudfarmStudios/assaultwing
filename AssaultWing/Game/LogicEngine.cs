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
            Helpers.Log.Write("Loading user-defined types");

            TypeLoader gobLoader = new TypeLoader(typeof(Gob), Helpers.Paths.Gobs);
            Gob[] gobs = (Gob[])gobLoader.LoadAllTypes();
            foreach (Gob gob in gobs)
                AssaultWing.Instance.DataEngine.AddTypeTemplate(typeof(Gob), gob.TypeName, gob);
            gobLoader.SaveTemplates();

            TypeLoader weaponLoader = new TypeLoader(typeof(Weapon), Helpers.Paths.Weapons);
            Weapon[] weapons = (Weapon[])weaponLoader.LoadAllTypes();
            foreach (Weapon weapon in weapons)
                AssaultWing.Instance.DataEngine.AddTypeTemplate(typeof(Weapon), weapon.TypeName, weapon);
            weaponLoader.SaveTemplates();

            TypeLoader particleLoader = new TypeLoader(typeof(Gob), Helpers.Paths.Particles);
            Gob[] particleEngines = (Gob[])particleLoader.LoadAllTypes();
            foreach (Gob particleEngine in particleEngines)
                AssaultWing.Instance.DataEngine.AddTypeTemplate(typeof(Gob), particleEngine.TypeName, particleEngine);
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
            AssaultWing.Instance.DataEngine.ArenaPlaylist = arenaNames;
            AssaultWing.Instance.DataEngine.ArenaFileNameList = arenaFileNames;
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
            UpdateControls();

            // Update gobs, weapons and players.
            foreach (var gob in AssaultWing.Instance.DataEngine.Gobs) gob.Update();
            foreach (var weapon in AssaultWing.Instance.DataEngine.Weapons) weapon.Update();
            foreach (var player in AssaultWing.Instance.DataEngine.Players) player.Update();

            // Check for receptor collisions.
            AssaultWing.Instance.PhysicsEngine.MovesDone();

            // Check for arena gameplay start if we are a game client.
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client)
                AssaultWing.Instance.NetworkEngine.ReceiveFromServerWhile<ArenaStartMessage>(message =>
                {
                    AssaultWing.Instance.DataEngine.RefreshArenaRadarSilhouette();
                    return true;
                });

            // Check for arena end. Network games end when the game server presses Esc.
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Standalone)
            {
                int playersAlive = AssaultWing.Instance.DataEngine.Players.Count(player => player.Lives != 0);
                if (playersAlive <= 1)
                    AssaultWing.Instance.FinishArena();
            }
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client)
                AssaultWing.Instance.NetworkEngine.ReceiveFromServerWhile<ArenaFinishMessage>(message =>
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
