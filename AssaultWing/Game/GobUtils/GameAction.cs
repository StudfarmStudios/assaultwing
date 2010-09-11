using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Helpers;
using AW2.Net;

namespace AW2.Game.GobUtils
{
    [LimitedSerialization]
    public class GameAction : INetworkSerializable
    {
        private const int GAME_ACTION_TYPES_MAX = 256;
        private static Type[] g_gameActionTypes = new Type[GAME_ACTION_TYPES_MAX];

        public Player Player { get; set; }
        public string BonusText { get; protected set; }
        public string BonusIconName { get; protected set; }
        public Texture2D BonusIcon { get; private set; }
        public TimeSpan BeginTime { get; private set; }
        public TimeSpan EndTime { get; private set; }
        public int TypeID
        {
            get
            {
                return GetType()
                    .GetCustomAttributes(typeof(GameActionTypeAttribute), false)
                    .Cast<GameActionTypeAttribute>()
                    .Single().ID;
            }
        }

        static GameAction()
        {
            var types =
                from type in Assembly.GetExecutingAssembly().GetTypes()
                where typeof(GameAction).IsAssignableFrom(type) && type != typeof(GameAction)
                select type;
            foreach (var type in types)
            {
                var attributes = type.GetCustomAttributes(typeof(GameActionTypeAttribute), false);
                if (attributes.Length != 1) throw new ApplicationException("Each GameAction subclass must have GameActionTypeAttribute");
                var typeAttribute = (GameActionTypeAttribute)attributes[0];
                int id = typeAttribute.ID;
                if (id < 0 || id >= GAME_ACTION_TYPES_MAX) throw new ApplicationException("Invalid GameAction ID + " + id);
                if (g_gameActionTypes[id] != null) throw new ApplicationException(string.Format("GameAction ID " + id + " used by two types, {0} and {1}", g_gameActionTypes[id].Name, type.Name));
                g_gameActionTypes[id] = type;
            }
        }

        public static GameAction CreateGameAction(int typeID)
        {
            if (typeID < 0 || typeID >= GAME_ACTION_TYPES_MAX) throw new ApplicationException("Invalid GameAction ID " + typeID);
            if (g_gameActionTypes[typeID] == null) throw new ArgumentException("GameAction not defined for ID " + typeID);
            return (GameAction)Activator.CreateInstance(g_gameActionTypes[typeID]);
        }

        public void SetDuration(float duration)
        {
            BeginTime = AssaultWingCore.Instance.DataEngine.ArenaTotalTime;
            EndTime = BeginTime + TimeSpan.FromSeconds(duration);
        }

        /// <summary>
        /// Returns true on success and false on failure.
        /// </summary>
        public virtual bool DoAction()
        {
            BonusIcon = AssaultWingCore.Instance.Content.Load<Texture2D>(BonusIconName);
            if (AssaultWingCore.Instance.NetworkMode == NetworkMode.Server)
                Player.MustUpdateToClients = true;
            return true;
        }

        public virtual void RemoveAction()
        {
        }

        public virtual void Update()
        {
        }

        public virtual void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                var duration = EndTime - BeginTime;
                writer.Write(duration);
            }
        }

        public virtual void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                var duration = reader.ReadTimeSpan();
                BeginTime = AssaultWingCore.Instance.DataEngine.ArenaTotalTime;
                EndTime = BeginTime + duration;
            }
        }
    }
}
