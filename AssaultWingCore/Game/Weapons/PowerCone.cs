using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.Collisions;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Helpers.Serialization;

namespace AW2.Game.Weapons
{
    /// <summary>
    /// Deals damage across a conic area and across an area surrounding the shooter.
    /// </summary>
    public class PowerCone : Weapon
    {
        [TypeParameter]
        private CanonicalString[] _surroundEffects;
        [TypeParameter]
        private CollisionArea _surroundArea;
        [TypeParameter]
        private float _surroundDamage;

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public PowerCone()
        {
            _surroundEffects = new[] { (CanonicalString)"dummypeng" };
            _surroundArea = new CollisionArea("Hit", new Circle(Vector2.Zero, 100), null, CollisionAreaType.Receptor,
                CollisionAreaType.PhysicalDamageable, CollisionAreaType.None, CollisionMaterialType.Regular);
            _surroundDamage = 500;
        }

        public PowerCone(CanonicalString typeName)
            : base(typeName)
        {
        }

        protected override void ShootImpl()
        {
            var surroundHost = CreateShot();
            if (surroundHost != null) CreateSurroundingBlow(surroundHost);
        }

        private Gob CreateShot()
        {
            Gob createdShot = null;
            Gob.CreateGob<Triforce>(Owner.Game, _shotTypeName, shot =>
            {
                shot.ResetPos(Owner.Pos, Vector2.Zero, Owner.Rotation);
                shot.Owner = PlayerOwner;
                shot.Host = Owner;
                Arena.Gobs.Add(shot);
                createdShot = shot;
            });
            return createdShot;
        }

        private void CreateSurroundingBlow(Gob host)
        {
            GobHelper.CreatePengs(_surroundEffects, Owner);
            _surroundArea.Owner = host;
            foreach (var victim in Arena.GetOverlappingGobs(_surroundArea, _surroundArea.CollidesAgainst))
                if (victim != Owner) victim.InflictDamage(_surroundDamage, new DamageInfo(Owner));
            _surroundArea.Owner = null;
        }
    }
}
