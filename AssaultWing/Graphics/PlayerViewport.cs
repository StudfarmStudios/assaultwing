//#define PARALLAX_IN_3D // Defining this will make parallaxes be drawn as 3D primitives
//#define VIEWPORT_BLUR // Defining this will make player viewports blurred
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;

namespace AW2.Graphics
{
    /// <summary>
    /// A viewport that follows a player.
    /// </summary>
    class PlayerViewport : AWViewport
    {
#if VIEWPORT_BLUR
        protected RenderTarget2D rTarg;
        protected RenderTarget2D rTarg2;
        protected SpriteBatch sprite;
        protected DepthStencilBuffer depthBuffer;
        protected DepthStencilBuffer depthBuffer2;
        protected DepthStencilBuffer defDepthBuffer;
        protected Effect bloomatic;
#endif
        #region PlayerViewport fields

        /// <summary>
        /// The player we are following.
        /// </summary>
        Player player;

        /// <summary>
        /// Last used sign of player's shake angle. Either 1 or -1.
        /// </summary>
        float shakeSign;

#if PARALLAX_IN_3D
        #region Fields for drawing parallax as 3D primitives

        /// <summary>
        /// Effect for drawing parallaxes as 3D primitives.
        /// </summary>
        BasicEffect effect;

        /// <summary>
        /// Vertex declaration for drawing parallaxes as 3D primitives.
        /// </summary>
        VertexDeclaration vertexDeclaration;

        /// <summary>
        /// Vertex data scratch buffer for drawing parallaxes as 3D primitives.
        /// </summary>
        VertexPositionTexture[] vertexData;

        /// <summary>
        /// Index data scratch buffer for drawing parallaxes as 3D primitives.
        /// </summary>
        short[] indexData; // triangle fan

        #endregion Fields for drawing parallax as 3D primitives
#endif
        #endregion PlayerViewport fields

        /// <summary>
        /// Creates a new player viewport.
        /// </summary>
        /// <param name="player">Which player the viewport will follow.</param>
        /// <param name="onScreen">Where on screen is the viewport located.</param>
        /// <param name="lookAt">The point to follow.</param>
        public PlayerViewport(Player player, Rectangle onScreen, ILookAt lookAt)
            : base(onScreen, lookAt)
        {
            this.player = player;
            shakeSign = -1;

            // Create overlay graphics components.
            AddOverlayComponent(new MiniStatusOverlay(player));
            AddOverlayComponent(new ChatBoxOverlay(player));
            AddOverlayComponent(new RadarOverlay(player));
            AddOverlayComponent(new BonusListOverlay(player));
            AddOverlayComponent(new PlayerStatusOverlay(player));
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

        #region AWViewport implementation

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public override void LoadContent()
        {
            base.LoadContent();
#if VIEWPORT_BLUR
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            defDepthBuffer = gfx.DepthStencilBuffer;
            if (viewport.Width > 0)
            {
                rTarg = new RenderTarget2D(gfx, viewport.Width, viewport.Height,
                    0, SurfaceFormat.Color);
                rTarg2 = new RenderTarget2D(gfx, viewport.Width, viewport.Height,
                    0, SurfaceFormat.Color);
                sprite = new SpriteBatch(gfx);
                depthBuffer =
                    new DepthStencilBuffer(
                        gfx,
                        viewport.Width,
                        viewport.Height,
                        gfx.DepthStencilBuffer.Format);
                depthBuffer2 =
                    new DepthStencilBuffer(
                        gfx,
                        viewport.Width,
                        viewport.Height,
                        gfx.DepthStencilBuffer.Format);
            }
            bloomatic = AssaultWing.Instance.Content.Load<Effect>(@"effects/bloom");
#endif
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        public override void UnloadContent()
        {
            base.UnloadContent();
#if VIEWPORT_BLUR
            if (rTarg != null)
                rTarg.Dispose();
            if (sprite != null)
                sprite.Dispose();
            if (depthBuffer != null)
                depthBuffer.Dispose();
            // 'bloomatic' is managed by ContentManager
#endif
        }

        #endregion AWViewport implementation
    }
}
