using System;
using AW2.Core;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.BonusActions
{
    [GameActionType(2)]
    [LimitedSerialization]
    public class DamageBuffBonusAction : GameAction
    {
        [TypeParameter]
        private string _buffName; // not CanonicalString because this doesn't contain any "well known string" such as a content texture name

        [TypeParameter]
        private CanonicalString _bonusIconName;

        [TypeParameter]
        private float _damagePerSecond;

        public Gob Cause { get; set; }

        /// <summary>
        /// This constructor is only for serialization.
        /// </summary>
        public DamageBuffBonusAction()
        {
            _buffName = "dummy damage buff";
            _bonusIconName = (CanonicalString)"dummytexture";
            _damagePerSecond = 500;
        }

        public DamageBuffBonusAction(string buffName, CanonicalString bonusIconName, float damagePerSecond)
        {
            _buffName = buffName;
            _bonusIconName = bonusIconName;
            _damagePerSecond = damagePerSecond;
        }

        public override bool DoAction()
        {
            SetActionMessage();
            return base.DoAction();
        }

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write(_buffName);
                writer.Write(_bonusIconName);
                writer.Write(_damagePerSecond);
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                _buffName = reader.ReadString();
                _bonusIconName = reader.ReadCanonicalString();
                _damagePerSecond = reader.ReadSingle();
            }
        }

        public override void Update()
        {
            // HACK: If the ship dies, the player's bonus actions are cleared, which results in a crash
            // because this method is called while iterating over the player's bonus actions.
            // Workaround: Call InflictDamage later this frame.
            float damage = Player.Game.PhysicsEngine.ApplyChange(_damagePerSecond, Player.Game.GameTime.ElapsedGameTime);
            Player.Game.PostFrameLogicEngine.DoOnce += () =>
            {
                if (Player.Ship != null)
                {
                    if (damage > 0)
                        Player.Ship.InflictDamage(damage, new DamageInfo(Cause));
                    else
                        Player.Ship.RepairDamage(-damage);
                }
            };
        }

        private void SetActionMessage()
        {
            BonusText = _buffName;
            BonusIconName = _bonusIconName;
        }
    }
}
