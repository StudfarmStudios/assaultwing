using System;

namespace AW2.Game.GobUtils
{
    public class GameActionTypeAttribute : Attribute
    {
        public int ID { get; private set; }

        public GameActionTypeAttribute(int id)
        {
            ID = id;
        }
    }
}
