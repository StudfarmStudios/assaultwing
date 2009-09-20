using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AW2.UI;
using AW2.Net;
using Microsoft.Xna.Framework;

namespace AW2.Game
{
    /// <summary>
    /// Someone who is watching the game through a viewport.
    /// </summary>
    public class Spectator : IDisposable, INetworkSerializable
    {
        public class LookAtPoint : AW2.Graphics.ILookAt
        {
            public Vector2 Position { get; set; }
        }

        public LookAtPoint LookAt { get; set; }

        /// <summary>
        /// The player's unique identifier.
        /// </summary>
        /// The identifier may change if a remote game server says so.
        public int Id { get; set; }

        /// <summary>
        /// Identifier of the connection behind which this spectator lives,
        /// or negative if the spectator lives at the local game instance.
        /// </summary>
        public int ConnectionId { get; private set; }

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

        public Spectator(PlayerControls controls)
            : this(controls, -1)
        {
        }

        public Spectator(PlayerControls controls, int connectionId)
        {
            Controls = controls;
            ConnectionId = connectionId;
            LookAt = new LookAtPoint();
        }

        /// <summary>
        /// Creates a viewport for the spectator.
        /// </summary>
        /// <param name="onScreen">Location of the viewport on screen.</param>
        public virtual AW2.Graphics.AWViewport CreateViewport(Rectangle onScreen)
        {
            return new AW2.Graphics.AWViewport(onScreen, LookAt);
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
            float moveSpeed = 10;
            LookAt.Position +=
                Vector2.UnitY * moveSpeed * Controls.thrust.Force
                - Vector2.UnitY * moveSpeed * Controls.down.Force
                + Vector2.UnitX * moveSpeed * Controls.right.Force
                - Vector2.UnitX * moveSpeed * Controls.left.Force;
        }

        /// <summary>
        /// Resets the spectator's internal state for a new arena.
        /// </summary>
        public virtual void Reset()
        {
        }

        #region INetworkSerializable

        /// <summary>
        /// Serialises the spectator to a binary writer.
        /// </summary>
        public virtual void Serialize(Net.NetworkBinaryWriter writer, Net.SerializationModeFlags mode)
        {
        }

        /// <summary>
        /// Deserialises the gob from a binary writer.
        /// </summary>
        public virtual void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, TimeSpan messageAge)
        {
        }

        #endregion INetworkSerializable

        #region IDisposable Members

        public virtual void Dispose()
        {
            Controls.thrust.Release();
            Controls.left.Release();
            Controls.right.Release();
            Controls.down.Release();
            Controls.fire1.Release();
            Controls.fire2.Release();
            Controls.extra.Release();
        }

        #endregion
    }
}
