using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using Ship = AW2.Game.Gobs.Ship;

namespace AW2.Game
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
        #region Weapon fields

        /// <summary>
        /// Names of the weapon type upgrades of the weapon.
        /// </summary>
        [TypeParameter]
        CanonicalString[] upgradeNames;

        /// <summary>
        /// Indices of the bones that defines the weapon's barrels' locations 
        /// on the owning ship.
        /// </summary>
        protected int[] boneIndices;

        /// <summary>
        /// What type of gobs the weapon shoots out.
        /// </summary>
        [TypeParameter]
        protected CanonicalString shotTypeName;

        /// <summary>
        /// Recoil momentum of the weapon, measured in Newton seconds.
        /// Recoil pushes the shooter to the opposite direction.
        /// </summary>
        [TypeParameter]
        float recoilMomentum;

        #endregion // Weapon fields

        #region Weapon properties
        
        /// <summary>
        /// Names of the weapon type upgrades of the weapon, in order of upgrades.
        /// </summary>
        public CanonicalString[] UpgradeNames { get { return upgradeNames; } }

        #endregion // Weapon properties

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Weapon()
        {
            this.upgradeNames = new CanonicalString[] { (CanonicalString)"dummyweapontype" };
            this.shotTypeName = (CanonicalString)"dummygobtype";
            this.loadTime = 0.5f;
            this.recoilMomentum = 10000;
        }

        /// <summary>
        /// Creates a new weapon of the specified type.
        /// </summary>
        /// <param name="typeName">The type of the weapon.</param>
        protected Weapon(CanonicalString typeName)
            : base(typeName)
        {
            boneIndices = new int[] { 0 };
        }

        #region Weapon public methods

        /// <summary>
        /// Attaches the weapon to a ship.
        /// </summary>
        /// <param name="owner">The ship to attach to.</param>
        /// <param name="ownerHandle">A handle for identifying the weapon at the owner.</param>
        /// <param name="boneIndices">Indices of the bones that define the locations of the
        /// barrels of the weapon on the ship.</param>
        public void AttachTo(Ship owner, OwnerHandleType ownerHandle, int[] boneIndices)
        {
            base.AttachTo(owner, ownerHandle);
            this.boneIndices = boneIndices;
        }

        #endregion Weapon public methods

        #region Weapon protected methods

        /// <summary>
        /// Applies recoil to the owner of the weapon.
        /// Subclasses should call this method when they emit a new shot
        /// during a firing action.
        /// </summary>
        protected void ApplyRecoil()
        {
            Vector2 momentum = new Vector2((float)Math.Cos(owner.Rotation), (float)Math.Sin(owner.Rotation))
                * -recoilMomentum;
            AssaultWing.Instance.DataEngine.CustomOperations += () => { AssaultWing.Instance.PhysicsEngine.ApplyMomentum(owner, momentum); };
        }

        #endregion Weapon protected methods
    }
}
