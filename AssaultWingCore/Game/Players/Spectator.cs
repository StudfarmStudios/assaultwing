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
using AW2.Net;
using AW2.Stats;

namespace AW2.Game.Players
{
    /// <summary>
    /// Someone who is watching the game through a viewport.
    /// </summary>
    public class Spectator : INetworkSerializable
    {
        /// <summary>
        /// Ranking and score in the Steam leaderboards.
        /// </summary>
        public PilotRanking Ranking { get; set; }

        private enum ConnectionStatusType { Local, Remote, Disconnected };
        public enum ServerRegistrationType { No, Requested, Yes };

        public const int UNINITIALIZED_ID = 0;
        public const int CONNECTION_ID_LOCAL = -1;

        private static int g_nextLocalID;

        private bool _teamAssignmentDeserialized;

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

        /// <summary>This is a securely hashed id meant to detect if a player
        /// joining is the same player as in some earlier connection. In Steam
        /// mode the hashed value is the Steam ID. In raw networking mode this
        /// is a combination of client address and player name. secure hashing
        /// is used to prevent identifying information of a player from leaking
        /// to clients of other players.
        /// </summary>
        public string PilotId { get; set; }

        /// <summary>
        /// Identifier of the connection behind which this spectator lives,
        /// or negative if the spectator lives at the local game instance.
        /// </summary>
        public int ConnectionID { get; private set; }

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

        public Team Team { get { return TeamProxy != null ? TeamProxy.GetValue() : null; } set { TeamProxy = value; } }
        public LazyProxy<int, Team> TeamProxy { get; set; }
        public ArenaStatistics ArenaStatistics { get; private set; }
        public ArenaStatistics PreviousArenaStatistics { get; private set; }
        public ArenaStatistics LatestArenaStatistics { get { return !ArenaStatistics.IsEmpty ? ArenaStatistics : PreviousArenaStatistics; } }

        /// <summary>
        /// In real time.
        /// </summary>
        public TimeSpan LastDisconnectTime { get; private set; }

        private ConnectionStatusType ConnectionStatus { get; set; }

        public Spectator(AssaultWingCore game, string pilotId, int connectionId = CONNECTION_ID_LOCAL)
        {
            Game = game;
            Name = "<uninitialised>";
            PilotId = pilotId;
            ConnectionID = connectionId;
            ConnectionStatus = connectionId == CONNECTION_ID_LOCAL ? ConnectionStatusType.Local : ConnectionStatusType.Remote;
            ArenaStatistics = new ArenaStatistics();
            PreviousArenaStatistics = new ArenaStatistics();
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

        /// <summary>
        /// Returns true if the spectator is a friend, i.e. on the same side as this spectator.
        /// The friend relation is symmetric.
        /// </summary>
        public bool IsFriend(Spectator other)
        {
            return other != null && Team != null && other.Team == Team;
        }

        public void Disconnect()
        {
            if (ConnectionStatus != ConnectionStatusType.Remote) throw new InvalidOperationException("Cannot disconnect a " + ConnectionStatus + " spectator");
            LastDisconnectTime = Game.GameTime.TotalRealTime;
            ConnectionStatus = ConnectionStatusType.Disconnected;
        }

        /// <summary>
        /// Copies connection information from a new instance. Use this method on a game server
        /// when a spectator on a game client reconnects.
        /// </summary>
        public void ReconnectOnServer(Spectator newSpectator)
        {
            ConnectionID = newSpectator.ConnectionID;
            ConnectionStatus = ConnectionStatusType.Remote;
            Ranking = newSpectator.Ranking;
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
            if (_teamAssignmentDeserialized)
            {
                _teamAssignmentDeserialized = false;
                if (Team != null) Team.UpdateAssignment(this);
            }
        }

        /// <summary>
        /// Resets the spectator's internal state for a new arena.
        /// </summary>
        public virtual void ResetForArena()
        {
            PreviousArenaStatistics = ArenaStatistics.Clone();
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
                if (mode.HasFlag(SerializationModeFlags.ConstantDataFromServer))
                {
                    writer.Write((string)PilotId);
                }
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
                ArenaStatistics.Serialize(writer, mode);
            }
        }

        public virtual void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if (mode.HasFlag(SerializationModeFlags.ConstantDataFromServer))
            {
                PilotId = reader.ReadString();
            }
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
                var oldTeam = Team;
                TeamProxy = reader.ReadTeamID(FindTeam);
                if (oldTeam != Team)
                {
                    // Resign from old team now while we still have a direct reference to it.
                    // The new team may not exist yet, so assign to it later in Update().
                    if (oldTeam != null) oldTeam.UpdateAssignment(this);
                    _teamAssignmentDeserialized = true;
                }
            }
            ArenaStatistics.Deserialize(reader, mode, framesAgo);
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
    }
}
