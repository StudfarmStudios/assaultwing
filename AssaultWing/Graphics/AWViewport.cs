using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

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
        /// <summary>
        /// Sprite batch to use for drawing sprites.
        /// </summary>
        protected SpriteBatch spriteBatch;

        /// <summary>
        /// Overlay graphics components to draw in this viewport.
        /// </summary>
        protected List<OverlayComponent> overlayComponents;

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

        /// <summary>
        /// The minimum X and Y coordinates of the game world this viewport shows
        /// at a depth.
        /// </summary>
        /// <param name="z">The depth.</param>
        public Vector2 WorldAreaMin(float z)
        {
            return LookAt.Position - new Vector2(Viewport.Width, Viewport.Height) / 2 / GetScale(z);
        }

        /// <summary>
        /// The maximum X and Y coordinates of the game world this viewport shows
        /// at a depth.
        /// </summary>
        /// <param name="z">The depth.</param>
        public Vector2 WorldAreaMax(float z)
        {
            return LookAt.Position + new Vector2(Viewport.Width, Viewport.Height) / 2 / GetScale(z);
        }

        /// <summary>
        /// The matrix for projecting world coordinates to view coordinates.
        /// </summary>
        protected Matrix GetProjectionMatrix(float z)
        {
            float layerScale = GetScale(z);
            return Matrix.CreateOrthographic(
                Viewport.Width / layerScale, Viewport.Height / layerScale,
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
        public AWViewport(Rectangle onScreen, ILookAt lookAt)
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
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            gfx.Viewport = Viewport;
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
            foreach (var layer in AssaultWing.Instance.DataEngine.Arena.Layers)
            {
                gfx.Clear(ClearOptions.DepthBuffer, Color.Pink, 1, 0);
                float layerScale = GetScale(layer.Z);
                var projection = GetProjectionMatrix(layer.Z);

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
                    Texture2D tex = AssaultWing.Instance.Content.Load<Texture2D>(layer.ParallaxName);
                    int mult = (int)Math.Ceiling(pos.X / (float)tex.Width);
                    pos.X = pos.X - mult * tex.Width;
                    mult = (int)Math.Ceiling(pos.Y / (float)tex.Height);
                    pos.Y = pos.Y - mult * tex.Height;

                    int loopX = (int)Math.Ceiling((-pos.X + Viewport.Width) / tex.Width);
                    int loopY = (int)Math.Ceiling((-pos.Y + Viewport.Height) / tex.Height);
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
            gfx.Viewport = Viewport;
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
