using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AW2.Helpers;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Game
{
    [LimitedSerialization]
    public class GameAction : Clonable
    {
        [TypeParameter]
        protected CanonicalString name;

        protected Player player;


        protected string bonusText;
        protected string bonusIconName;
        protected Texture2D bonusIcon;

        public Player Player { get { return player; } set { player = value; } }
        public String BonusText { get { return bonusText; } }
        public String BonusIconName { get { return bonusIconName; } }
        public Texture2D BonusIcon { get { return bonusIcon; } }

        /// <summary>
        /// Starting times of the player's GameAction.
        /// </summary>
        /// Starting time is the time when the gameaction was activated.
        /// <seealso cref="PlayerBonus"/>
        public TimeSpan actionTimeins;

        /// <summary>
        /// Ending times of the player's GameAction.
        /// </summary>
        /// <seealso cref="PlayerBonus"/>
        public TimeSpan actionTimeouts;

        /// <summary>
        /// Creates an uninitialised bonus.
        /// </summary>
        /// This constructor is only for serialisation.
        public GameAction():base()
        {
            name = (CanonicalString)"dummyaction";
        }
        /// <summary>
        /// Creates a new gameaction.
        /// </summary>
        /// <param name="typeName">Type of the action.</param>
        public GameAction(CanonicalString typeName)
            : base(typeName)
        {
        }

        /// <summary>
        /// Action method. Contains logic for enabling the action
        /// </summary>
        /// <param name="duration">Time how long the action is active</param>
        public virtual void DoAction(float duration)
        {
            actionTimeins = AssaultWing.Instance.GameTime.TotalGameTime;
            actionTimeouts = AssaultWing.Instance.GameTime.TotalGameTime + TimeSpan.FromSeconds(duration);
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
                player.MustUpdateToClients = true;
        }

        /// <summary>
        /// Returs the default state
        /// </summary>
        public virtual void RemoveAction()
        {

        }

        /*not used for anything*/
        /* this should be enabled if Actions are defined in own XML files
        public GameAction CreateAction()
        {
            GameAction action = (GameAction)Clonable.Instantiate(name);
            return action;
        }
         * */

    }
}
