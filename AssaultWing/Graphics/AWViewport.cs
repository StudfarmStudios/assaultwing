//#define PARALLAX_IN_3D // Defining this will make parallaxes be drawn as 3D primitives
#if !PARALLAX_IN_3D
  #define PARALLAX_WITH_SPRITE_BATCH
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;
using AW2.Helpers;

namespace AW2.Graphics
{
    /// <summary>
    /// A point in game world that a viewport is viewing.
    /// </summary>
    public interface ILookAt
    {
        Vector2 Position { get; }
    }

    /// <summary>
    /// A view on the display that looks into the game world.
    /// </summary>
    /// <c>LoadContent</c> must be called before a viewport is used.
    public class AWViewport
    {
        #region Fields that are used only when PARALLAX_IN_3D is #defined

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

        #endregion Fields that are used only when PARALLAX_IN_3D is #defined

        /// <summary>
        /// Sprite batch to use for drawing sprites.
        /// </summary>
        protected SpriteBatch spriteBatch;

        /// <summary>
        /// Overlay graphics components to draw in this viewport.
        /// </summary>
        protected List<OverlayComponent> overlayComponents;

        TexturePostprocessor _postprocessor;
        Func<IEnumerable<CanonicalString>> _getPostprocessEffectNames;

        /// <summary>
        /// Ratio of screen pixels to game world meters. Default value is 1.
        /// </summary>
        public float ZoomRatio { get; set; }

        #region Properties

        /// <summary>
        /// The area of the display to draw on.
        /// </summary>
        protected Viewport Viewport { get; set; }

        /// <summary>
        /// Center of the view in game world coordinates.
        /// </summary>
        protected ILookAt LookAt { get; set; }

        /// <summary>
        /// The area of the viewport on the render target surface.
        /// </summary>
        public Rectangle OnScreen { get { return new Rectangle(Viewport.Y, Viewport.Y, Viewport.Width, Viewport.Height); } }

        #endregion

        /// <summary>
        /// The minimum X and Y coordinates of the game world this viewport shows
        /// at a depth.
        /// </summary>
        /// <param name="z">The depth.</param>
        public Vector2 WorldAreaMin(float z)
        {
            return LookAt.Position - new Vector2(Viewport.Width, Viewport.Height) / (2 * ZoomRatio * GetScale(z));
        }

        /// <summary>
        /// The maximum X and Y coordinates of the game world this viewport shows
        /// at a depth.
        /// </summary>
        /// <param name="z">The depth.</param>
        public Vector2 WorldAreaMax(float z)
        {
            return LookAt.Position + new Vector2(Viewport.Width, Viewport.Height) / (2 * ZoomRatio * GetScale(z));
        }

        /// <summary>
        /// The matrix for projecting world coordinates to view coordinates.
        /// </summary>
        protected Matrix GetProjectionMatrix(float z)
        {
            float layerScale = GetScale(z);
            return Matrix.CreateOrthographic(
                Viewport.Width / (ZoomRatio * layerScale),
                Viewport.Height / (ZoomRatio * layerScale),
                1f, 11000f);
        }

        /// <summary>
        /// The view matrix for drawing 3D content into the viewport.
        /// </summary>
        protected virtual Matrix ViewMatrix
        {
            get
            {
                return Matrix.CreateLookAt(new Vector3(LookAt.Position, 1000), new Vector3(LookAt.Position, 0), Vector3.Up);
            }
        }

        /// <summary>
        /// Creates a viewport.
        /// </summary>
        /// <param name="onScreen">Where on screen is the viewport located.</param>
        /// <param name="lookAt">The point to follow.</param>
        public AWViewport(Rectangle onScreen, ILookAt lookAt, Func<IEnumerable<CanonicalString>> getPostprocessEffectNames)
        {
            overlayComponents = new List<OverlayComponent>();
            Viewport = new Viewport
            {
                X = onScreen.X,
                Y = onScreen.Y,
                Width = onScreen.Width,
                Height = onScreen.Height,
                MinDepth = 0f,
                MaxDepth = 1f
            };
            LookAt = lookAt;
            _getPostprocessEffectNames = getPostprocessEffectNames;
            ZoomRatio = 1;
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
        public bool Intersects(BoundingSphere volume, float z)
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
        public bool Intersects(BoundingBox volume, float z)
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
        /// Converts a 2D point in the viewport into a 2D point in an arena layer in the game world.
        /// </summary>
        /// <param name="pointInViewport">Point in viewport; origin is top left corner,
        /// positive X is right and positive Y is down.</param>
        /// <param name="z">The Z coordinate of the arena layer.</param>
        public Vector2 ToPos(Vector2 pointInViewport, float z)
        {
            // Note: Z coordinate in view space is irrelevant because we have
            // an orthogonal projection from game world space to view space.
            var viewPos = new Vector3(pointInViewport, 0f);
            var view = ViewMatrix;
            var projection = GetProjectionMatrix(z);
            var worldPos = Viewport.Unproject(viewPos, projection, view, Matrix.Identity);
            return new Vector2(worldPos.X, worldPos.Y);
        }

        /// <summary>
        /// Converts a 2D point in the viewport into a ray in one arena layer in the game world.
        /// </summary>
        /// <param name="pointInViewport">Point in viewport; origin is top left corner,
        /// positive X is right and positive Y is down.</param>
        /// <param name="z">The Z coordinate of the arena layer.</param>
        public Ray ToRay(Vector2 pointInViewport, float z)
        {
            var nearView = new Vector3(pointInViewport, 0f);
            var farView = new Vector3(pointInViewport, 1f);
            var view = ViewMatrix;
            var projection = GetProjectionMatrix(z);
            var nearWorld = Viewport.Unproject(nearView, projection, view, Matrix.Identity);
            var farWorld = Viewport.Unproject(farView, projection, view, Matrix.Identity);
            var direction = farWorld - nearWorld;
            direction.Normalize();
            return new Ray(nearWorld, direction);
        }

        /// <summary>
        /// Draws the viewport's contents.
        /// </summary>
        public void Draw()
        {
            var gfx = AssaultWing.Instance.GraphicsDevice;
            gfx.Viewport = Viewport;
            gfx.Clear(Color.Black);
            Draw_InitializeParallaxIn3D();
            _postprocessor.ProcessToScreen(RenderGameWorld);
            DrawOverlayComponents();
        }

        private void DrawOverlayComponents()
        {
            var gfx = AssaultWing.Instance.GraphicsDevice;
            gfx.Viewport = Viewport;
            foreach (OverlayComponent component in overlayComponents)
                component.Draw(spriteBatch);
        }

        protected virtual void RenderGameWorld()
        {
            var gfx = AssaultWing.Instance.GraphicsDevice;
            var view = ViewMatrix;
            gfx.Clear(Color.Black);
            foreach (var layer in AssaultWing.Instance.DataEngine.Arena.Layers)
            {
                gfx.Clear(ClearOptions.DepthBuffer, Color.Pink, 1, 0);
                float layerScale = GetScale(layer.Z);
                var projection = GetProjectionMatrix(layer.Z);

                // Note: These methods have ConditionalAttribute.
                // Only one of them will be executed at runtime.
                Draw_DrawParallaxIn3D(layer);
                Draw_DrawParallaxWithSpriteBatch(layer);

                // 3D graphics
                foreach (var gob in layer.Gobs)
                {
                    var bounds = gob.DrawBounds;
                    if (bounds.Radius > 0 && Intersects(bounds, layer.Z))
                    {
                        AssaultWing.Instance.GobsDrawnPerFrameAvgPerSecondCounter.Increment();
                        if (gob.IsVisible) gob.Draw(view, projection);
                    }
                }

                // 2D graphics
                Matrix gameToScreen = view * projection
                    * Matrix.CreateReflection(new Plane(Vector3.UnitY, 0))
                    * Matrix.CreateTranslation(1, 1, 0)
                    * Matrix.CreateScale(new Vector3(Viewport.Width, Viewport.Height, Viewport.MaxDepth - Viewport.MinDepth) / 2);
                DrawMode2D? drawMode = null;
                layer.Gobs.ForEachIn2DOrder(gob =>
                {
                    if (!gob.IsVisible) return;
                    if (!drawMode.HasValue || drawMode.Value.CompareTo(gob.DrawMode2D) != 0)
                    {
                        if (drawMode.HasValue)
                            drawMode.Value.EndDraw(spriteBatch);
                        drawMode = gob.DrawMode2D;
                        drawMode.Value.BeginDraw(spriteBatch);
                    }
                    gob.Draw2D(gameToScreen, spriteBatch, layerScale * ZoomRatio);
                });
                if (drawMode.HasValue)
                    drawMode.Value.EndDraw(spriteBatch);
            }
        }

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public virtual void LoadContent()
        {
            spriteBatch = new SpriteBatch(AssaultWing.Instance.GraphicsDevice);
            Action<ICollection<Effect>> effectContainerUpdater = container =>
            {
                container.Clear();
                foreach (var name in _getPostprocessEffectNames())
                    container.Add(AssaultWing.Instance.Content.Load<Effect>(name));
            };
            _postprocessor = new TexturePostprocessor(AssaultWing.Instance.GraphicsDevice, effectContainerUpdater);
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
            _postprocessor.Dispose();
            spriteBatch.Dispose();
        }

        /// <summary>
        /// Returns the visual scaling factor at a depth in game coordinates.
        /// </summary>
        /// <param name="z">The depth, in game coordinates.</param>
        /// <returns>The scaling factor at the depth.</returns>
        float GetScale(float z)
        {
            return 1000 / (1000 - z);
        }

        #region Methods that are used only conditionally

        /// <summary>
        /// This method is a temporary hack. It initialises parallax drawing
        /// in 3D. If a final decision is made to do parallax drawing in 3D,
        /// the contents of this method must be moved to appropriate places in
        /// LoadContent and UnloadContent. If a final decision is made to do parallax
        /// drawing in 2D with SpriteBatch, this method should be removed.
        /// </summary>
        [Conditional("PARALLAX_IN_3D")]
        private void Draw_InitializeParallaxIn3D()
        {
            if (effect != null) return;
            var gfx = AssaultWing.Instance.GraphicsDevice;
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
                new VertexPositionTexture(new Vector3(-1, -1, 1), Vector2.UnitY),
                new VertexPositionTexture(new Vector3(-1, 1, 1), Vector2.Zero),
                new VertexPositionTexture(new Vector3(1, -1, 1), Vector2.One),
                new VertexPositionTexture(new Vector3(1, 1, 1), Vector2.UnitX)
            };
        }

        /// <summary>
        /// HACK: Alternative implementation, parallax drawing in 3D by two triangles.
        /// Perhaps less time lost in RenderState changes.
        /// </summary>
        [Conditional("PARALLAX_IN_3D")]
        private void Draw_DrawParallaxIn3D(AW2.Game.ArenaLayer layer)
        {
            var gfx = AssaultWing.Instance.GraphicsDevice;
            // Modify renderstate for parallax.
            gfx.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            gfx.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            gfx.RenderState.AlphaTestEnable = false;
            gfx.RenderState.AlphaBlendEnable = true;
            gfx.RenderState.BlendFunction = BlendFunction.Add;
            gfx.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            gfx.RenderState.SourceBlend = Blend.SourceAlpha;

            // Layer parallax
            if (layer.ParallaxName != "")
            {
                // Render looping parallax as two huge triangles.
                gfx.RenderState.DepthBufferEnable = false;
                gfx.VertexDeclaration = vertexDeclaration;
                effect.Texture = AssaultWing.Instance.Content.Load<Texture2D>(layer.ParallaxName);
                var texCenter = GetScale(layer.Z) * new Vector2(
                    LookAt.Position.X / effect.Texture.Width,
                    -LookAt.Position.Y / effect.Texture.Height);
                var texCornerOffset = new Vector2(
                    Viewport.Width / (2f * effect.Texture.Width),
                    -Viewport.Height / (2f * effect.Texture.Height)) / ZoomRatio;
                vertexData[0].TextureCoordinate = texCenter - texCornerOffset;
                vertexData[1].TextureCoordinate = texCenter + new Vector2(-texCornerOffset.X, texCornerOffset.Y);
                vertexData[2].TextureCoordinate = texCenter + new Vector2(texCornerOffset.X, -texCornerOffset.Y);
                vertexData[3].TextureCoordinate = texCenter + texCornerOffset;

                effect.Begin();
                effect.CurrentTechnique.Passes[0].Begin();
                gfx.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleStrip, vertexData, 0, 2);
                effect.CurrentTechnique.Passes[0].End();
                effect.End();
            }

            // Modify renderstate for 3D graphics.
            gfx.RenderState.DepthBufferEnable = true;
        }

        /// <summary>
        /// HACK: The old way of drawing parallaxes, with several calls to SpriteBatch.Draw.
        /// </summary>
        [Conditional("PARALLAX_WITH_SPRITE_BATCH")]
        private void Draw_DrawParallaxWithSpriteBatch(ArenaLayer layer)
        {
            var gfx = AssaultWing.Instance.GraphicsDevice;
            if (layer.ParallaxName != "")
            {
                spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
                gfx.RenderState.AlphaTestEnable = false;
                var tex = AssaultWing.Instance.Content.Load<Texture2D>(layer.ParallaxName);
                float texCenterX = (GetScale(layer.Z) * LookAt.Position.X).Modulo(tex.Width);
                float texCenterY = (GetScale(layer.Z) * -LookAt.Position.Y).Modulo(tex.Height);
                float screenStartX = (Viewport.Width / 2f - texCenterX * ZoomRatio).Modulo(tex.Width * ZoomRatio) - tex.Width * ZoomRatio;
                float screenStartY = (Viewport.Height / 2f - texCenterY * ZoomRatio).Modulo(tex.Height * ZoomRatio) - tex.Height * ZoomRatio;
                for (float posX = screenStartX; posX <= Viewport.Width; posX += tex.Width * ZoomRatio)
                    for (float posY = screenStartY; posY <= Viewport.Height; posY += tex.Height * ZoomRatio)
                        spriteBatch.Draw(tex, new Vector2(posX, posY), null, Color.White, 0, Vector2.Zero, ZoomRatio, SpriteEffects.None, 1);
/*
                Vector2 pos = WorldAreaMin(0) * -GetScale(layer.Z);
                pos.Y = -pos.Y;
                Vector2 fillPos = new Vector2();
                pos.X -= tex.Width * (float)Math.Ceiling(pos.X / (tex.Width * zoomRatio));
                pos.Y -= tex.Height * (float)Math.Ceiling(pos.Y / (tex.Height * zoomRatio));
                int loopX = (int)Math.Ceiling((-pos.X + Viewport.Width) / (tex.Width * zoomRatio));
                int loopY = (int)Math.Ceiling((-pos.Y + Viewport.Height) / (tex.Height * zoomRatio));

                fillPos.Y = pos.Y;
                for (int y = 0; y < loopY; y++)
                {
                    fillPos.X = pos.X;
                    for (int x = 0; x < loopX; x++)
                    {
                        spriteBatch.Draw(tex, fillPos, null, Color.White, 0, Vector2.Zero, zoomRatio, SpriteEffects.None, 1);
                        fillPos.X += tex.Width * zoomRatio;
                    }
                    fillPos.Y += tex.Height * zoomRatio;
                }
*/
                spriteBatch.End();
            }

            // Modify renderstate for 3D graphics.
            gfx.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            gfx.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            gfx.RenderState.DepthBufferEnable = true;
            gfx.RenderState.AlphaTestEnable = false;
            gfx.RenderState.AlphaBlendEnable = false;
        }

        #endregion Methods that are used only conditionally
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
}
