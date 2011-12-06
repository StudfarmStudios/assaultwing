using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Helpers.Serialization;
using AW2.UI;

namespace AW2.Game
{
    /// <summary>
    /// Someone who is watching the game through a viewport.
    /// </summary>
    public class Spectator : IDisposable, INetworkSerializable
    {
        public enum ServerRegistrationType { No, Requested, Yes };

        public const int UNINITIALIZED_ID = -1;
        public const int CONNECTION_ID_LOCAL = -1;

        private static int g_nextLocalID;

        /// <summary>
        /// Meaningful only for a client's local spectators.
        /// </summary>
        public ServerRegistrationType ServerRegistration { get; set; }

        public AssaultWingCore Game { get; set; }

        /// <summary>
        /// The player's unique identifier.
        /// The identifier may change if a remote game server says so.
        /// May be <see cref="UNINITIALIZED_ID"/> on a game client.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The player's unique identifier at a game client. Needed so that
        /// a game server can change the <see cref="ID"/> of a player.
        /// This identifier is set by the game client and not changed by the
        /// game server.
        /// </summary>
        public int LocalID { get; set; }

        /// <summary>
        /// Identifier of the connection behind which this spectator lives,
        /// or negative if the spectator lives at the local game instance.
        /// </summary>
        public int ConnectionID { get; private set; }

        /// <summary>
        /// Identification token of the logged-in spectator on the statistics server, or the empty string.
        /// </summary>
        public string LoginToken { get; set; }

        /// <summary>
        /// If <c>true</c> then the spectator lives at a remote game instance.
        /// If <c>false</c> then the spectator lives at this game instance.
        /// </summary>
        public bool IsRemote { get { return ConnectionID >= 0; } }

        /// <summary>
        /// Does the spectator state need to be updated to the clients.
        /// For use by game server only.
        /// </summary>
        public bool MustUpdateToClients { get; set; }

        /// <summary>
        /// The human-readable name of the spectator.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Identification color of the spectator.
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Does the spectator need a viewport on the game window.
        /// </summary>
        public virtual bool NeedsViewport { get { return false; } }

        /// <summary>
        /// All the gobs this spectator controls.
        /// </summary>
        public virtual IEnumerable<Gob> Minions { get { yield break; } }

        public SpectatorArenaStatistics ArenaStatistics { get; private set; }

        public Spectator(AssaultWingCore game, int connectionId = CONNECTION_ID_LOCAL)
        {
            Game = game;
            ConnectionID = connectionId;
            Color = Color.LightGray;
            ArenaStatistics = new SpectatorArenaStatistics();
            LoginToken = "";
        }

        /// <param name="onScreen">Location of the viewport on screen.</param>
        public virtual AW2.Graphics.AWViewport CreateViewport(Rectangle onScreen)
        {
            throw new NotImplementedException("Spectator.CreateViewport is to be implemented in subclasses only");
        }

        /// <summary>
        /// Updates the spectator.
        /// </summary>
        public virtual void Update()
        {
        }

        /// <summary>
        /// Resets the spectator's internal state for a new arena.
        /// </summary>
        public virtual void ResetForArena()
        {
            ArenaStatistics = new SpectatorArenaStatistics(Game.DataEngine.GameplayMode);
        }

        public void ResetForClient()
        {
            if (Game.NetworkMode != AW2.Core.NetworkMode.Client) throw new InvalidOperationException("Not a client game instance");
            ServerRegistration = ServerRegistrationType.No;
            LocalID = g_nextLocalID++;
            ID = UNINITIALIZED_ID;
        }

        public virtual void Dispose()
        {
        }

        public virtual void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {

                if (mode.HasFlag(SerializationModeFlags.ConstantDataFromServer) ||
                    mode.HasFlag(SerializationModeFlags.ConstantDataFromClient))
                {
                    writer.Write((string)Name);
                }
                if (mode.HasFlag(SerializationModeFlags.ConstantDataFromClient))
                {
                    writer.Write((string)LoginToken);
                }
                ArenaStatistics.Serialize(writer, mode);
            }
        }

        public virtual void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if (mode.HasFlag(SerializationModeFlags.ConstantDataFromServer) ||
                mode.HasFlag(SerializationModeFlags.ConstantDataFromClient))
            {
                Name = reader.ReadString();
            }
            if (mode.HasFlag(SerializationModeFlags.ConstantDataFromClient))
            {
                LoginToken = reader.ReadString();
            }
            ArenaStatistics.Deserialize(reader, mode, framesAgo);
        }
    }
}
