using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using AW2.Helpers.Serialization;

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
        public TimeSpan LoginTime { get; private set; }

        public string LoginToken { get; private set; }
        public bool IsLoggedIn { get { return !string.IsNullOrEmpty(LoginToken); } }
        public float Rating { get { return 123.5f; } }

        /// <summary>
        /// Received from web page "pilot/id/.../rankings".
        /// </summary>
        public JObject RankingsData { get; set; }

        /// <summary>
        /// Returns true on success.
        /// </summary>
        public bool TrySetLoginData(JObject loginData, TimeSpan totalRealTime)
        {
            LoginTime = totalRealTime;
            if (loginData["token"] == null) return false;
            LoginToken = loginData["token"].ToString();
            return true;
        }

        public void Logout()
        {
            LoginToken = null;
        }

        /// <summary>
        /// To be called when stats server sends a ranking update.
        /// </summary>
        public void OnRankingUpdate(JObject obj)
        {
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
