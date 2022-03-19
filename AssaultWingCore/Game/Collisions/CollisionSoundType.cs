namespace AW2.Game.Collisions
{
    public enum CollisionSoundType
    {
        None = 0x00,
        Collision = 0x01,
        ShipCollision = 0x02,
    }

    public static class CollisionSoundTypeExtension
    {
        public static string EffectName (this CollisionSoundType sound)
        {
            switch(sound)
            {
                case CollisionSoundType.Collision: 
                    return "collision";
                case CollisionSoundType.ShipCollision: 
                    return "shipCollision";
                default:
                    return "none";
            }
        }
    }
}
