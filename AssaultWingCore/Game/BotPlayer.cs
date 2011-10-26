using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using Rectangle = AW2.Helpers.Geometric.Rectangle;

namespace AW2.Game
{
    public class BotPlayer : Spectator
    {
        private const int MAX_MINION_COUNT = 5;
        private const int MIN_BOT_COUNT = 2; // overrides MAX_MINION_COUNT
        private readonly TimeSpan BOT_CREATION_INTERVAL = TimeSpan.FromSeconds(9.5);

        private List<Gob> _bots;
        private TimeSpan _nextBotCreationTime;

        public override IEnumerable<Gob> Minions { get { return _bots; } }
        private Arena Arena { get { return Game.DataEngine.Arena; } }
        private bool EnoughBots
        {
            get
            {
                var minions = Game.DataEngine.Minions;
                var botCount = minions.OfType<Bot>().Count();
                var minionCount = minions.Count();
                return botCount >= MIN_BOT_COUNT && minionCount >= MAX_MINION_COUNT;
            }
        }

        public BotPlayer(AssaultWingCore game, int connectionID = Spectator.CONNECTION_ID_LOCAL)
            : base(game, connectionID)
        {
            _bots = new List<Gob>();
            Name = "The Bots";
            Color = Color.LightGray;
        }

        public override void ResetForArena()
        {
            base.ResetForArena();
            _nextBotCreationTime = BOT_CREATION_INTERVAL;
            _bots.Clear();
        }

        public override void Update()
        {
            base.Update();
            CreateBot();
        }

        public void SeizeBot(Bot bot)
        {
            if (_bots.Contains(bot)) return;
            _bots.Add(bot);
            bot.Owner = this;
            bot.Death += MinionDeathHandler.OnMinionDeath;
            bot.Death += coroner => _bots.Remove(bot);
        }

        private void CreateBot()
        {
            if (_nextBotCreationTime > Game.GameTime.TotalGameTime) return;
            _nextBotCreationTime = Game.GameTime.TotalGameTime + BOT_CREATION_INTERVAL;
            if (EnoughBots) return;
            Gob.CreateGob<Bot>(Game, (CanonicalString)"rocket bot", bot =>
            {
                SeizeBot(bot);
                var pos = Arena.GetFreePosition(bot, Arena.BoundedArea);
                bot.ResetPos(pos, Vector2.Zero, Gob.DEFAULT_ROTATION);
                Arena.Gobs.Add(bot);
            });
        }
    }
}
