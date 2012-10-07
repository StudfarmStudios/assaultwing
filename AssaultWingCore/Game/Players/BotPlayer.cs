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
        private const int MAX_MINION_COUNT = 5;
        private const int MIN_BOT_COUNT = 2; // overrides MAX_MINION_COUNT
        private readonly TimeSpan BOT_CREATION_INTERVAL = TimeSpan.FromSeconds(9.5);

        private List<Gob> _bots;
        private TimeSpan _nextBotCreationTime;
        private int _preferredBotTypeIndex;

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

        public BotPlayer(AssaultWingCore game, int connectionID = Spectator.CONNECTION_ID_LOCAL, IPAddress ipAddress = null)
            : base(game, connectionID, ipAddress)
        {
            _bots = new List<Gob>();
            Name = AW2.Settings.PlayerSettings.BOTS_NAME;
            Color = Color.LightGray;
        }

        public override void ResetForArena()
        {
            base.ResetForArena();
            _nextBotCreationTime = Game.GameTime.TotalGameTime + BOT_CREATION_INTERVAL;
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
            _preferredBotTypeIndex = (_preferredBotTypeIndex + 1) % g_botTypes.Length;
            Gob.CreateGob<Bot>(Game, g_botTypes[_preferredBotTypeIndex], bot =>
            {
                SeizeBot(bot);
                var pos = Arena.GetFreePosition(Gob.LARGE_GOB_PHYSICAL_RADIUS, Arena.BoundedAreaNormal);
                bot.ResetPos(pos, Vector2.Zero, Gob.DEFAULT_ROTATION);
                Arena.Gobs.Add(bot);
                Game.Stats.Send(new
                {
                    Ship = bot.TypeName.Value,
                    Weapon2 = bot.WeaponName.Value,
                    Device = "",
                    Player = Game.Stats.GetStatsString(this),
                    Pos = bot.Pos,
                });
            });
        }
    }
}
