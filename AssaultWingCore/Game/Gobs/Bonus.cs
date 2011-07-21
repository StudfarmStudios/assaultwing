using System;
using AW2.Core;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Game.Gobs
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
        private float _lifetime;

        /// <summary>
        /// Time at which the bonus dies, in game time.
        /// </summary>
        private TimeSpan _deathTime;

        /// <summary>
        /// What happens when the bonus is collected.
        /// </summary>
        [TypeParameter]
        private CanonicalString _bonusActionTypeName;

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Bonus()
        {
            _lifetime = 10;
        }

        public Bonus(CanonicalString typeName)
            : base(typeName)
        {
            _bonusActionTypeName = (CanonicalString)"dummygob";
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

        public override Arena.CollisionSideEffectType Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck, Arena.CollisionSideEffectType sideEffectTypes)
        {
            // We assume we have only one receptor area and that's the one for
            // bonus collection. That means that the other gob is a ship.
            if (myArea.Type == CollisionAreaType.Receptor && theirArea.Owner is Ship)
            {
                if (sideEffectTypes.HasFlag(AW2.Game.Arena.CollisionSideEffectType.Irreversible))
                {
                    DoBonusAction(theirArea.Owner.Owner);
                    Game.SoundEngine.PlaySound("BonusCollection", this);
                    Die();
                    return Arena.CollisionSideEffectType.Irreversible;
                }
            }
            return Arena.CollisionSideEffectType.None;
        }

        private void DoBonusAction(Player player)
        {
            if (Game.NetworkMode == NetworkMode.Client) return;
            var gameAction = BonusAction.Create<BonusAction>(_bonusActionTypeName, player, gob => { });
            if (gameAction == null) return;
            Gob.CreateGob<ArenaMessage>(Game, (CanonicalString)"bonusmessage", gob =>
            {
                gob.ResetPos(Pos, Vector2.Zero, Gob.DEFAULT_ROTATION);
                gob.Message = gameAction.BonusText;
                gob.IconName = gameAction.BonusIconName;
                gob.DrawColor = gameAction.Owner.PlayerColor;
                Game.DataEngine.Arena.Gobs.Add(gob);
            });
            player.Messages.Add(new PlayerMessage("You collected " + gameAction.BonusText, player.PlayerColor));
        }
    }
}
