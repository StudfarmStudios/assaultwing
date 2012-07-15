using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using AW2.Core;
using AW2.Game.Arenas;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.UI;

namespace AW2.Game
{
    /// <summary>
    /// Basic implementation of game logic.
    /// </summary>
    public class LogicEngine : AWGameComponent
    {
        private struct GobKillData
        {
            public int GobID;
            public TimeSpan KillTime; // In arena time
        }

        private static readonly TimeSpan PLAYER_DISCONNECT_DROP_DELAY = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan GOB_KILL_TIMEOUT_ON_CLIENT = TimeSpan.FromSeconds(5);

        private List<GobKillData> _gobsToKillOnClient;
        private Control _helpControl;
#if DEBUG
        private Control _showOnlyPlayer1Control, _showOnlyPlayer2Control, _showEverybodyControl;
#endif
 
        public LogicEngine(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
            _gobsToKillOnClient = new List<GobKillData>();
            _helpControl = new KeyboardKey(Keys.F1);
#if DEBUG
            _showOnlyPlayer1Control = new KeyboardKey(Keys.F11);
            _showOnlyPlayer2Control = new KeyboardKey(Keys.F12);
            _showEverybodyControl = new KeyboardKey(Keys.F9);
#endif
        }

        public override void Initialize()
        {
            Log.Write("Loading user-defined types");

            var gobLoader = new TypeLoader(typeof(Gob), Helpers.Paths.GOBS);
            var deviceLoader = new TypeLoader(typeof(ShipDevice), Helpers.Paths.DEVICES);
            var particleLoader = new TypeLoader(typeof(Gob), Helpers.Paths.PARTICLES);
            var arenaLoader = new ArenaTypeLoader(typeof(Arena), Helpers.Paths.ARENAS);

            DeleteTemplates(gobLoader, deviceLoader, particleLoader, arenaLoader);

            foreach (Gob gob in gobLoader.LoadTemplates())
                Game.DataEngine.AddTypeTemplate(gob.TypeName, gob);
            foreach (ShipDevice device in deviceLoader.LoadTemplates())
                Game.DataEngine.AddTypeTemplate(device.TypeName, device);
            foreach (Gob particleEngine in particleLoader.LoadTemplates())
                Game.DataEngine.AddTypeTemplate(particleEngine.TypeName, particleEngine);
            foreach (Arena arena in arenaLoader.LoadTemplates())
                Game.DataEngine.AddTypeTemplate(arena.Info.Name, arena);

            SaveTemplates(gobLoader, deviceLoader, particleLoader, arenaLoader);
            base.Initialize();
        }

        public void Reset()
        {
            _gobsToKillOnClient.Clear();
        }

        public override void Update()
        {
            var data = Game.DataEngine;
            UpdateControls();
            data.Arena.Update();
            foreach (var gob in data.Arena.Gobs) gob.Update();
            foreach (var device in data.Devices) device.Update();
            foreach (var player in data.Spectators) player.Update();
            KillGobsOnClient();
            RemoveInactivePlayers();

            // Check for arena end. Network games end when the game server presses Esc.
            if (Game.NetworkMode == NetworkMode.Standalone)
            {
                if (data.GameplayMode.ArenaFinished(data.Arena, data.Players))
                    Game.FinishArena();
            }
        }

        public void KillGobsOnClient(IEnumerable<int> gobIDs)
        {
            foreach (var gobID in gobIDs)
                _gobsToKillOnClient.Add(new GobKillData { GobID = gobID, KillTime = Game.DataEngine.ArenaTotalTime });
        }

        private void KillGobsOnClient()
        {
            var remainingKills = new List<GobKillData>();
            foreach (var data in _gobsToKillOnClient)
            {
                var gob = Game.DataEngine.Arena.Gobs.FirstOrDefault(gobb => gobb.ID == data.GobID);
                if (gob != null)
                    gob.DieOnClient();
                else if (data.KillTime + GOB_KILL_TIMEOUT_ON_CLIENT > Game.DataEngine.ArenaTotalTime)
                    remainingKills.Add(data);
            }
            _gobsToKillOnClient = remainingKills;
        }

        private void RemoveInactivePlayers()
        {
            if (Game.NetworkMode != NetworkMode.Server) return;
            Game.DataEngine.Spectators.Remove(spec => spec.IsDisconnected && spec.LastDisconnectTime + PLAYER_DISCONNECT_DROP_DELAY < Game.GameTime.TotalRealTime);
        }

        /// <summary>
        /// Checks general game controls and reacts to them.
        /// </summary>
        private void UpdateControls()
        {
            if (_helpControl.Pulse) Game.ShowPlayerHelp();
#if DEBUG
            if (_showEverybodyControl.Pulse)
                Game.ShowOnlyPlayer(-1);
            if (_showOnlyPlayer1Control.Pulse)
                Game.ShowOnlyPlayer(0);
            if (_showOnlyPlayer2Control.Pulse && Game.DataEngine.Spectators.Count > 1)
                Game.ShowOnlyPlayer(1);
#endif
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void DeleteTemplates(params TypeLoader[] typeLoaders)
        {
            if (!Game.CommandLineOptions.DeleteTemplates) return;
            Log.Write("Deleting templates now...");
            foreach (var typeLoader in typeLoaders) typeLoader.DeleteTemplates();
            Log.Write("...templates deleted");
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void SaveTemplates(params TypeLoader[] typeLoaders)
        {
            if (!Game.CommandLineOptions.SaveTemplates) return;
            Log.Write("Saving templates now...");
            foreach (var typeLoader in typeLoaders) typeLoader.SaveTemplateExamples();
            Log.Write("...templates saved");
        }
    }
}
