using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Core;
using AW2.Helpers.Serialization;

namespace AW2.Net.ConnectionUtils
{
    public class GameClientStatus : INetworkSerializable
    {
        private string _currentArenaName;

        /// <summary>
        /// Name of the arena the instance at the end of the connection is currently playing,
        /// or the empty string if no arena is being played.
        /// </summary>
        public string CurrentArenaName { get { return _currentArenaName ?? ""; } set { _currentArenaName = value; } }

        public bool IsPlayingArena { get { return CurrentArenaName != ""; } }

        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            writer.Write((string)CurrentArenaName);
        }

        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            CurrentArenaName = reader.ReadString();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GameClientStatus)) return false;
            var other = (GameClientStatus)obj;
            return _currentArenaName == other._currentArenaName;
        }

        public override int GetHashCode()
        {
            return _currentArenaName.GetHashCode();
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(CurrentArenaName))
                return "Not playing any arena";
            else
                return "Playing " + CurrentArenaName;
        }
    }
}
