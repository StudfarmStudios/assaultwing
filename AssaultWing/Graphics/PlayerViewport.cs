using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game;
using AW2.Helpers;

namespace AW2.Graphics
{
    /// <summary>
    /// A viewport that follows a player.
    /// </summary>
    class PlayerViewport : AWViewport
    {
        #region PlayerViewport fields

        /// <summary>
        /// The player we are following.
        /// </summary>
        Player player;

        /// <summary>
        /// Last used sign of player's shake angle. Either 1 or -1.
        /// </summary>
        float shakeSign;

        #endregion PlayerViewport fields

        /// <summary>
        /// Creates a new player viewport.
        /// </summary>
        /// <param name="player">Which player the viewport will follow.</param>
        /// <param name="onScreen">Where on screen is the viewport located.</param>
        /// <param name="lookAt">The point to follow.</param>
        /// <param name="getPostprocessEffectNames">Provider of names of postprocess effects.</param>
        public PlayerViewport(Player player, Rectangle onScreen, ILookAt lookAt,
            Func<IEnumerable<CanonicalString>> getPostprocessEffectNames)
            : base(onScreen, lookAt, getPostprocessEffectNames)
        {
            this.player = player;
            shakeSign = -1;

            // Create overlay graphics components.
            AddOverlayComponent(new MiniStatusOverlay(player));
            AddOverlayComponent(new ChatBoxOverlay(player));
            AddOverlayComponent(new RadarOverlay(player));
            AddOverlayComponent(new BonusListOverlay(player));
            AddOverlayComponent(new PlayerStatusOverlay(player));
            AddOverlayComponent(new ScoreOverlay(player));
        }

        #region PlayerViewport properties

        public Player Player { get { return player; } }

        /// <summary>
        /// The view matrix for drawing 3D content into the viewport.
        /// </summary>
        protected override Matrix ViewMatrix
        {
            get
            {
                // Shake only if gameplay is on. Otherwise freeze because
                // shake won't be attenuated either.
                if (AssaultWing.Instance.GameState == GameState.Gameplay)
                    shakeSign = -shakeSign;

                float viewShake = shakeSign * player.Shake;
                return Matrix.CreateLookAt(new Vector3(LookAt.Position, 1000), new Vector3(LookAt.Position, 0),
                    new Vector3((float)Math.Cos(MathHelper.PiOver2 + viewShake),
                                (float)Math.Sin(MathHelper.PiOver2 + viewShake),
                                0));
            }
        }

        #endregion PlayerViewport properties
    }
}
