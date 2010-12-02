using System;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// Defines the identifier of a <see cref="GameAction"/> subclass.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class GameActionTypeAttribute : Attribute
    {
        public int ID { get; private set; }

        public GameActionTypeAttribute(int id)
        {
            ID = id;
        }
    }
}
