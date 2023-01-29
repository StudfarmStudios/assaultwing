using System;
using Microsoft.Xna.Framework;

namespace AW2.Core
{
    /// <summary>
    /// Like <see cref="Microsoft.Xna.Framework.GameTime"/> but with TotalRealTime.
    /// </summary>
    public class AWGameTime
    {
        public TimeSpan ElapsedGameTime { get; private set; }
        public TimeSpan TotalGameTime { get; private set; }
        public TimeSpan TotalRealTime { get; private set; }

        public AWGameTime() { }

        public AWGameTime(TimeSpan totalGameTime, TimeSpan elapsedGameTime, TimeSpan totalRealTime)
        {
            TotalGameTime = totalGameTime;
            ElapsedGameTime = elapsedGameTime;
            TotalRealTime = totalRealTime;
        }
    }
}
