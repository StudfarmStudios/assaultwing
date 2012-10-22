using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game.Logic;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.UI;

namespace AW2.Game.Players
{
    /// <summary>
    /// Someone who is watching the game through a viewport.
    /// </summary>
    public class Spectator : INetworkSerializable
    {
        public enum ConnectionStatusType { Local, Remote, Disconnected };
        public enum ServerRegistrationType { No, Requested, Yes };

        public const int UNINITIALIZED_ID = 0;
        public const int CONNECTION_ID_LOCAL = -1;

        private static int g_nextLocalID;

        public static Func<Spectator, INetworkSerializable> CreateStatsData;

        /// <summary>
        /// Meaningful only on a game client.
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
        /// Data received from the statistics server.
        /// </summary>
        public INetworkSerializable StatsData { get; set; }

        /// <summary>
        /// The last known IP address of the connection of the spectator. For local players it's 127.0.0.1.
        /// </summary>
        public IPAddress IPAddress { get; private set; }

        /// <summary>
        /// Is the spectator connected from a remote game instance.
        /// </summary>
        public bool IsRemote { get { return ConnectionStatus == ConnectionStatusType.Remote; } }

        /// <summary>
        /// Is the spectator from a remote game instance but currently disconnected.
        /// </summary>
        public bool IsDisconnected { get { return ConnectionStatus == ConnectionStatusType.Disconnected; } }

        /// <summary>
        /// Does the spectator live on the local game instance.
        /// </summary>
        public bool IsLocal { get { return ConnectionStatus == ConnectionStatusType.Local; } }

        /// <summary>
        /// For use by the game server only.
        /// </summary>
        public bool IsClientUpdateRequested { get; set; }

        /// <summary>
        /// The human-readable name of the spectator.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Identification color of the spectator.
        /// </summary>
        public Color Color { get { return Team == null ? Color.LightGray : Team.Color; } }

        /// <summary>
        /// Does the spectator need a viewport on the game window.
        /// </summary>
        public virtual bool NeedsViewport { get { return false; } }

        /// <summary>
        /// All the gobs this spectator controls.
        /// </summary>
        public virtual IEnumerable<Gob> Minions { get { yield break; } }

        public Team Team { get{ return TeamProxy != null ? TeamProxy.GetValue() : null; } set { TeamProxy = value; } }
        public LazyProxy<int, Team> TeamProxy { get; set; }
        public ArenaStatistics ArenaStatistics { get; private set; }

        /// <summary>
        /// In real time.
        /// </summary>
        public TimeSpan LastDisconnectTime { get; private set; }

        private ConnectionStatusType ConnectionStatus { get; set; }

        public Spectator(AssaultWingCore game, int connectionId = CONNECTION_ID_LOCAL, IPAddress ipAddress = null)
        {
            Game = game;
            ConnectionID = connectionId;
            ConnectionStatus = connectionId == CONNECTION_ID_LOCAL ? ConnectionStatusType.Local : ConnectionStatusType.Remote;
            IPAddress = ipAddress ?? IPAddress.Loopback;
            ArenaStatistics = new ArenaStatistics();
            ArenaStatistics.Updated += StatisticsUpdatedHandler;
            StatsData = CreateStatsData(this);
        }

        /// <param name="onScreen">Location of the viewport on screen.</param>
        public virtual AW2.Graphics.AWViewport CreateViewport(Rectangle onScreen)
        {
            throw new NotImplementedException("Spectator.CreateViewport is to be implemented in subclasses only");
        }

        /// <summary>
        /// Assigns the spectator to a team. The spectator will resign any previous team.
        /// </summary>
        public void AssignTeam(Team team)
        {
            var oldTeam = Team;
            Team = team;
            if (oldTeam != null) oldTeam.UpdateAssignment(this);
            if (Team != null) Team.UpdateAssignment(this);
        }

        public void Disconnect()
        {
            if (ConnectionStatus != ConnectionStatusType.Remote) throw new InvalidOperationException("Cannot disconnect a " + ConnectionStatus + " spectator");
            LastDisconnectTime = Game.GameTime.TotalRealTime;
            ConnectionStatus = ConnectionStatusType.Disconnected;
            IsClientUpdateRequested = true;
        }

        /// <summary>
        /// Copies connection information from a new instance. Use this method on a game server
        /// when a spectator on a game client reconnects.
        /// </summary>
        public void ReconnectOnServer(Spectator newSpectator)
        {
            ConnectionID = newSpectator.ConnectionID;
            ConnectionStatus = ConnectionStatusType.Remote;
            StatsData = newSpectator.StatsData;
            IsClientUpdateRequested = true;
        }

        /// <summary>
        /// Steals minion ownership from the old spectator. Use this method on a game client
        /// when the spectator has reconnected.
        /// </summary>
        public virtual void ReconnectOnClient(Spectator oldSpectator)
        {
        }

        /// <summary>
        /// Updates the spectator.
        /// </summary>
        public virtual void Update()
        {
            // Refresh team assignment in case TeamProxy has gained more data.
            if (Game.NetworkMode == NetworkMode.Client) AssignTeam(Team);
        }

        /// <summary>
        /// Resets the spectator's internal state for a new arena.
        /// </summary>
        public virtual void ResetForArena()
        {
            ArenaStatistics.Reset(Game.DataEngine.GameplayMode);
        }

        public void ResetForClient()
        {
            if (Game.NetworkMode != AW2.Core.NetworkMode.Client) throw new InvalidOperationException("Not a client game instance");
            ServerRegistration = ServerRegistrationType.No;
            LocalID = g_nextLocalID++;
            ID = UNINITIALIZED_ID;
        }

        public virtual void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            checked
            {
                if (mode.HasFlag(SerializationModeFlags.ConstantDataFromServer) ||
                    mode.HasFlag(SerializationModeFlags.ConstantDataFromClient))
                {
                    writer.Write((string)Name);
                }
                if (mode.HasFlag(SerializationModeFlags.VaryingDataFromServer))
                {
                    writer.Write((bool)IsDisconnected);
                    writer.WriteID(Team);
                }
                StatsData.Serialize(writer, mode);
            }
        }

        public virtual void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if (mode.HasFlag(SerializationModeFlags.ConstantDataFromServer) ||
                mode.HasFlag(SerializationModeFlags.ConstantDataFromClient))
            {
                Name = reader.ReadString();
            }
            if (mode.HasFlag(SerializationModeFlags.VaryingDataFromServer))
            {
                var isDisconnected = reader.ReadBoolean();
                if (IsRemote && isDisconnected) ConnectionStatus = ConnectionStatusType.Disconnected;
                if (IsDisconnected && !isDisconnected) ConnectionStatus = ConnectionStatusType.Remote;
                TeamProxy = reader.ReadTeamID(FindTeam);
                // Note: The team is refreshed in Spectator.Update()
            }
            StatsData.Deserialize(reader, mode, framesAgo);
        }

        public override string ToString()
        {
            return string.Format("{0} ({1}, {2})", Name, ID, ConnectionStatus);
        }

        private Team FindTeam(int id)
        {
            return id == Team.UNINITIALIZED_ID
                ? null
                : Game.DataEngine.Teams.FirstOrDefault(t => t.ID == id);
        }

        private void StatisticsUpdatedHandler()
        {
            if (Game == null || Game.NetworkMode != NetworkMode.Server) return;
            Game.DataEngine.EnqueueArenaStateToClients();
        }
    }
}
