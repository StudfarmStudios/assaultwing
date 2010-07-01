using System;
using Microsoft.Xna.Framework;
using AW2.Game.GobUtils;
using AW2.Helpers;

namespace AW2.Game.Weapons
{
    public class SelfDestruct : Weapon
    {
        [TypeParameter, ShallowCopy]
        CanonicalString[] deathGobTypes;

        /// This constructor is only for serialisation.
        public SelfDestruct()
        {
            deathGobTypes = new CanonicalString[0];
        }

        public SelfDestruct(CanonicalString typeName)
            : base(typeName)
        {
        }

        protected override void ShootImpl()
        {
            owner.InflictDamage(owner.MaxDamageLevel - owner.DamageLevel - 1, new DeathCause(owner, DeathCauseType.Damage, owner));
            foreach (var gobType in deathGobTypes)
                Gob.CreateGob<Gob>(gobType, gob =>
                {
                    gob.ResetPos(owner.Pos, Vector2.Zero, owner.Rotation);
                    gob.Owner = owner.Owner;
                    Arena.Gobs.Add(gob);
                });
        }

        protected override void CreateVisuals()
        {
        }
    }
}
