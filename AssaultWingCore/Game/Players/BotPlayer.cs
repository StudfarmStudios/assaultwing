using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using Rectangle = AW2.Helpers.Geometric.Rectangle;

namespace AW2.Game.Players
{
    [System.Diagnostics.DebuggerDisplay("ID:{ID} Name:{Name}")]
    public class BotPlayer : Spectator
    {
        private static CanonicalString[] g_botTypes = new[]
        {
            (CanonicalString)"rocket bot",
            (CanonicalString)"fusion cone bot",
            (CanonicalString)"bazooka bot",
            (CanonicalString)"mine bot",
        };

        private List<Gob> _bots;
        private int _preferredBotTypeIndex;
        private int _optimalBotCount;
        private List<TimedAction> _timedActions;

        public override IEnumerable<Gob> Minions { get { return _bots; } }
        private Arena Arena { get { return Game.DataEngine.Arena; } }

        public BotPlayer(AssaultWingCore game, int connectionID = Spectator.CONNECTION_ID_LOCAL, string lastKnownConnectionAddressString = null)
            : base(game, connectionID, lastKnownConnectionAddressString)
        {
            _bots = new List<Gob>();
        }

        public override void ResetForArena()
        {
            base.ResetForArena();
            _bots.Clear();
            _timedActions = new List<TimedAction>
            {
                new TimedAction(TimeSpan.FromSeconds(20), () => _optimalBotCount = GetOptimalBotCount()),
                new TimedAction(TimeSpan.FromSeconds(3), CreateBot),
            };
        }

        public override void Update()
        {
            base.Update();
            foreach (var act in _timedActions) act.Update(Arena.TotalTime);
        }

        public void SeizeBot(Bot bot)
        {
            if (_bots.Contains(bot)) return;
            _bots.Add(bot);
            bot.Owner = this;
            bot.Death += MinionDeathHandler.OnMinionDeath;
            bot.Death += BotDeathHandler;
        }

        private int GetOptimalBotCount()
        {
            if (Team == null) return 2;
            var standings = Game.DataEngine.GameplayMode.GetStandings(Game.DataEngine.Teams);
            var ourScore = standings[Team].Score;
            var bestScore = standings.Max(x => x.Item1.Score);
            var weAreLosingBadly = bestScore >= 10 && bestScore >= ourScore * 2;
            var weAreLosing = bestScore >= 10 && bestScore >= (ourScore * 4) / 3;
            return weAreLosingBadly ? 5
                : weAreLosing ? 3
                : 2;
        }

        private void CreateBot()
        {
            if (Minions.Count() >= _optimalBotCount) return;
            _preferredBotTypeIndex = (_preferredBotTypeIndex + 1) % g_botTypes.Length;
            Gob.CreateGob<Bot>(Game, g_botTypes[_preferredBotTypeIndex], bot =>
            {
                SeizeBot(bot);
                SpawnPlayer.PositionNewMinion(bot, Game.DataEngine.Arena);
                Arena.Gobs.Add(bot);
            });
        }

        private void BotDeathHandler(Coroner coroner)
        {
            _bots.Remove(coroner.DamageInfo.Target);
        }
    }
}
