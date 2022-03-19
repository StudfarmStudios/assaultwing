using System;
using Newtonsoft.Json.Linq;
using AW2.Helpers.Serialization;
using AW2.Game.Players;

namespace AW2.Net
{
    /// <summary>
    /// Wrapper around JSON objects received from the stats server for a spectator.
    /// </summary>
    public class SpectatorStats : INetworkSerializable
    {
        /// <summary>
        /// Time of login data reception in real time.
        /// </summary>
        public TimeSpan LoginTime { get; set; }

        public string PilotId { get; private set; }
        public string Username { get; private set; }
        public string LoginToken { get; private set; }
        public bool IsLoggedIn { get { return LoginToken != ""; } }
        public float Rating { get; private set; }
        public int RatingRank { get; private set; }

        /// <summary>
        /// Received from web page "pilot/id/.../rankings".
        /// </summary>
        public JObject RankingsData { get; set; }

        public Spectator Spectator { get; private set; }

        public SpectatorStats(Spectator spectator)
        {
            Spectator = spectator;
            LoginToken = "";
        }

        public void Update(JObject obj)
        {
            try
            {
                if (obj["_id"] != null) PilotId = (string)obj["_id"];
                if (obj["username"] != null) Spectator.Name = (string)obj["username"];
                if (obj["token"] != null) LoginToken = (string)obj["token"];
                if (obj["rating"] != null) Rating = (float)obj["rating"];
                if (obj["ratingRank"] != null) RatingRank = (int)obj["ratingRank"];
            }
            catch (ArgumentException) { } // invalid cast of a JToken value
        }

        public void Logout()
        {
            LoginToken = "";
        }

        public virtual void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
                checked
                {
                    if (mode.HasFlag(SerializationModeFlags.ConstantDataFromClient))
                    {
                        writer.Write((string)LoginToken);
                    }
                }
        }

        public virtual void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if (mode.HasFlag(SerializationModeFlags.ConstantDataFromClient))
            {
                LoginToken = reader.ReadString();
            }
        }

    }
}
