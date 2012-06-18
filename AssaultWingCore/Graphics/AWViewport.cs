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
        /// <summary>
        /// Effect for drawing parallaxes as 3D primitives.
        /// </summary>
        private BasicEffect _effect;

        /// <summary>
        /// Vertex data scratch buffer for drawing parallaxes as 3D primitives.
        /// </summary>
        private VertexPositionTexture[] _vertexData;

        private List<OverlayComponent> _overlayComponents;
        private TexturePostprocessor _postprocessor;
        private Func<IEnumerable<CanonicalString>> _getPostprocessEffectNames;
        private Vector2 _previousLookAt;

        public AssaultWingCore Game { get; private set; }
        public virtual Player Owner { get { return null; } }

        /// <summary>
        /// Ratio of screen pixels to game world meters. Default value is 1.
        /// </summary>
        public float ZoomRatio { get; set; }

        /// <summary>
        /// The area of the viewport on the render target surface.
        /// </summary>
        public Rectangle OnScreen { get { return new Rectangle(Viewport.X, Viewport.Y, Viewport.Width, Viewport.Height); } }

        public Vector2 CurrentLookAt { get; protected set; }
        public Vector2 Move { get { return Game.TargetFPS * (CurrentLookAt - _previousLookAt); } }
        public event Func<ArenaLayer, bool> LayerDrawing;
        public event Action<Gob> GobDrawn;

        /// <summary>
        /// The area of the display to draw on.
        /// </summary>
        private Viewport Viewport { get; set; }

        private SpriteBatch SpriteBatch { get { return Game.GraphicsEngine.GameContent.ViewportSpriteBatch; } }
        private GraphicsDevice GraphicsDevice { get { return Game.GraphicsDeviceService.GraphicsDevice; } }

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
                return Matrix.CreateLookAt(new Vector3(CurrentLookAt, 1000), new Vector3(CurrentLookAt, 0), Vector3.Up);
            }
        }

        /// <param name="onScreen">Where on screen is the viewport located.</param>
        protected AWViewport(AssaultWingCore game, Rectangle onScreen, Func<IEnumerable<CanonicalString>> getPostprocessEffectNames)
        {
            Game = game;
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
            // !!! GobDrawn += gob => game.DataEngine.Arena.DebugDrawGob(gob, ViewMatrix, GetProjectionMatrix(gob.Layer.Z));
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
            var worldPos = Viewport.Unproject(viewPos, GetProjectionMatrix(z), ViewMatrix, Matrix.Identity);
            return worldPos.ProjectXY();
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
            return new Ray(nearWorld, Vector3.Normalize(farWorld - nearWorld));
        }

        public void PrepareForDraw()
        {
            DoInMyViewport(_postprocessor.PrepareForDisplay);
        }

        public void Draw()
        {
            DoInMyViewport(() =>
            {
                _postprocessor.DisplayOnScreen();
                DrawOverlayComponents();
            });
        }

        public virtual void Update()
        {
            _previousLookAt = CurrentLookAt;
            foreach (var component in _overlayComponents) component.Update();
        }

        public abstract void Reset(Vector2 lookAtPos);

        public Matrix GetGameToScreenMatrix(float z)
        {
            return ViewMatrix * GetProjectionMatrix(z)
                * Matrix.CreateReflection(new Plane(Vector3.UnitY, 0))
                * Matrix.CreateTranslation(1, 1, 0)
                * Matrix.CreateScale(new Vector3(Viewport.Width, Viewport.Height, Viewport.MaxDepth - Viewport.MinDepth) / 2);
        }

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public virtual void LoadContent()
        {
            Action<ICollection<Effect>> effectContainerUpdater = container =>
            {
                container.Clear();
                foreach (var name in _getPostprocessEffectNames())
                    container.Add(AssaultWingCore.Instance.Content.Load<Effect>(name));
            };
            _postprocessor = new TexturePostprocessor(AssaultWingCore.Instance, RenderGameWorld, effectContainerUpdater);
            _effect = new BasicEffect(AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice);
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
            foreach (var component in _overlayComponents) component.LoadContent();
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        public virtual void UnloadContent()
        {
            foreach (var component in _overlayComponents) component.UnloadContent();
            if (_postprocessor != null)
            {
                _postprocessor.Dispose();
                _postprocessor = null;
            }
            if (_effect != null)
            {
                _effect.Dispose();
                _effect = null;
            }
        }

        /// <summary>
        /// Called when allocated resources should be released.
        /// </summary>
        public virtual void Dispose()
        {
            UnloadContent();
            foreach (var component in _overlayComponents) component.Dispose();
        }

        /// <summary>
        /// Returns the visual scaling factor at a depth in game coordinates.
        /// </summary>
        /// <param name="z">The depth, in game coordinates.</param>
        /// <returns>The scaling factor at the depth.</returns>
        private float GetScale(float z)
        {
            return 1000 / (1000 - z);
        }

        private void RenderGameWorld()
        {
            if (Game.DataEngine.Arena == null) return; // workaround for ArenaEditor crash when window resized without arena being loaded first
            var layerIndex = Game.DataEngine.Arena.Layers.Count();
            GraphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Pink, 1, 0);
            foreach (var layer in Game.DataEngine.Arena.Layers)
            {
                var translationMatrix = Matrix.CreateTranslation(0, 0, -1024 * layerIndex--);
                var view = Matrix.Multiply(ViewMatrix, translationMatrix);
                if (LayerDrawing != null && !LayerDrawing(layer)) continue;
                var layerScale = GetScale(layer.Z);
                var projection = GetProjectionMatrix(layer.Z);
                DrawParallax(layer);
                Draw3D(layer, ref view, ref projection);
                Draw2D(layer, layerScale);
                if (GobDrawn != null) foreach (var gob in layer.Gobs) GobDrawn(gob);
            }
        }

        private void DoInMyViewport(Action action)
        {
            var oldViewport = GraphicsDevice.Viewport;
            GraphicsDevice.Viewport = Viewport;
            action();
            GraphicsDevice.Viewport = oldViewport;
        }

        private void Draw3D(ArenaLayer layer, ref Matrix view, ref Matrix projection)
        {
            foreach (var gob in layer.Gobs)
            {
                var bounds = gob.DrawBounds;
                if (bounds.Radius <= 0 || !Intersects(bounds.Transform(view), layer.Z)) continue;
                gob.Draw3D(view, projection, Owner);
            }
        }

        /// <summary>
        /// Checks if a bounding volume might be visible in the viewport at a certain depth.
        /// Returns false if the bounding volume definitely cannot be seen in the viewport.
        /// </summary>
        private bool Intersects(BoundingSphere volume, float z)
        {
            var halfDiagonal = new Vector2(Viewport.Width, Viewport.Height) / (2 * ZoomRatio * GetScale(z));
            var safeRadius = volume.Radius + 1f;
            if (volume.Center.X + safeRadius < -halfDiagonal.X) return false;
            if (volume.Center.Y + safeRadius < -halfDiagonal.Y) return false;
            if (halfDiagonal.X < volume.Center.X - safeRadius) return false;
            if (halfDiagonal.Y < volume.Center.Y - safeRadius) return false;
            return true;
        }

        private void Draw2D(ArenaLayer layer, float layerScale)
        {
            DrawMode2D? drawMode = null;
            var gameToScreenMatrix = GetGameToScreenMatrix(layer.Z);
            layer.Gobs.ForEachIn2DOrder(gob =>
            {
                if (!drawMode.HasValue || drawMode.Value.CompareTo(gob.DrawMode2D) != 0)
                {
                    if (drawMode.HasValue)
                        drawMode.Value.EndDraw(AssaultWingCore.Instance, SpriteBatch);
                    drawMode = gob.DrawMode2D;
                    drawMode.Value.BeginDraw(AssaultWingCore.Instance, SpriteBatch);
                }
                gob.Draw2D(gameToScreenMatrix, SpriteBatch, layerScale * ZoomRatio, Owner);
            });
            if (drawMode.HasValue)
                drawMode.Value.EndDraw(AssaultWingCore.Instance, SpriteBatch);
        }

        private void DrawOverlayComponents()
        {
            GraphicsDevice.Viewport = Viewport;
            foreach (var component in _overlayComponents) component.Draw(SpriteBatch);
        }

        private void DrawParallax(ArenaLayer layer)
        {
            if (layer.ParallaxName != "")
            {
                var oldState = GraphicsDevice.BlendState;

                // Modify renderstate for parallax.
                GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
                GraphicsDevice.BlendState = BlendState.AlphaBlend;
                GraphicsDevice.DepthStencilState = DepthStencilState.None;

                // Render looping parallax as two huge triangles.
                _effect.Texture = AssaultWingCore.Instance.Content.Load<Texture2D>(layer.ParallaxName);
                var texCenter = GetScale(layer.Z) * CurrentLookAt / _effect.Texture.Dimensions();
                var texCornerOffset = new Vector2(
                    Viewport.Width / (2f * _effect.Texture.Width) / ZoomRatio,
                    Viewport.Height / (2f * _effect.Texture.Height) / ZoomRatio);
                _vertexData[0].TextureCoordinate = texCenter - texCornerOffset;
                _vertexData[1].TextureCoordinate = texCenter + texCornerOffset.MirrorX();
                _vertexData[2].TextureCoordinate = texCenter + texCornerOffset.MirrorY();
                _vertexData[3].TextureCoordinate = texCenter + texCornerOffset;
                _effect.CurrentTechnique.Passes[0].Apply();
                GraphicsDevice.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleStrip, _vertexData, 0, 2);
            }
            // Modify renderstate for 3D graphics.
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;
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
