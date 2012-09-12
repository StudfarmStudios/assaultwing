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

        public override void GetDraw3DBounds(out Vector2 min, out Vector2 max) { min = max = new Vector2(float.NaN); }
        public virtual string BonusText { get { return TypeName; } }
        public abstract CanonicalString BonusIconName { get; }
        public TimeSpan Duration { get { return _duration; } }
        public TimeSpan EndTime { get; private set; }
        public Gob Host { get { return HostProxy != null ? HostProxy.GetValue() : null; } private set { HostProxy = value; } }
        private LazyProxy<int, Gob> HostProxy { get; set; }

        /// <summary>
        /// Creates a <see cref="BonusAction"/> or if an action of the same type and typename
        /// already exists on the player, resets its timer. If an action of the same type but
        /// of a different typename exists, requested action replaces it.
        /// Returns the action that was created or whose timeout was reset,
        /// or returns null if no action was created or reset.
        /// </summary>
        public static T Create<T>(CanonicalString typeName, Gob host, Action<T> init) where T : BonusAction
        {
            var actionType = host.Game.DataEngine.GetTypeTemplate(typeName).GetType();
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
                // The new bonus action may expect the old action to have been disposed first.
                // This is so at least with Weapon2UpgradeBonusAction.
                sameTypeActions.First().Dispose();
            }
            T result = null;
            Gob.CreateGob<T>(host.Game, typeName, gob =>
            {
                gob.ResetPos(Vector2.Zero, Vector2.Zero, Gob.DEFAULT_ROTATION);
                gob.Host = host;
                host.BonusActions.Add(gob);
                init(gob);
                host.Game.DataEngine.Arena.Gobs.Add(gob);
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
            if (Game.NetworkMode == Core.NetworkMode.Client && Host != null)
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
            var baseMode = mode & SerializationModeFlags.ConstantDataFromServer;
            base.Serialize(writer, baseMode);
            if (mode.HasFlag(SerializationModeFlags.ConstantDataFromServer))
            {
                writer.Write((short)Host.ID);
            }
            if (mode.HasFlag(SerializationModeFlags.VaryingDataFromServer))
            {
                writer.Write((bool)_timeoutReset);
                _timeoutReset = false;
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            var baseMode = mode & SerializationModeFlags.ConstantDataFromServer;
            base.Deserialize(reader, baseMode, framesAgo);
            if (mode.HasFlag(SerializationModeFlags.ConstantDataFromServer))
            {
                var hostID = reader.ReadInt16();
                HostProxy = new LazyProxy<int, Gob>(FindGob);
                HostProxy.SetData(hostID);
            }
            if (mode.HasFlag(SerializationModeFlags.VaryingDataFromServer))
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

        public void TimeOut()
        {
            EndTime = Arena.TotalTime;
        }
    }
}
