#define PARALLAX_IN_3D // Defining this will make parallaxes be drawn as 3D primitives
#if !PARALLAX_IN_3D
#define PARALLAX_WITH_SPRITE_BATCH
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game.Arenas;
using AW2.Helpers;
using AW2.Game;

namespace AW2.Graphics
{
    /// <summary>
    /// A view on the display that looks into the game world.
    /// </summary>
    /// <c>LoadContent</c> must be called before a viewport is used.
    public abstract class AWViewport : IDisposable
    {
        #region Fields that are used only when PARALLAX_IN_3D is #defined

        /// <summary>
        /// Effect for drawing parallaxes as 3D primitives.
        /// </summary>
        private BasicEffect _effect;

        /// <summary>
        /// Vertex data scratch buffer for drawing parallaxes as 3D primitives.
        /// </summary>
        private VertexPositionTexture[] _vertexData;

        #endregion Fields that are used only when PARALLAX_IN_3D is #defined

        /// <summary>
        /// Sprite batch to use for drawing sprites.
        /// </summary>
        protected SpriteBatch _spriteBatch;

        /// <summary>
        /// Overlay graphics components to draw in this viewport.
        /// </summary>
        protected List<OverlayComponent> _overlayComponents;

        private TexturePostprocessor _postprocessor;
        private Func<IEnumerable<CanonicalString>> _getPostprocessEffectNames;

        /// <summary>
        /// Ratio of screen pixels to game world meters. Default value is 1.
        /// </summary>
        public float ZoomRatio { get; set; }

        /// <summary>
        /// The area of the display to draw on.
        /// </summary>
        protected Viewport Viewport { get; set; }

        /// <summary>
        /// The area of the viewport on the render target surface.
        /// </summary>
        public Rectangle OnScreen { get { return new Rectangle(Viewport.Y, Viewport.Y, Viewport.Width, Viewport.Height); } }

        public event Func<ArenaLayer, bool> LayerDrawing;
        public event Action<Gob> GobDrawn;

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
                return Matrix.CreateLookAt(new Vector3(GetLookAtPos(), 1000), new Vector3(GetLookAtPos(), 0), Vector3.Up);
            }
        }

        /// <param name="onScreen">Where on screen is the viewport located.</param>
        protected AWViewport(Rectangle onScreen, Func<IEnumerable<CanonicalString>> getPostprocessEffectNames)
        {
            _overlayComponents = new List<OverlayComponent>();
            Viewport = new Viewport
            {
                X = onScreen.X,
                Y = onScreen.Y,
                Width = onScreen.Width,
                Height = onScreen.Height,
                MinDepth = 0f,
                MaxDepth = 1f
            };
            _getPostprocessEffectNames = getPostprocessEffectNames;
            ZoomRatio = 1;
        }

        /// <summary>
        /// Adds an overlay graphics component to the viewport.
        /// </summary>
        /// <param name="component">The component to add.</param>
        public void AddOverlayComponent(OverlayComponent component)
        {
            _overlayComponents.Add(component);
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
            Vector2 min, max;
            GetWorldAreaMinAndMax(z, out min, out max);
            var safeRadius = volume.Radius + 1f;
            if (volume.Center.X + safeRadius < min.X) return false;
            if (volume.Center.Y + safeRadius < min.Y) return false;
            if (max.X < volume.Center.X - safeRadius) return false;
            if (max.Y < volume.Center.Y - safeRadius) return false;
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

        public void PrepareForDraw()
        {
            DoInMyViewport(() =>
            {
                Draw_InitializeParallaxIn3D();
                _postprocessor.PrepareForDisplay();
            });
        }

        public void Draw()
        {
            DoInMyViewport(() =>
            {
                _postprocessor.DisplayOnScreen();
                DrawOverlayComponents();
            });
        }

        public Matrix GetGameToScreenMatrix(float z)
        {
            return ViewMatrix * GetProjectionMatrix(z)
                * Matrix.CreateReflection(new Plane(Vector3.UnitY, 0))
                * Matrix.CreateTranslation(1, 1, 0)
                * Matrix.CreateScale(new Vector3(Viewport.Width, Viewport.Height, Viewport.MaxDepth - Viewport.MinDepth) / 2);
        }

        protected virtual void RenderGameWorld()
        {
            var gfx = AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice;
            //var view = ViewMatrix;
            if (AssaultWingCore.Instance.DataEngine.Arena == null) return; // workaround for ArenaEditor crash when window resized without arena being loaded first
            int layerIndex = AssaultWingCore.Instance.DataEngine.Arena.Layers.Count();
            gfx.Clear(ClearOptions.DepthBuffer, Color.Pink, 1, 0);
            foreach (var layer in AssaultWingCore.Instance.DataEngine.Arena.Layers)
            {
                Matrix translationMatrix = Matrix.CreateTranslation(0, 0, -1024 * layerIndex--);
                var view = Matrix.Multiply(ViewMatrix, translationMatrix);
                if (LayerDrawing != null && !LayerDrawing(layer)) continue;               
                float layerScale = GetScale(layer.Z);
                var projection = GetProjectionMatrix(layer.Z);

                // Note: These methods have ConditionalAttribute.
                // Only one of them will be executed at runtime.
                Draw_DrawParallaxIn3D(layer);
                Draw_DrawParallaxWithSpriteBatch(layer);

                Draw3D(layer, ref view, ref projection);
                Draw2D(layer, layerScale);
                if (GobDrawn != null) foreach (var gob in layer.Gobs) GobDrawn(gob);
            }
        }

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public virtual void LoadContent()
        {
            AssaultWingCore.Instance.GraphicsDeviceService.CheckReentrancyBegin();
            try
            {
                _spriteBatch = new SpriteBatch(AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice);
                Action<ICollection<Effect>> effectContainerUpdater = container =>
                {
                    container.Clear();
                    foreach (var name in _getPostprocessEffectNames())
                        container.Add(AssaultWingCore.Instance.Content.Load<Effect>(name));
                };
                _postprocessor = new TexturePostprocessor(AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice, RenderGameWorld, effectContainerUpdater);
                foreach (var component in _overlayComponents) component.LoadContent();
            }
            finally
            {
                AssaultWingCore.Instance.GraphicsDeviceService.CheckReentrancyEnd();
            }
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        public virtual void UnloadContent()
        {
            foreach (var component in _overlayComponents) component.UnloadContent();
            _postprocessor.Dispose();
            _spriteBatch.Dispose();
        }

        /// <summary>
        /// Called when allocated resources should be released.
        /// </summary>
        public virtual void Dispose()
        {
            UnloadContent();
            foreach (var component in _overlayComponents) component.Dispose();
        }

        protected abstract Vector2 GetLookAtPos();

        /// <summary>
        /// Returns the visual scaling factor at a depth in game coordinates.
        /// </summary>
        /// <param name="z">The depth, in game coordinates.</param>
        /// <returns>The scaling factor at the depth.</returns>
        private float GetScale(float z)
        {
            return 1000 / (1000 - z);
        }

        /// <summary>
        /// Returns the minimum and maximum coordinates of the game world this viewport shows at a depth.
        /// </summary>
        private void GetWorldAreaMinAndMax(float z, out Vector2 min, out Vector2 max)
        {
            var lookAtPos = GetLookAtPos();
            var halfDiagonal = new Vector2(Viewport.Width, Viewport.Height) / (2 * ZoomRatio * GetScale(z));
            min = lookAtPos - halfDiagonal;
            max = lookAtPos + halfDiagonal;
        }

        private void DoInMyViewport(Action action)
        {
            var gfx = AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice;
            var oldViewport = gfx.Viewport;
            gfx.Viewport = Viewport;
            action();
            gfx.Viewport = oldViewport;
        }

        private void Draw3D(ArenaLayer layer, ref Matrix view, ref Matrix projection)
        {
            foreach (var gob in layer.Gobs)
            {
                var bounds = gob.DrawBounds;
                if (gob.IsVisible && bounds.Radius > 0 && Intersects(bounds, layer.Z))
                    gob.Draw(view, projection);
            }
        }

        private void Draw2D(ArenaLayer layer, float layerScale)
        {
            DrawMode2D? drawMode = null;
            var gameToScreenMatrix = GetGameToScreenMatrix(layer.Z);
            layer.Gobs.ForEachIn2DOrder(gob =>
            {
                if (!gob.IsVisible) return;
                if (!drawMode.HasValue || drawMode.Value.CompareTo(gob.DrawMode2D) != 0)
                {
                    if (drawMode.HasValue)
                        drawMode.Value.EndDraw(AssaultWingCore.Instance, _spriteBatch);
                    drawMode = gob.DrawMode2D;
                    drawMode.Value.BeginDraw(AssaultWingCore.Instance, _spriteBatch);
                }
                gob.Draw2D(gameToScreenMatrix, _spriteBatch, layerScale * ZoomRatio);
            });
            if (drawMode.HasValue)
                drawMode.Value.EndDraw(AssaultWingCore.Instance, _spriteBatch);
        }

        private void DrawOverlayComponents()
        {
            var gfx = AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice;
            gfx.Viewport = Viewport;
            foreach (var component in _overlayComponents) component.Draw(_spriteBatch);
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
            if (_effect != null) return;
            var gfx = AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice;
            _effect = new BasicEffect(gfx);
            _effect.World = Matrix.Identity;
            _effect.Projection = Matrix.Identity;
            _effect.View = Matrix.Identity;
            _effect.TextureEnabled = true;
            _effect.LightingEnabled = false;
            _effect.FogEnabled = false;
            _effect.VertexColorEnabled = false;
            _vertexData = new[]
            {
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
        private void Draw_DrawParallaxIn3D(ArenaLayer layer)
        {
            var gfx = AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice;
            if (layer.ParallaxName != "")
            {
                BlendState oldState = gfx.BlendState;

                // Modify renderstate for parallax.
                gfx.SamplerStates[0] = SamplerState.LinearWrap;
                gfx.BlendState = BlendState.AlphaBlend;
                gfx.DepthStencilState = DepthStencilState.None;

                // Render looping parallax as two huge triangles.
                _effect.Texture = AssaultWingCore.Instance.Content.Load<Texture2D>(layer.ParallaxName);
                var texCenter = GetScale(layer.Z) * GetLookAtPos() / _effect.Texture.Dimensions();
                var texCornerOffset = new Vector2(
                    Viewport.Width / (2f * _effect.Texture.Width) / ZoomRatio,
                    Viewport.Height / (2f * _effect.Texture.Height)) / ZoomRatio;
                _vertexData[0].TextureCoordinate = texCenter - texCornerOffset;
                _vertexData[1].TextureCoordinate = texCenter + new Vector2(-texCornerOffset.X, texCornerOffset.Y);
                _vertexData[2].TextureCoordinate = texCenter + new Vector2(texCornerOffset.X, -texCornerOffset.Y);
                _vertexData[3].TextureCoordinate = texCenter + texCornerOffset;
                _effect.CurrentTechnique.Passes[0].Apply();
                gfx.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleStrip, _vertexData, 0, 2);
            }
            // Modify renderstate for 3D graphics.
            gfx.SamplerStates[0] = SamplerState.LinearWrap;
            gfx.DepthStencilState = DepthStencilState.Default;
            gfx.BlendState = BlendState.Opaque;
        }

        /// <summary>
        /// HACK: The old way of drawing parallaxes, with several calls to SpriteBatch.Draw.
        /// </summary>
        [Conditional("PARALLAX_WITH_SPRITE_BATCH")]
        private void Draw_DrawParallaxWithSpriteBatch(ArenaLayer layer)
        {
            var gfx = AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice;
            if (layer.ParallaxName != "")
            {
                _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                var tex = AssaultWingCore.Instance.Content.Load<Texture2D>(layer.ParallaxName);
                var lookAtPosScaled = GetScale(layer.Z) * GetLookAtPos();
                float texCenterX = lookAtPosScaled.X.Modulo(tex.Width);
                float texCenterY = (-lookAtPosScaled.Y).Modulo(tex.Height);
                float screenStartX = (Viewport.Width / 2f - texCenterX * ZoomRatio).Modulo(tex.Width * ZoomRatio) - tex.Width * ZoomRatio;
                float screenStartY = (Viewport.Height / 2f - texCenterY * ZoomRatio).Modulo(tex.Height * ZoomRatio) - tex.Height * ZoomRatio;
                for (float posX = screenStartX; posX <= Viewport.Width; posX += tex.Width * ZoomRatio)
                    for (float posY = screenStartY; posY <= Viewport.Height; posY += tex.Height * ZoomRatio)
                        _spriteBatch.Draw(tex, new Vector2(posX, posY), null, Color.White, 0, Vector2.Zero, ZoomRatio, SpriteEffects.None, 1);
                _spriteBatch.End();
            }

            // Modify renderstate for 3D graphics.
            gfx.SamplerStates[0] = SamplerState.LinearWrap;
            gfx.DepthStencilState = DepthStencilState.Default;
            gfx.BlendState = BlendState.Opaque;
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
        public bool Vertical;

        /// <summary>
        /// The X coordinate of a vertical separator, or
        /// the Y coordinate of a horizontal separator.
        /// </summary>
        public int Coordinate;

        /// <summary>
        /// Creates a new viewport separator.
        /// </summary>
        /// <param name="vertical">Is the separator vertical.</param>
        /// <param name="coordinate">The X or Y coordinate of the separator.</param>
        public ViewportSeparator(bool vertical, int coordinate)
        {
            Vertical = vertical;
            Coordinate = coordinate;
        }
    }
}
