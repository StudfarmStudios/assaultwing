using System;

namespace AW2.Game.Weapons
{
    [Flags]
    public enum ShipBarrelTypes
    {
        None = 0x00,
        Middle = 0x01,
        Left = 0x02,
        Right = 0x04,
        Rear = 0x08
    };
}
