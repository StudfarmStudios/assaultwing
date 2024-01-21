using System;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game.Weapons;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// A weapon, usually used by a ship it is attached to.
    /// </summary>
    /// A weapon can be fired only when it's loaded and it has a sufficient amount
    /// of charge for firing. When a weapon is fired, it becomes unloaded and a
    /// fixed amount of charge is reduced from it. The weapon will be loaded again
    /// after a fixed amount of time, and the level of charge regenerates
    /// at a fixed rate. Charge is managed totally by <b>Gobs.Ship</b>.
    /// 
    /// The weapon's functionality is implemented in a subclass.
    /// An instance of a subclass of Weapon represents one weapon instance
    /// that is usually attached to a ship and has its own load meters and other
    /// gametime properties.
    /// 
    /// HOW TO IMPLEMENT A WEAPON:
    /// The subclass's implementation should first check <see cref="CanFire"/> if
    /// the weapon is able to fire at all. When firing really starts, call
    /// <see cref="StartFiring"/>. When the firing process has stopped, call
    /// <see cref="DoneFiring"/>. This keeps load times in order. If the weapon has recoil, call
    /// <see cref="ApplyRecoil"/> to apply recoil at the end of the frame when
    /// it doesn't have unpleasant side effects.
    ///
    /// There can be several special instances of each subclass of Weapon. Each of
    /// these 'template instances' defines a weapon type by specifying values for the
    /// type parameters of that Weapon subclass. Newly created weapon instances automatically
    /// initialise their type parameter fields by copying them from a template instance.
    /// Template instances are referred to by human-readable names such as "shotgun".
    /// 
    /// Class Weapon and its subclasses use limited (de)serialisation for
    /// for saving and loading weapon types. Therefore only those fields
    /// that describe the weapon type -- not fields that describe the weapon's 
    /// state during gameplay -- should be marked as 'type parameters' by 
    /// <b>TypeParameterAttribute</b>.
    /// 
    /// Each Weapon subclass must provide a parameterless constructor that initialises all
    /// of its type parameters to descriptive and exemplary default values.
    /// <see cref="AW2.Helpers.TypeParameterAttribute"/>
    public abstract class Weapon : ShipDevice
    {
        protected delegate void ShipBarrelAction(int barrelBoneIndex, float barrelRotation);

        [TypeParameter]
        private WeaponInfo _weaponInfo;

        /// <summary>
        /// Names of the weapon type upgrades of the weapon.
        /// </summary>
        [TypeParameter]
        private CanonicalString[] _upgradeNames;

        /// <summary>
        /// What type of gobs the weapon shoots out.
        /// </summary>
        [TypeParameter]
        protected CanonicalString _shotTypeName;

        /// <summary>
        /// Recoil momentum of the weapon, measured in Newton seconds.
        /// Recoil pushes the shooter to the opposite direction.
        /// </summary>
        [TypeParameter]
        private float _recoilMomentum;

        /// <summary>
        /// Names of the weapon type upgrades of the weapon, in order of upgrades.
        /// </summary>
        public CanonicalString[] UpgradeNames { get { return _upgradeNames; } }

        public WeaponInfo WeaponInfo { get { return _weaponInfo; } }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Weapon()
        {
            _weaponInfo = new WeaponInfo();
            _upgradeNames = new[] { (CanonicalString)"dummyweapontype" };
            _shotTypeName = (CanonicalString)"dummygobtype";
            _recoilMomentum = 10000;
        }

        protected Weapon(CanonicalString typeName)
            : base(typeName)
        {
        }

        /// <summary>
        /// Applies recoil to the owner of the weapon.
        /// Subclasses should call this method when they emit a new shot
        /// during a firing action.
        /// </summary>
        protected void ApplyRecoil()
        {
            var momentum = -_recoilMomentum * AWMathHelper.GetUnitVector2(Owner.Rotation);
            Owner.Game.PostFrameLogicEngine.DoOnce += () => PhysicsHelper.ApplyImpulse(Owner, momentum);
        }

        protected void ForEachShipBarrel(ShipBarrelTypes barrelTypes, ShipBarrelAction action)
        {
            if ((barrelTypes &
                ~(ShipBarrelTypes.Middle |
                  ShipBarrelTypes.Left |
                  ShipBarrelTypes.Right |
                  ShipBarrelTypes.Rear)) != 0)
                throw new ApplicationException("Unknown ShipBarrelTypes " + barrelTypes);
            if ((barrelTypes & ShipBarrelTypes.Middle) != 0) action(GetBarrelBoneIndex(0), 0);
            if ((barrelTypes & ShipBarrelTypes.Left) != 0) action(GetBarrelBoneIndex(1), 0);
            if ((barrelTypes & ShipBarrelTypes.Right) != 0) action(GetBarrelBoneIndex(2), 0);
            if ((barrelTypes & ShipBarrelTypes.Rear) != 0) action(GetBarrelBoneIndex(3), MathHelper.Pi);
        }

        /// <summary>
        /// Returns the 3D model bone index corresponding to a barrel.
        /// </summary>
        private int GetBarrelBoneIndex(int barrelIndex)
        {
            var boneIndices = Owner.BarrelBoneIndices;
            return barrelIndex >= boneIndices.Length ? 0 : boneIndices[barrelIndex];
        }
    }
}
