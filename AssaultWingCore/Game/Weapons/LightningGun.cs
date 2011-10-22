using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Weapons
{
    /// <summary>
    /// A weapon that shoots auto-targeting lightnings.
    /// </summary>
    public class LightningGun : Weapon
    {
        private const int LIGHTNING_CHAIN_LENGTH_MAX = 3;

        /// <summary>
        /// Maximum distance the lightning can carry, in meters.
        /// </summary>
        [TypeParameter]
        private float _range;

        /// <summary>
        /// Ratio of range of a lightning chain link compared to the previous link.
        /// </summary>
        [TypeParameter]
        private float _chainLinkRangeMultiplier;

        private TargetSelector _targetSelector = new TargetSelector(0); // maxRange is set later;

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public LightningGun()
        {
            _range = 500;
            _chainLinkRangeMultiplier = 0.8f;
        }

        public LightningGun(CanonicalString typeName)
            : base(typeName)
        {
        }

        protected override void ShootImpl()
        {
            FireAtTargets(FindTargets(Owner.Game.DataEngine.Minions));
        }

        public IEnumerable<Gob> FindTargets(IEnumerable<Gob> potentialTargets)
        {
            var current = Owner;
            var direction = Owner.Rotation;
            _targetSelector.MaxRange = _range;
            for (int i = 0; i < LIGHTNING_CHAIN_LENGTH_MAX; i++)
            {
                var target = _targetSelector.ChooseTarget(potentialTargets, current, direction);
                if (target == null)
                {
                    if (i == 0) yield return null;
                    break;
                }
                yield return target;
                direction = (target.Pos - current.Pos).Angle();
                current = target;
                _targetSelector.MaxRange *= _chainLinkRangeMultiplier;
            }
        }

        private void FireAtTargets(IEnumerable<Gob> targets)
        {
            ForEachShipBarrel(ShipBarrelTypes.Middle, (index, rotation) => CreateShot(Owner, index, targets.First()));
            var previous = targets.First();
            foreach (var target in targets.Skip(1))
            {
                CreateShot(previous, 0, target);
                previous = target;
            }
        }

        private void CreateShot(Gob source, int sourceBoneIndex, Gob target)
        {
            Gob.CreateGob<Lightning>(Owner.Game, _shotTypeName, shot =>
            {
                shot.ResetPos(Vector2.Zero, Vector2.Zero, Gob.DEFAULT_ROTATION); // pos not used but must be initialized
                shot.Owner = PlayerOwner;
                shot.Shooter = source;
                shot.ShooterBoneIndex = sourceBoneIndex;
                shot.Target = target;
                Arena.Gobs.Add(shot);
            });
        }
    }
}
