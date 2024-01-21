using System;
using System.Collections.Generic;
using System.Linq;

namespace AW2.Game.Collisions
{
    public struct CollisionMaterial
    {
        private static CollisionMaterial[] g_collisionMaterials;

        public static CollisionMaterial Get(CollisionMaterialType type)
        {
            return g_collisionMaterials[(int)type];
        }

        static CollisionMaterial()
        {
            g_collisionMaterials = new CollisionMaterial[Enum.GetValues(typeof(CollisionMaterialType)).Length];
            for (int i = 0; i < g_collisionMaterials.Length; ++i) g_collisionMaterials[i].Elasticity = -1;

            g_collisionMaterials[(int)CollisionMaterialType.Regular] = new CollisionMaterial
            {
                Elasticity = 0.2f,
                Friction = 0.45f,
                Damage = 1.0f,
            };
            g_collisionMaterials[(int)CollisionMaterialType.Rough] = new CollisionMaterial
            {
                Elasticity = 0.01f,
                Friction = 0.4f,
                Damage = 1.0f,
            };
            g_collisionMaterials[(int)CollisionMaterialType.Bouncy] = new CollisionMaterial
            {
                Elasticity = 0.4f,
                Friction = 0.5f,
                Damage = 1.0f,
            };
            g_collisionMaterials[(int)CollisionMaterialType.Sticky] = new CollisionMaterial
            {
                Elasticity = 0.0f,
                Friction = 1.0f,
                Damage = 0.0f,
            };

            if (g_collisionMaterials.Any(mat => mat.Elasticity == -1))
                throw new ApplicationException("Invalid number of collision materials defined");
        }

        /// <summary>
        /// Elasticity factor of the collision area. Zero means no collision bounce.
        /// One means fully elastic collision.
        /// </summary>
        /// The elasticity factors of both colliding collision areas affect the final elasticity
        /// of the collision. Avoid using zero; instead, use a very small number.
        /// Use a number above one to regain fully elastic collisions even
        /// when countered by inelastic gobs.
        public float Elasticity;

        /// <summary>
        /// Friction factor of the collision area. Zero means that movement along the
        /// collision surface is not slowed by friction.
        /// </summary>
        /// The friction factors of both colliding collision areas affect the final friction
        /// of the collision. It's a good idea to use values that are closer to
        /// zero than one.
        public float Friction;

        /// <summary>
        /// Multiplier for collision damage.
        /// </summary>
        public float Damage;
    }
}
