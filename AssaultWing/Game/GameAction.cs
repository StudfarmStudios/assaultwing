using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AW2.Net;

namespace AW2.Game
{
    [LimitedSerialization]
    public class GameAction : INetworkSerializable
    {
        [TypeParameter]
        protected string bonusText;
        [TypeParameter]
        protected string bonusIconName;

        protected Player player;
        protected Texture2D bonusIcon;

        public Player Player { get { return player; } set { player = value; } }
        public String BonusText { get { return bonusText; } protected set { bonusText = value; } }
        public String BonusIconName { get { return bonusIconName; } }
        public Texture2D BonusIcon { get { return bonusIcon; } }

        /// <summary>
        /// Starting times of the player's GameAction.
        /// Starting time is the time when the gameaction was activated.
        /// <seealso cref="PlayerBonus"/>
        public TimeSpan actionTimeins;

        /// <summary>
        /// Ending times of the player's GameAction.
        /// </summary>
        /// <seealso cref="PlayerBonus"/>
        public TimeSpan actionTimeouts;

        /// <summary>
        /// This constructor is only for serialization.
        /// </summary>
        public GameAction()
        {
            bonusText = "unknown bonus";
            bonusIconName = "dummytexture";
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
        /// Returns the default state
        /// </summary>
        public virtual void RemoveAction()
        {
        }

        /// <summary>
        /// Actions that do something when active
        /// </summary>
        public virtual void Update()
        {
        }

        #region INetworkSerializable Members

        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, TimeSpan messageAge)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
