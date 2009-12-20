using System;
using System.Linq;
using AW2.Helpers;

namespace AW2.Game.Gobs.Bonus
{


    /// <summary>
    /// A bonus that can be collected by a player.
    /// </summary>
    public class DefinedBonus : Bonus
    {
        #region Bonus fields


        [TypeParameter]
        PlayerBonusTypes bonusType;
        
        #endregion Bonus fields

        /// <summary>
        /// Creates an uninitialised bonus.
        /// </summary>
        /// This constructor is only for serialisation.
        public DefinedBonus()
            : base()
        {
            this.lifetime = 10;
            this.deathTime = new TimeSpan(0, 1, 20);
            this.bonusType = new PlayerBonusTypes();
        }

        /// <summary>
        /// Creates a new bonus.
        /// </summary>
        /// <param name="typeName">Type of the bonus.</param>
        public DefinedBonus(CanonicalString typeName)
            : base(typeName)
        {

        }

        #region Methods related to gobs' functionality in the game world

        /// <summary>
        /// Activates the gob, i.e. performs an initialisation rite.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            Log.Write("A Defined bonus was activated, Type of Bonus: "+bonusType);
            deathTime = AssaultWing.Instance.GameTime.TotalGameTime + TimeSpan.FromSeconds(lifetime);
        }

        /// <summary>
        /// Updates the bonus's internal state.
        /// </summary>
        public override void Update()
        {
            base.Update();
            if (deathTime <= AssaultWing.Instance.GameTime.TotalGameTime)
                Die(new DeathCause());
        }

        #endregion Methods related to gobs' functionality in the game world

        /// <summary>
        /// Perform a bonus action on a player.
        /// </summary>
        /// <param name="player">The player to receive the bonus action.</param>
        protected override void DoBonusAction(Player player)
        {
            if (bonusType != PlayerBonusTypes.None)
            {
                TimeSpan expiryTime = AssaultWing.Instance.GameTime.TotalGameTime
                    + TimeSpan.FromSeconds(duration);
                player.AddBonus(bonusType, expiryTime);
                if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
                    player.MustUpdateToClients = true;

                // Display bonusmessage
                Gob.CreateGob((CanonicalString)"bonusmessage", gob =>
                {
                    gob.ResetPos(Pos, gob.Move, gob.Rotation);
                    var data = ((PlayerBonus)bonusType).GetData(player);
                    ((BonusMessage)gob).Message = data.message;
                    ((BonusMessage)gob).IconName = data.iconName;
                    AssaultWing.Instance.DataEngine.Arena.Gobs.Add(gob);
                });

            }
        }

        #region IConsistencyCheckable Members

        /// <summary>
        /// Makes the instance consistent in respect of fields marked with a
        /// limitation attribute.
        /// </summary>
        /// <param name="limitationAttribute">Check only fields marked with 
        /// this limitation attribute.</param>
        /// <see cref="Serialization"/>
        public new void MakeConsistent(Type limitationAttribute)
        {
            // NOTE: This method is meant to re-implement the interface member
            // IConsistencyCheckable.MakeConsistent(Type) that is already implemented
            // in the base class Gob. According to the C# Language Specification 1.2
            // (and not corrected in the specification version 2.0), adding the 'new'
            // keyword to this re-implementation would make this code
            // 
            //      Wall wall;
            //      ((IConsistencyCheckable)wall).MakeConsistent(type)
            //
            // call Gob.MakeConsistent(Type). However, debugging reveals this is not the
            // case. By leaving out the 'new' keyword, the semantics stays the same, as
            // seen by debugging, but the compiler produces a warning.
            base.MakeConsistent(limitationAttribute);
            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
                lifetime = Math.Max(0.5f, lifetime);
            }
        }

        #endregion
    }
}