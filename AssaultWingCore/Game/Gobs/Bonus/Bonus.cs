using System;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using Microsoft.Xna.Framework;

namespace AW2.Game.Gobs.Bonus
{
    /// <summary>
    /// A bonus that can be collected by a player.
    /// </summary>
    public class Bonus : Gob, IConsistencyCheckable
    {
        /// <summary>
        /// Lifetime of the bonus, in seconds.
        /// </summary>
        [TypeParameter]
        protected float _lifetime;

        /// <summary>
        /// Time at which the bonus dies, in game time.
        /// </summary>
        [RuntimeState]
        protected TimeSpan _deathTime;

        /// <summary>
        /// The duration of the bonus, in seconds.
        /// </summary>
        /// Bonus that don't have a meaningful duration
        /// leave this field uninterpreted.
        [TypeParameter]
        protected float _duration;

        /// <summary>
        /// What happens when the bonus is collected.
        /// </summary>
        [TypeParameter]
        protected GameAction _gameAction;

        /// This constructor is only for serialisation.
        public Bonus()
        {
            _lifetime = 10;
            _deathTime = new TimeSpan(0, 1, 20);
        }

        public Bonus(CanonicalString typeName)
            : base(typeName)
        {
        }

        #region Methods related to gobs' functionality in the game world

        public override void Activate()
        {
            base.Activate();
            _deathTime = Arena.TotalTime + TimeSpan.FromSeconds(_lifetime);
        }

        public override void Update()
        {
            base.Update();
            if (_deathTime <= Arena.TotalTime)
                Die();
        }

        #endregion Methods related to gobs' functionality in the game world

        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
            // We assume we have only one receptor area and that's the one for
            // bonus collection. That means that the other gob is a ship.
            if (myArea.Type == CollisionAreaType.Receptor)
            {
                if (Game.NetworkMode != NetworkMode.Client)
                    DoBonusAction(theirArea.Owner.Owner);
                Game.SoundEngine.PlaySound("BonusCollection", this);
                Die();
            }
        }

        private void DoBonusAction(Player player)
        {
            _gameAction.Player = player;
            _gameAction.SetDuration(_duration);
            if (!_gameAction.DoAction())
            {
                player.Messages.Add(new PlayerMessage("Useless bonus discarded", PlayerMessage.DEFAULT_COLOR));
                return;
            }

            Gob.CreateGob<ArenaMessage>(Game, (CanonicalString)"bonusmessage", gob =>
            {
                gob.ResetPos(Pos, Vector2.Zero, Gob.DEFAULT_ROTATION);
                gob.Message = _gameAction.BonusText;
                gob.IconName = _gameAction.BonusIconName;
                gob.DrawColor = _gameAction.Player.PlayerColor;
                Game.DataEngine.Arena.Gobs.Add(gob);
            });
            player.BonusActions.AddOrReplace(_gameAction);
            player.Messages.Add(new PlayerMessage("You collected " + _gameAction.BonusText, player.PlayerColor));
        }
    }
}
