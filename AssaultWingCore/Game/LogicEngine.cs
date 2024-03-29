using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using AW2.Core;
using AW2.Game.Arenas;
using AW2.Game.GobUtils;
using AW2.Game.Logic;
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

        public LogicEngine(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
            _gobsToKillOnClient = new List<GobKillData>();
            _helpControl = new KeyboardKey(Keys.F1);
        }

        public static IEnumerable<object> LoadTemplates()
        {
            var gobLoader = new TypeLoader(typeof(Gob), Helpers.Paths.GOBS);
            var deviceLoader = new TypeLoader(typeof(ShipDevice), Helpers.Paths.DEVICES);
            var particleLoader = new TypeLoader(typeof(Gob), Helpers.Paths.PARTICLES);
            var gameplayModeLoader = new TypeLoader(typeof(GameplayMode), Helpers.Paths.GAMEPLAY_MODES);
            var arenaLoader = new ArenaTypeLoader(typeof(Arena), Helpers.Paths.ARENAS);
            return gobLoader.LoadTemplates().Union(
                deviceLoader.LoadTemplates().Union(
                particleLoader.LoadTemplates().Union(
                gameplayModeLoader.LoadTemplates().Union(
                arenaLoader.LoadTemplates()))));
        }

        public override void Initialize()
        {
            foreach (var template in LoadTemplates())
            {
                var name = template is Clonable ? ((Clonable)template).TypeName
                    : template is Arena ? ((Arena)template).Info.Name
                    : template is GameplayMode ? ((GameplayMode)template).Name
                    : (CanonicalString)null;
                System.Diagnostics.Debug.Assert(name.Value != null, "Unexpected template type " + template.GetType());
                Game.DataEngine.AddTypeTemplate(name, template);
            }
        }

        public void Reset()
        {
            _gobsToKillOnClient.Clear();
            Game.DataEngine.RebalanceTeams();
        }

        public override void Update()
        {
            var data = Game.DataEngine;
            UpdateControls();
            data.Arena.Update();
            foreach (var viewport in data.Viewports) viewport.Update();
            foreach (var gob in data.Arena.Gobs) gob.Update();
            foreach (var device in data.Devices) device.Update();
            foreach (var player in data.Spectators) player.Update();
            KillGobsOnClient();
            RemoveInactivePlayers();
            data.Arena.Gobs.FinishAddsAndRemoves();

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
                var gob = Game.DataEngine.Arena.Gobs[data.GobID];
                if (gob != null && !gob.Dead)
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
        }
    }
}
