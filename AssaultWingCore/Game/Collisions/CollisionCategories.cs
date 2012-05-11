using System;
using System.Collections.Generic;
using System.Linq;
using FarseerPhysics.Dynamics;

namespace AW2.Game.Collisions
{
    /// <summary>
    /// Contains static methods for interoperation of <see cref="AW2.Game.Collisions.CollisionAreaType"/>
    /// with <see cref="FarseerPhysics.Dynamics.Category"/>.
    /// </summary>
    public static class CollisionCategories
    {
        private static bool[] g_isPhysical;
        private static Category[] g_collidesWith;

        private static int CollisionAreaTypeCount { get { return Enum.GetValues(typeof(CollisionAreaType)).Length; } }

        static CollisionCategories()
        {
            InitializeIsPhysical();
            InitializeCollidesWith();
        }

        private static void InitializeIsPhysical()
        {
            g_isPhysical = new bool[CollisionAreaTypeCount];
            SetIsPhysical(CollisionAreaType.Common, CollisionAreaType.MinePhysical, CollisionAreaType.Shot, CollisionAreaType.Static);
        }

        private static void InitializeCollidesWith()
        {
            g_collidesWith = new Category[CollisionAreaTypeCount];
            SetCollidesWith(CollisionAreaType.BonusCollect, CollisionAreaType.Common);
            SetCollidesWith(CollisionAreaType.Common, CollisionAreaType.Common, CollisionAreaType.Static, CollisionAreaType.MinePhysical);
            SetCollidesWith(CollisionAreaType.Damage, CollisionAreaType.Common, CollisionAreaType.Static, CollisionAreaType.MinePhysical);
            SetCollidesWith(CollisionAreaType.DockRepair, CollisionAreaType.Common);
            SetCollidesWith(CollisionAreaType.Flow, CollisionAreaType.Common, CollisionAreaType.Shot);
            SetCollidesWith(CollisionAreaType.MineMagnet, CollisionAreaType.Common);
            SetCollidesWith(CollisionAreaType.MineSpread, CollisionAreaType.MinePhysical);
            SetCollidesWith(CollisionAreaType.MinePhysical, CollisionAreaType.Common, CollisionAreaType.Static, CollisionAreaType.MinePhysical);
            SetCollidesWith(CollisionAreaType.Shot, CollisionAreaType.Common, CollisionAreaType.Static, CollisionAreaType.MinePhysical);
            SetCollidesWith(CollisionAreaType.Static, CollisionAreaType.Common, CollisionAreaType.MinePhysical);
        }

        public static bool IsPhysical(this CollisionAreaType collisionAreaType)
        {
            return g_isPhysical[(int)collisionAreaType];
        }

        public static Category Category(this CollisionAreaType collisionAreaType)
        {
            return (Category)(1 << (int)collisionAreaType);
        }

        public static Category CollidesWith(this CollisionAreaType collisionAreaType)
        {
            return g_collidesWith[(int)collisionAreaType];
        }

        private static void SetIsPhysical(params CollisionAreaType[] who)
        {
            foreach (var c in who) g_isPhysical[(int)c] = true;
        }

        private static void SetCollidesWith(CollisionAreaType who, params CollisionAreaType[] collidesWith)
        {
            var union = 0;
            foreach (var with in collidesWith) union |= 1 << (int)with;
            g_collidesWith[(int)who] |= (Category)union;

            // Symmetric closure for Farseer.
            foreach (var with in collidesWith) g_collidesWith[(int)with] |= (Category)(1 << (int)who);
        }
    }
}
