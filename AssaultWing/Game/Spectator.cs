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

        private static int g_nextLocalID;

        /// <summary>
        /// Meaningful only for a client's local spectators.
        /// </summary>
        public ServerRegistrationType ServerRegistration { get; set; }

        public AssaultWingCore Game { get; set; }

        /// <summary>
        /// The player's unique identifier.
        /// The identifier may change if a remote game server says so.
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
        /// If <c>true</c> then the spectator lives at a remote game instance.
        /// If <c>false</c> then the spectator lives at this game instance.
        /// </summary>
        public bool IsRemote { get { return ConnectionID >= 0; } }

        /// <summary>
        /// The human-readable name of the spectator.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The controls the player uses in menus and in game.
        /// </summary>
        public PlayerControls Controls { get; private set; }

        /// <summary>
        /// Does the spectator need a viewport on the game window.
        /// </summary>
        public virtual bool NeedsViewport { get { return true; } }

        public Spectator(AssaultWingCore game, PlayerControls controls)
            : this(game, controls, -1)
        {
        }

        public Spectator(AssaultWingCore game, PlayerControls controls, int connectionId)
        {
            Game = game;
            Controls = controls;
            ConnectionID = connectionId;
        }

        /// <param name="onScreen">Location of the viewport on screen.</param>
        public virtual AW2.Graphics.AWViewport CreateViewport(Rectangle onScreen)
        {
            throw new NotImplementedException("Spectator.CreateViewport is to be implemented in subclasses only");
        }

        /// <summary>
        /// Initialises the spectator for a game session, that is, for the first arena.
        /// </summary>
        public virtual void InitializeForGameSession()
        {
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
        }

        public void ResetForClient()
        {
            if (Game.NetworkMode != AW2.Core.NetworkMode.Client) throw new InvalidOperationException("Not a client game instance");
            ServerRegistration = ServerRegistrationType.No;
            LocalID = g_nextLocalID++;
            ID = -1;
        }

        #region INetworkSerializable

        public virtual void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write(Name, 32, true);
            }
        }

        public virtual void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                Name = reader.ReadString(32);
            }
        }

        #endregion INetworkSerializable

        #region IDisposable Members

        public virtual void Dispose()
        {
            Controls.Thrust.Dispose();
            Controls.Left.Dispose();
            Controls.Right.Dispose();
            Controls.Down.Dispose();
            Controls.Fire1.Dispose();
            Controls.Fire2.Dispose();
            Controls.Extra.Dispose();
        }

        #endregion
    }
}
