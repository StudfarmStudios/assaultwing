using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Weapons
{
    /// <summary>
    /// A weapon that shoots gobs forward.
    /// Each firing can consist of a number of shots being fired.
    /// The shots are shot at even temporal intervals in a random
    /// angle. The shot angles distribute evenly in an angular fan
    /// whose center is directed at the direction of the weapon's owner.
    /// </summary>
    public class ForwardShot : Weapon
    {
        #region ForwardShot fields

        /// <summary>
        /// The ship gun barrels this weapon uses.
        /// </summary>
        [TypeParameter]
        private ShipBarrelTypes _gunBarrels;

        /// <summary>
        /// Names of muzzle fire engine types.
        /// </summary>
        [TypeParameter]
        private CanonicalString[] _muzzleFireEngineNames;

        /// <summary>
        /// How fast the shots leave the weapon barrel,
        /// in meters per second.
        /// </summary>
        [TypeParameter]
        private float _shotSpeed;

        /// <summary>
        /// Difference of the maximum and the minimum of a shot's random angle
        /// relative to the general shot direction.
        /// </summary>
        [TypeParameter]
        private float _shotAngleVariation;

        /// <summary>
        /// If true, shots are distributed evenly over the angle variation.
        /// Otherwise shot angles are chosen at random from the variation range.
        /// </summary>
        [TypeParameter]
        private bool _shotAngleEvenDistribution;

        /// <summary>
        /// Difference of the maximum and the minimum of a shot's random speed
        /// relative to the general shot speed.
        /// </summary>
        [TypeParameter]
        private float _shotSpeedVariation;

        #endregion ForwardShot fields

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public ForwardShot()
        {
            _gunBarrels = ShipBarrelTypes.Middle | ShipBarrelTypes.Left | ShipBarrelTypes.Right | ShipBarrelTypes.Rear;
            _muzzleFireEngineNames = new[] { (CanonicalString)"dummypeng" };
            _shotSpeed = 300f;
            _shotAngleVariation = 0.3f;
            _shotAngleEvenDistribution = false;
            _shotSpeedVariation = 20f;
        }

        public ForwardShot(CanonicalString typeName)
            : base(typeName)
        {
        }

        protected override void ShootImpl()
        {
            ForEachShipBarrel(_gunBarrels, CreateShot);
            ApplyRecoil();
        }

        protected override void CreateVisuals()
        {
            ForEachShipBarrel(_gunBarrels, CreateMuzzleFire);
        }

        private void CreateShot(int boneIndex, float barrelRotation)
        {
            var direction = barrelRotation + Owner.Rotation + GetShotAngleVariation();
            var kickSpeed = _shotSpeed + _shotSpeedVariation * RandomHelper.GetRandomFloat(-0.5f, 0.5f);
            var kick = kickSpeed * AWMathHelper.GetUnitVector2(direction);
            Gob.CreateGob<Gob>(Owner.Game, _shotTypeName, shot =>
            {
                shot.Owner = SpectatorOwner;
                shot.ResetPos(Owner.GetNamedPosition(boneIndex), Owner.Move + kick,
                    Owner.Rotation);  // 'Owner.Rotation' could also be 'direction' for a different angle
                Arena.Gobs.Add(shot);
            });
        }

        private float GetShotAngleVariation()
        {
            if (!_shotAngleEvenDistribution) return _shotAngleVariation * RandomHelper.GetRandomFloat(-0.5f, 0.5f);
            var step = _shotAngleVariation / 2 / ShotCount;
            var shotIndex = ShotCount - FiringOperator.ShotsLeft;
            var sign = (shotIndex % 2) * 2 - 1;
            return step * shotIndex * sign;
        }

        private void CreateMuzzleFire(int barrelBoneIndex, float barrelRotation)
        {
            foreach (var engineName in _muzzleFireEngineNames)
            {
                Gob.CreateGob<Peng>(Owner.Game, engineName, fireEngine =>
                {
                    fireEngine.Owner = SpectatorOwner;
                    fireEngine.Leader = Owner;
                    fireEngine.LeaderBone = barrelBoneIndex;
                    Arena.Gobs.Add(fireEngine);
                });
            }
        }
    }
}
