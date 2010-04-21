using System;
using Microsoft.Xna.Framework;

namespace AW2.Game
{
    public class AWGameTime : GameTime
    {
        public TimeSpan TotalArenaTime { get; private set; }

        public AWGameTime() { }

        private AWGameTime(GameTime gameTime, TimeSpan totalArenaTime)
            : base(gameTime.TotalRealTime, gameTime.ElapsedRealTime, gameTime.TotalGameTime, gameTime.ElapsedGameTime)
        {
            TotalArenaTime = totalArenaTime;
        }

        public AWGameTime Update(GameTime gameTime, bool updateArenaTime)
        {
            var arenaTime = updateArenaTime ? TotalArenaTime + gameTime.ElapsedGameTime : TotalArenaTime;
            return new AWGameTime(gameTime, arenaTime);
        }

        public AWGameTime ResetArenaTime()
        {
            return new AWGameTime(this, TimeSpan.Zero);
        }
    }
}
