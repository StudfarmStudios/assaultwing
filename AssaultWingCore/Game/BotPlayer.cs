using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game.Gobs;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using Rectangle = AW2.Helpers.Geometric.Rectangle;

namespace AW2.Game
{
    public class BotPlayer : Spectator
    {
        private const int MAX_BOT_AND_SHIP_COUNT = 0; // UNDONE: Temporarily in release 1.4.0.0 bots are turned off. This value should otherwise be 4.
        private readonly TimeSpan BOT_CREATION_INTERVAL = TimeSpan.FromSeconds(9.5);

        private TimeSpan _nextBotCreationTime;

        private Arena Arena { get { return Game.DataEngine.Arena; } }
        private bool EnoughBots { get { return Arena.Gobs.GameplayLayer.Gobs.Count(gob => gob is Bot || gob is Ship) >= MAX_BOT_AND_SHIP_COUNT; } }

        public BotPlayer(AssaultWingCore game, int connectionID = Spectator.CONNECTION_ID_LOCAL)
            : base(game, connectionID)
        {
            Name = "The Bots";
        }

        public override void ResetForArena()
        {
            base.ResetForArena();
            _nextBotCreationTime = BOT_CREATION_INTERVAL;
        }

        public override void Update()
        {
            base.Update();
            CreateBot();
        }

        private void CreateBot()
        {
            if (_nextBotCreationTime > Game.GameTime.TotalGameTime) return;
            _nextBotCreationTime = Game.GameTime.TotalGameTime + BOT_CREATION_INTERVAL;
            if (EnoughBots) return;
            Gob.CreateGob<Bot>(Game, (CanonicalString)"rocket bot", bot =>
            {
                var pos = Arena.GetFreePosition(bot, Arena.BoundedArea);
                bot.ResetPos(pos, Vector2.Zero, Gob.DEFAULT_ROTATION);
                bot.Owner = this;
                Arena.Gobs.Add(bot);
            });
        }
    }
}
