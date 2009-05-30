//#define PARALLAX_IN_3D // Defining this will make parallaxes be drawn as 3D primitives
//#define VIEWPORT_BLUR // Defining this will make player viewports blurred
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;
using AW2.Game.Particles;
using AW2.Helpers;

namespace AW2.Graphics
{
    /// <summary>
    /// A view on the display that looks into the game world.
    /// </summary>
    /// <c>LoadContent</c> must be called before a viewport is used.
    public abstract class AWViewport
    {
        /// <summary>
        /// Sprite batch to use for drawing sprites.
        /// </summary>
        protected SpriteBatch spriteBatch;

        /// <summary>
        /// Overlay graphics components to draw in this viewport.
        /// </summary>
        protected List<OverlayComponent> overlayComponents;

        /// <summary>
        /// The minimum X and Y coordinates of the game world this viewport shows
        /// at a depth.
        /// </summary>
        /// <param name="z">The depth.</param>
        public abstract Vector2 WorldAreaMin(float z);

        /// <summary>
        /// The maximum X and Y coordinates of the game world this viewport shows
        /// at a depth.
        /// </summary>
        /// <param name="z">The depth.</param>
        public abstract Vector2 WorldAreaMax(float z);

        /// <summary>
        /// Creates a viewport.
        /// </summary>
        public AWViewport()
        {
            overlayComponents = new List<OverlayComponent>();
        }

        /// <summary>
        /// Adds an overlay graphics component to the viewport.
        /// </summary>
        /// <param name="component">The component to add.</param>
        public void AddOverlayComponent(OverlayComponent component)
        {
            overlayComponents.Add(component);
        }

        /// <summary>
        /// Removes all overlay graphics components from the viewport.
        /// </summary>
        public void ClearOverlayComponents()
        {
            overlayComponents.Clear();
        }

        /// <summary>
        /// Checks if a bounding volume might be visible in the viewport.
        /// </summary>
        /// <param name="volume">The bounding volume.</param>
        /// <param name="z">The depth at which the volume resides.</param>
        /// <returns><b>false</b> if the bounding volume definitely cannot be seen in the viewport;
        /// <b>true</b> otherwise.</returns>
        public abstract bool Intersects(BoundingSphere volume, float z);

        /// <summary>
        /// Checks if a bounding volume might be visible in the viewport.
        /// </summary>
        /// <param name="volume">The bounding volume.</param>
        /// <param name="z">The depth at which the volume resides.</param>
        /// <returns><b>false</b> if the bounding volume definitely cannot be seen in the viewport;
        /// <b>true</b> otherwise.</returns>
        public abstract bool Intersects(BoundingBox volume, float z);

        /// <summary>
        /// Draws the viewport's overlay graphics components.
        /// </summary>
        public virtual void Draw()
        {
            foreach (OverlayComponent component in overlayComponents)
                component.Draw(spriteBatch);
        }

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public virtual void LoadContent()
        {
            spriteBatch = new SpriteBatch(AssaultWing.Instance.GraphicsDevice);
            foreach (OverlayComponent component in overlayComponents)
                component.LoadContent();
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        public virtual void UnloadContent()
        {
            foreach (OverlayComponent component in overlayComponents)
                component.UnloadContent();
            spriteBatch.Dispose();
        }
    }

    /// <summary>
    /// A visual separator between viewports.
    /// </summary>
    public struct ViewportSeparator
    {
        /// <summary>
        /// If <b>true</b>, the separator is vertical;
        /// if <b>false</b>, the separator is horizontal.
        /// </summary>
        public bool vertical;

        /// <summary>
        /// The X coordinate of a vertical separator, or
        /// the Y coordinate of a horizontal separator.
        /// </summary>
        public int coordinate;

        /// <summary>
        /// Creates a new viewport separator.
        /// </summary>
        /// <param name="vertical">Is the separator vertical.</param>
        /// <param name="coordinate">The X or Y coordinate of the separator.</param>
        public ViewportSeparator(bool vertical, int coordinate)
        {
            this.vertical = vertical;
            this.coordinate = coordinate;
        }
    }
    
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
        /// The area of the display to draw on.
        /// </summary>
        Viewport viewport;

        /// <summary>
        /// Last point we looked at.
        /// </summary>
        Vector2 lookAt;

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
        public PlayerViewport(Player player, Rectangle onScreen)
        {
            this.player = player;
            viewport = new Viewport();
            viewport.X = onScreen.X;
            viewport.Y = onScreen.Y;
            viewport.Width = onScreen.Width;
            viewport.Height = onScreen.Height;
            viewport.MinDepth = 0f;
            viewport.MaxDepth = 1f;
            lookAt = Vector2.Zero;
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
        private Matrix ViewMatrix
        {
            get
            {
                Gob ship = player.Ship;
                if (ship != null)
                    lookAt = ship.Pos;

                // Shake only if gameplay is on. Otherwise freeze because
                // shake won't be attenuated either.
                if (AssaultWing.Instance.GameState == GameState.Gameplay)
                    shakeSign = -shakeSign;

                float viewShake = shakeSign * player.Shake;
                return Matrix.CreateLookAt(new Vector3(lookAt, 1000), new Vector3(lookAt, 0),
                    new Vector3((float)Math.Cos(MathHelper.PiOver2 + viewShake),
                                (float)Math.Sin(MathHelper.PiOver2 + viewShake),
                                0));
            }
        }

        #endregion PlayerViewport properties

        #region AWViewport implementation

        /// <summary>
        /// The minimum X and Y coordinates of the game world this viewport shows
        /// at a depth.
        /// </summary>
        /// <param name="z">The depth.</param>
        public override Vector2 WorldAreaMin(float z)
        {
            return lookAt - new Vector2(viewport.Width, viewport.Height) / 2 / GetScale(z);
        }

        /// <summary>
        /// The maximum X and Y coordinates of the game world this viewport shows
        /// at a depth.
        /// </summary>
        /// <param name="z">The depth.</param>
        public override Vector2 WorldAreaMax(float z)
        {
            return lookAt + new Vector2(viewport.Width, viewport.Height) / 2 / GetScale(z);
        }

        /// <summary>
        /// Checks if a bounding volume might be visible in the viewport.
        /// </summary>
        /// <param name="volume">The bounding volume.</param>
        /// <param name="z">The depth at which the volume resides.</param>
        /// <returns><b>false</b> if the bounding volume definitely cannot be seen in the viewport;
        /// <b>true</b> otherwise.</returns>
        public override bool Intersects(BoundingSphere volume, float z)
        {
            // We add one unit to the bounding sphere to account for rounding of floating-point
            // world coordinates to integer-valued screen pixels.
            Vector2 min = WorldAreaMin(z);
            Vector2 max = WorldAreaMax(z);
            if (volume.Center.X + volume.Radius + 1f < min.X)
                return false;
            if (volume.Center.Y + volume.Radius + 1f < min.Y)
                return false;
            if (max.X < volume.Center.X - volume.Radius - 1f)
                return false;
            if (max.Y < volume.Center.Y - volume.Radius - 1f)
                return false;
            return true;
        }

        /// <summary>
        /// Checks if a bounding volume might be visible in the viewport.
        /// </summary>
        /// <param name="volume">The bounding volume.</param>
        /// <param name="z">The depth at which the volume resides.</param>
        /// <returns><b>false</b> if the bounding volume definitely cannot be seen in the viewport;
        /// <b>true</b> otherwise.</returns>
        public override bool Intersects(BoundingBox volume, float z)
        {
            // We add one unit to the bounding box to account for rounding of floating-point
            // world coordinates to integer-valued screen pixels.
            Vector2 min = WorldAreaMin(z);
            Vector2 max = WorldAreaMax(z);
            if (volume.Max.X + 1f < min.X)
                return false;
            if (volume.Max.Y + 1f < min.Y)
                return false;
            if (max.X < volume.Min.X - 1f)
                return false;
            if (max.Y < volume.Min.Y - 1f)
                return false;
            return true;
        }

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

        /// <summary>
        /// Draws the viewport's contents.
        /// </summary>
        public override void Draw()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            gfx.Viewport = viewport;
            Matrix view = ViewMatrix;

#if VIEWPORT_BLUR
            gfx.SetRenderTarget(0, rTarg);
            gfx.DepthStencilBuffer = depthBuffer;
            gfx.Clear(ClearOptions.Target, Color.Black, 0, 0);
#else
            gfx.Clear(Color.Black);
#endif


#if PARALLAX_IN_3D
            if (effect == null) // HACK: initialise parallax drawing in 3D, move this to LoadContent and UnloadContent
            {
                effect = new BasicEffect(gfx, null);
                effect.World = Matrix.Identity;
                effect.Projection = Matrix.Identity;
                effect.View = Matrix.Identity;
                effect.TextureEnabled = true;
                effect.LightingEnabled = false;
                effect.FogEnabled = false;
                effect.VertexColorEnabled = false;
                vertexDeclaration = new VertexDeclaration(gfx, VertexPositionTexture.VertexElements);
                vertexData = new VertexPositionTexture[] {
                    new VertexPositionTexture(new Vector3(-1, -1, 1), new Vector2(0, 1)),
                    new VertexPositionTexture(new Vector3(-1, 1, 1), new Vector2(0, 0)),
                    new VertexPositionTexture(new Vector3(1, 1, 1), new Vector2(1, 0)),
                    new VertexPositionTexture(new Vector3(1, -1, 1), new Vector2(1, 1)),
                };
                indexData = new short[] { 0, 1, 2, 3, };
            }
#endif
            foreach (var layer in data.ArenaLayers)
            {
                gfx.Clear(ClearOptions.DepthBuffer, Color.Pink, 1, 0);
                float layerScale = GetScale(layer.Z);
                Matrix projection = Matrix.CreateOrthographic(
                    viewport.Width / layerScale, viewport.Height / layerScale,
                    1f, 11000f);

#if PARALLAX_IN_3D // HACK: Alternative implementation, parallax drawing in 3D by two triangles. Perhaps less time lost in RenderState changes.
                // Modify renderstate for parallax.
                gfx.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
                gfx.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
                gfx.RenderState.AlphaTestEnable = false;
                gfx.RenderState.AlphaBlendEnable = true;
                gfx.RenderState.BlendFunction = BlendFunction.Add;
                gfx.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
                gfx.RenderState.SourceBlend = Blend.SourceAlpha;

                // Layer parallax
                if (layer.ParallaxName != null)
                {
                    // Render looping parallax as two huge triangles.
                    gfx.RenderState.DepthBufferEnable = false;
                    gfx.VertexDeclaration = vertexDeclaration;
                    effect.Texture = data.Textures[layer.ParallaxName];

                    Vector2 texMin = layerScale * new Vector2(
                        lookAt.X / effect.Texture.Width,
                        -lookAt.Y / effect.Texture.Height);
                    Vector2 texMax = texMin + new Vector2(
                        viewport.Width / (float)effect.Texture.Width,
                        -viewport.Height / (float)effect.Texture.Height);
                    vertexData[0].TextureCoordinate = texMin;
                    vertexData[1].TextureCoordinate = new Vector2(texMin.X, texMax.Y);
                    vertexData[2].TextureCoordinate = texMax;
                    vertexData[3].TextureCoordinate = new Vector2(texMax.X, texMin.Y);

                    effect.Begin();
                    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Begin();
                        gfx.DrawUserIndexedPrimitives<VertexPositionTexture>(
                            PrimitiveType.TriangleFan, vertexData, 0, vertexData.Length, indexData, 0, indexData.Length - 2);
                        pass.End();
                    }
                    effect.End();
                }

                // Modify renderstate for 3D graphics.
                gfx.RenderState.DepthBufferEnable = true;
#else // HACK: The old way of drawing parallaxes, with several calls to SpriteBatch.Draw
                // Layer parallax
                if (layer.ParallaxName != "")
                {
                    spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
                    gfx.RenderState.AlphaTestEnable = false;
                    Vector2 pos = WorldAreaMin(0) * -layerScale;
                    pos.Y = -pos.Y;
                    Vector2 fillPos = new Vector2();
                    Texture2D tex = data.Textures[layer.ParallaxName];
                    int mult = (int)Math.Ceiling(pos.X / (float)tex.Width);
                    pos.X = pos.X - mult * tex.Width;
                    mult = (int)Math.Ceiling(pos.Y / (float)tex.Height);
                    pos.Y = pos.Y - mult * tex.Height;

                    int loopX = (int)Math.Ceiling((-pos.X + viewport.Width) / tex.Width);
                    int loopY = (int)Math.Ceiling((-pos.Y + viewport.Height) / tex.Height);
                    fillPos.Y = pos.Y;
                    for (int y = 0; y < loopY; y++)
                    {
                        fillPos.X = pos.X;
                        for (int x = 0; x < loopX; x++)
                        {
                            spriteBatch.Draw(tex, fillPos, Color.White);
                            fillPos.X += tex.Width;
                        }
                        fillPos.Y += tex.Height;
                    }
                    spriteBatch.End();
                }

                // Modify renderstate for 3D graphics.
                gfx.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
                gfx.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
                gfx.RenderState.DepthBufferEnable = true;
                gfx.RenderState.AlphaTestEnable = false;
                gfx.RenderState.AlphaBlendEnable = false;
#endif

                // 3D graphics
                foreach (var gob in layer.Gobs) gob.Draw(view, projection);

                // 2D graphics
                Matrix gameToScreen = view * projection
                    * Matrix.CreateReflection(new Plane(Vector3.UnitY, 0))
                    * Matrix.CreateTranslation(1, 1, 0)
                    * Matrix.CreateScale(new Vector3(viewport.Width, viewport.Height, viewport.MaxDepth - viewport.MinDepth) / 2);
                DrawMode2D? drawMode = null;
                layer.Gobs.ForEachIn2DOrder(gob =>
                {
                    if (!drawMode.HasValue || drawMode.Value.CompareTo(gob.DrawMode2D) != 0)
                    {
                        if (drawMode.HasValue)
                            drawMode.Value.EndDraw(spriteBatch);
                        drawMode = gob.DrawMode2D;
                        drawMode.Value.BeginDraw(spriteBatch);
                    }
                    gob.Draw2D(gameToScreen, spriteBatch, layerScale);
                });
                if (drawMode.HasValue)
                    drawMode.Value.EndDraw(spriteBatch);
            }


#if VIEWPORT_BLUR
            // EFFECTS REDRAW
            bloomatic.Parameters["alpha"].SetValue((float)(0.9));
            bloomatic.Parameters["maxx"].SetValue((float)viewport.Width);
            bloomatic.Parameters["maxy"].SetValue((float)viewport.Height);

            bloomatic.Parameters["hirange"].SetValue(false);

            gfx.SetRenderTarget(0, rTarg2);
            gfx.DepthStencilBuffer = depthBuffer2;

            bloomatic.Begin();
            sprite.Begin(SpriteBlendMode.None, SpriteSortMode.Immediate,
                SaveStateMode.SaveState);

            EffectPass pass = bloomatic.CurrentTechnique.Passes[0];
            pass.Begin();
    
            sprite.Draw(rTarg.GetTexture(), new Rectangle(0, 0, viewport.Width, viewport.Height),
                new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);

            pass.End();
            sprite.End();
            bloomatic.End();

            gfx.SetRenderTarget(0, null);
            gfx.DepthStencilBuffer = defDepthBuffer;

            bloomatic.Begin();
            sprite.Begin(SpriteBlendMode.None, SpriteSortMode.Immediate,
                SaveStateMode.SaveState);

            pass = bloomatic.CurrentTechnique.Passes[1];
            pass.Begin();

            sprite.Draw(rTarg2.GetTexture(), new Rectangle(viewport.X, viewport.Y, viewport.Width, viewport.Height),
                new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);

            pass.End();
            sprite.End();
            bloomatic.End();

            // actual unblurred image
            /*
            sprite.Begin(SpriteBlendMode.Additive);
            sprite.Draw(rTarg.GetTexture(), new Rectangle(viewport.X, viewport.Y, viewport.Width, viewport.Height),
                new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);
            sprite.End();
            */
#endif

            // Overlay components
            gfx.Viewport = viewport;
            base.Draw();
        }

        #endregion AWViewport implementation

        /// <summary>
        /// Returns the visual scaling factor at a depth in game coordinates.
        /// </summary>
        /// <param name="z">The depth, in game coordinates.</param>
        /// <returns>The scaling factor at the depth.</returns>
        float GetScale(float z)
        {
            return 1000 / (1000 - z);
        }
    }
}
