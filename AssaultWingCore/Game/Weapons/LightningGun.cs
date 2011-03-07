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

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public LightningGun()
        {
            _range = 500;
        }

        public LightningGun(CanonicalString typeName)
            : base(typeName)
        {
        }

        protected override void ShootImpl()
        {
            var potentialTargets =
                from player in Owner.Game.DataEngine.Players
                where player.Ship != null && player.Ship != Owner
                select player.Ship;
            FireAtTargets(FindTargets(potentialTargets));
        }

        protected override void CreateVisualsImpl()
        {
        }

        public IEnumerable<Gob> FindTargets(IEnumerable<Gob> potentialTargets)
        {
            Gob current = Owner;
            var direction = Owner.Rotation;
            var range = _range;
            for (int i = 0; i < LIGHTNING_CHAIN_LENGTH_MAX; i++)
            {
                var target = TargetSelection.ChooseTarget(potentialTargets, current, direction, range);
                if (target == null)
                {
                    if (i == 0) yield return null;
                    break;
                }
                yield return target;
                direction = (target.Pos - current.Pos).Angle();
                current = target;
                range *= _chainLinkRangeMultiplier;
            }
        }

        private void FireAtTargets(IEnumerable<Gob> targets)
        {
            ForEachShipBarrel(ShipBarrelTypes.Middle, (index, rotation) => CreateShot(Owner, index, targets.First()));
            Gob previous = targets.First();
            foreach (var target in targets.Skip(1))
            {
                CreateShot(previous, 0, target);
                previous = target;
            }
        }

        private void CreateShot(Gob source, int sourceBoneIndex, Gob target)
        {
            Gob.CreateGob<Lightning>(Owner.Game, shotTypeName, shot =>
            {
                shot.Owner = source.Owner;
                shot.Shooter = source;
                shot.ShooterBoneIndex = sourceBoneIndex;
                shot.Target = target;
                Arena.Gobs.Add(shot);
            });
        }
    }
}
