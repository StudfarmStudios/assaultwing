using System;
using Microsoft.Xna.Framework;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

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
            Owner.Game.Stats.Send(new { Fired = PlayerOwner.LoginToken, Role = OwnerHandle, Type = TypeName.Value });
            Owner.InflictDamage(Owner.MaxDamageLevel - Owner.DamageLevel + 1, new DamageInfo(Owner, ignoreLastDamager: true));
            foreach (var gobType in deathGobTypes)
                Gob.CreateGob<Gob>(Owner.Game, gobType, gob =>
                {
                    gob.ResetPos(Owner.Pos, Vector2.Zero, Owner.Rotation);
                    gob.Owner = SpectatorOwner;
                    Arena.Gobs.Add(gob);
                });
        }
    }
}
