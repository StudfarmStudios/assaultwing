using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Helpers;
using AW2.Net;
using AW2.Sound;

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

        /// <summary>
        /// Fires the weapon.
        /// </summary>
        protected override void FireImpl(AW2.UI.ControlState triggerState)
        {
            owner.SelfDestruct(deathGobTypes);
            owner.DamageLevel = owner.MaxDamageLevel * 10;
            owner.Die(new DeathCause(owner, DeathCauseType.Damage));
        }

        public override void Activate()
        {
            FireMode = FireModeType.Single;
        }

        public override void Update()
        {
        }

        /// <summary>
        /// Releases all resources allocated by the weapon.
        /// </summary>
        public override void Dispose()
        {
        }

        #region INetworkSerializable Members

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                writer.Write((bool)false); // HACK: write dummy boolean to imitate ForwardShot, this works around a bug
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, TimeSpan messageAge)
        {
            base.Deserialize(reader, mode, messageAge);
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                reader.ReadBoolean(); // HACK: read dummy boolean to imitate ForwardShot, this works around a bug
            }
        }

        #endregion
    }
}
