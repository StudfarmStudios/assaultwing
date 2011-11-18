using System;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    public abstract class BonusAction : Gob
    {
        [TypeParameter]
        protected TimeSpan _duration;

        private bool _timeoutReset;

        public override BoundingSphere DrawBounds { get { return new BoundingSphere(); } }
        public virtual string BonusText { get { return TypeName; } }
        public abstract CanonicalString BonusIconName { get; }
        public TimeSpan Duration { get { return _duration; } }
        public TimeSpan EndTime { get; private set; }
        public Gob Host { get; private set; }

        /// <summary>
        /// Creates a <see cref="BonusAction"/> or if an action of the same type and typename
        /// already exists on the player, resets its timer. If an action of the same type but
        /// of a different typename exists, requested action replaces it.
        /// Returns the action that was created or whose timeout was reset,
        /// or returns null if no action was created or reset.
        /// </summary>
        public static T Create<T>(CanonicalString typeName, Player player, Action<T> init) where T : BonusAction
        {
            var host = player.Ship;
            var actionType = player.Game.DataEngine.GetTypeTemplate(typeName).GetType();
            var sameTypeActions = host.BonusActions.Where(ba => ba.GetType() == actionType);
            if (sameTypeActions.Any())
            {
                var oldAction = sameTypeActions.FirstOrDefault(ba => ba.TypeName == typeName);
                if (oldAction != null)
                {
                    oldAction.ResetTimeout();
                    return (T)oldAction;
                }
                sameTypeActions.First().Die();
            }
            T result = null;
            Gob.CreateGob<T>(player.Game, typeName, gob =>
            {
                gob.ResetPos(Vector2.Zero, Vector2.Zero, Gob.DEFAULT_ROTATION);
                gob.Host = host;
                host.BonusActions.Add(gob);
                init(gob);
                player.Game.DataEngine.Arena.Gobs.Add(gob);
                result = gob;
            });
            return result;
        }

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public BonusAction()
        {
            _duration = TimeSpan.FromSeconds(30);
        }

        public BonusAction(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Activate()
        {
            base.Activate();
            ResetTimeout();
            if (Game.NetworkMode == Core.NetworkMode.Client)
            {
                foreach (var ba in Host.BonusActions.Where(ba => ba != this && ba.GetType() == GetType()).ToArray()) ba.DieOnClient();
                if (!Host.BonusActions.Contains(this)) Host.BonusActions.Add(this);
            }
        }

        public override void Update()
        {
            base.Update();
            if (EndTime <= Arena.TotalTime || Host == null || Host.Dead) Die();
        }

        public override void Dispose()
        {
            if (Host != null) Host.BonusActions.Remove(this);
            base.Dispose();
        }

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if (mode.HasFlag(SerializationModeFlags.VaryingData))
            {
                writer.Write((bool)_timeoutReset);
                _timeoutReset = false;
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if (mode.HasFlag(SerializationModeFlags.VaryingData))
            {
                if (reader.ReadBoolean()) ResetTimeout();
            }
        }

        public void ResetTimeout()
        {
            // Arena may be null if this method is called by another BonusAction of the same type
            // on the same frame this instance was created. In that case, ResetTimeout will be
            // called again the next frame from Activate().
            if (Arena != null) EndTime = Arena.TotalTime + Duration;
            if (Game.NetworkMode == Core.NetworkMode.Server)
            {
                _timeoutReset = true;
                ForcedNetworkUpdate = true;
            }
        }
    }
}
