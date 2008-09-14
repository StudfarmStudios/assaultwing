using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AW2.Events;
using AW2.Game;
using AW2.Graphics;
using AW2.UI;
using Microsoft.Xna.Framework.Input;

namespace AW2.Menu
{
    /// <summary>
    /// Type of menu component, one for each subclass of <c>MenuComponent</c>.
    /// </summary>
    public enum MenuComponentType
    {
        /// <summary>
        /// The main menu component.
        /// </summary>
        Main = 0,

        /// <summary>
        /// The equip menu component.
        /// </summary>
        Equip,

        /// <summary>
        /// The arena select menu component.
        /// </summary>
        Arena,
    }

    /// <summary>
    /// A menu system consisting of menu components.
    /// </summary>
    /// The menu system has its own coordinate system, where positive X is to the right
    /// and positive Y is down. Menu components lie in this coordinate space all
    /// at the same time. The menu system maintains a view to the menu space. The view
    /// is defined by the coordinates of the top left coordinates of the viewport
    /// in the menu coordinate system.
    public class MenuEngineImpl : DrawableGameComponent
    {
        MenuComponentType activeComponent;
        MenuComponent[] components;
        Vector2 view; // top left corner of menu view in menu system coordinates
        Vector2 viewFrom, viewTo; // start and goal of current movement of 'view'
        TimeSpan viewMoveStartTime; // time of start of view movement

        /// <summary>
        /// Movement curve of 'view' from 'viewFrom' to 'viewTo' as a function of 
        /// seconds from start of movement to linear interpolation weight (0..1) of 
        /// 'viewTo' against 'viewFrom'.
        /// </summary>
        Curve viewMoveCurve;

        // The menu system draws a shadow on the screen as this transparent 3D object.
        VertexPositionColor[] shadowVertexData;
        short[] shadowIndexData; // stored as a triangle list
        VertexDeclaration vertexDeclaration;
        BasicEffect effect;

        SpriteBatch spriteBatch;
        Texture2D backgroundTexture;

        /// <summary>
        /// Creates a menu system.
        /// </summary>
        /// <param name="game"></param>
        public MenuEngineImpl(Microsoft.Xna.Framework.Game game)
            : base(game)
        {
            components = new MenuComponent[Enum.GetValues(typeof(MenuComponentType)).Length];
            // The components are created in Initialize() when other resources are ready.

            viewMoveCurve = new Curve();
            viewMoveCurve.Keys.Add(new CurveKey(0.0f, 0.0f));
            viewMoveCurve.Keys.Add(new CurveKey(0.5f, 0.8f));
            viewMoveCurve.Keys.Add(new CurveKey(1.0f, 0.98f));
            viewMoveCurve.Keys.Add(new CurveKey(1.5f, 1.0f));
            viewMoveCurve.PreLoop = CurveLoopType.Constant;
            viewMoveCurve.PostLoop = CurveLoopType.Constant;
            viewMoveCurve.ComputeTangents(CurveTangent.Smooth);
            viewMoveCurve.Keys[0].TangentOut = 0;
            viewMoveCurve.Keys[viewMoveCurve.Keys.Count - 1].TangentIn = 0;

            InitializeShadow();
        }

        /// <summary>
        /// Called when the component needs to load graphics resources. Override this
        /// method to load any component-specific graphics resources.
        /// </summary>
        protected override void LoadContent()
        {
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            vertexDeclaration = new VertexDeclaration(gfx, VertexPositionColor.VertexElements); 
            effect = new BasicEffect(gfx, null);
            effect.FogEnabled = false;
            effect.LightingEnabled = false;
            effect.View = Matrix.CreateLookAt(100 * Vector3.UnitZ, Vector3.Zero, Vector3.Up);
            effect.World = Matrix.Identity;
            effect.VertexColorEnabled = true;
            effect.TextureEnabled = false;
        }

        /// <summary>
        /// Called when graphics resources should be unloaded. 
        /// Handle component-specific graphics resources.
        /// </summary>
        protected override void UnloadContent()
        {
            spriteBatch.Dispose();
            spriteBatch = null;
            effect.Dispose();
            effect = null;
            base.UnloadContent();
        }

        /// <summary>
        /// Initialises the menu system.
        /// </summary>
        public override void Initialize()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            spriteBatch = new SpriteBatch(AssaultWing.Instance.GraphicsDevice);
            backgroundTexture = data.GetTexture(TextureName.MenuBackground);
            components[(int)MenuComponentType.Main] = new MainMenuComponent(this);
            components[(int)MenuComponentType.Equip] = new EquipMenuComponent(this);
            components[(int)MenuComponentType.Arena] = new ArenaMenuComponent(this);

            WindowResize();
            base.Initialize();

            // Set initial menu view position and active menu component.
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            viewFrom = viewTo = components[(int)MenuComponentType.Main].Center
                - new Vector2(gfx.Viewport.Width, gfx.Viewport.Height) / 2;
            viewMoveStartTime = AssaultWing.Instance.GameTime.ElapsedRealTime;
            ActivateComponent(MenuComponentType.Main);
        }

        /// <summary>
        /// Activates a menu component after deactivating the previously active menu component
        /// and moving the menu view to the new component.
        /// </summary>
        /// <param name="component">The menu component to activate and move menu view to.</param>
        public void ActivateComponent(MenuComponentType component)
        {
            components[(int)activeComponent].Active = false;
            activeComponent = component;
            MenuComponent newComponent = components[(int)activeComponent];

            // Make menu view scroll to the new component's position.
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            viewFrom = view;
            viewTo = newComponent.Center - new Vector2(gfx.Viewport.Width, gfx.Viewport.Height) / 2;
            viewMoveStartTime = AssaultWing.Instance.GameTime.TotalRealTime;

            // The new component will be activated in 'Update()' when the view is closer to its center.
        }

        /// <summary>
        /// Updates the menu system.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Microsoft.Xna.Framework.GameComponent.Update(Microsoft.Xna.Framework.GameTime)</param>
        public override void Update(GameTime gameTime)
        {
            // Update menu view position.
            float moveTime = (float)(AssaultWing.Instance.GameTime.TotalRealTime - viewMoveStartTime).TotalSeconds;
            float moveWeight = viewMoveCurve.Evaluate(moveTime);
            view = Vector2.Lerp(viewFrom, viewTo, moveWeight);

            // Activate 'activeComponent' if the view has just come close enough to its center.
            if (!components[(int)activeComponent].Active && moveWeight > 0.7f)
                components[(int)activeComponent].Active = true;

            // Update menu components.
            foreach (MenuComponent component in components)
                component.Update();
        }

        /// <summary>
        /// Draws the menu system.
        /// </summary>
        /// <param name="gameTime">
        /// Time passed since the last call to Microsoft.Xna.Framework.DrawableGameComponent.Draw(Microsoft.Xna.Framework.GameTime).
        /// </param>
        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            Viewport screen = gfx.Viewport;
            screen.X = 0;
            screen.Y = 0;
            screen.Width = AssaultWing.Instance.ClientBounds.Width;
            screen.Height = AssaultWing.Instance.ClientBounds.Height;
#if false
            // If client bounds are very small, render everything
            // to a separate render target of reasonable size, 
            // then scale the target to the screen.
            if (screen.Width < 800 || screen.Height < 800)
            {
                GraphicsDeviceCapabilities gfxCaps = gfx.GraphicsDeviceCapabilities;
                int targetWidth = (int)arenaDimensionsOnRadar.X;
                int targetHeight = (int)arenaDimensionsOnRadar.Y;
                GraphicsAdapter gfxAdapter = gfx.CreationParameters.Adapter;
                if (!gfxAdapter.CheckDeviceFormat(DeviceType.Hardware, gfx.DisplayMode.Format,
                    TextureUsage.None, QueryUsages.None, ResourceType.RenderTarget, SurfaceFormat.Color))
                    throw new Exception("Cannot create render target of type SurfaceFormat.Color");
                RenderTarget2D maskTarget = new RenderTarget2D(gfx, targetWidth, targetHeight,
                    1, SurfaceFormat.Color);

                // Set up graphics device.
                DepthStencilBuffer oldDepthStencilBuffer = gfx.DepthStencilBuffer;
                gfx.DepthStencilBuffer = null;

                // Set and clear our own render target.
                gfx.SetRenderTarget(0, maskTarget);
                gfx.Clear(ClearOptions.Target, Color.TransparentBlack, 0, 0);

            // Draw

                // Restore render target so what we can extract drawn pixels.
                // Create a copy of the texture in local memory so that a graphics device
                // reset (e.g. when changing resolution) doesn't lose the texture.
                gfx.SetRenderTarget(0, null);
                Color[] textureData = new Color[targetHeight * targetWidth];
                maskTarget.GetTexture().GetData(textureData);
                arenaRadarSilhouette = new Texture2D(gfx, targetWidth, targetHeight, 1, TextureUsage.None, SurfaceFormat.Color);
                arenaRadarSilhouette.SetData(textureData);

                // Restore graphics device's old settings.
                gfx.DepthStencilBuffer = oldDepthStencilBuffer;
                maskTarget.Dispose();

                // Draw texture on screen.
            }
#endif
            gfx.Viewport = screen;
            gfx.Clear(Color.DimGray);
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);

            // Draw background looping all over the screen.
            float yStart = view.Y < 0
                ? -(view.Y % backgroundTexture.Height) - backgroundTexture.Height
                : -(view.Y % backgroundTexture.Height);
            float xStart = view.X < 0
                ? -(view.X % backgroundTexture.Width) - backgroundTexture.Width
                : -(view.X % backgroundTexture.Width);
            for (float y = yStart; y < screen.Height; y += backgroundTexture.Height)
                for (float x = xStart; x < screen.Width; x += backgroundTexture.Width)
                    spriteBatch.Draw(backgroundTexture, new Vector2(x, y), Color.White);

            // Draw menu components.
            foreach (MenuComponent component in components)
                component.Draw(view, spriteBatch);

            spriteBatch.End();

            // Draw menu focus shadow.
            gfx.VertexDeclaration = vertexDeclaration;
            effect.Projection = Matrix.CreateOrthographicOffCenter(
                -screen.Width / 2, screen.Width / 2,
                -screen.Height, 0,
                1, 500);
            effect.Begin();
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Begin();
                gfx.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.TriangleList,
                    shadowVertexData, 0, shadowVertexData.Length,
                    shadowIndexData, 0, shadowIndexData.Length / 3);
                pass.End();
            }
            effect.End();
        }

        /// <summary>
        /// Reacts to a system window resize.
        /// </summary>
        /// This method should be called after the window size changes in windowed mode,
        /// or after the screen resolution changes in fullscreen mode,
        /// or after switching between windowed and fullscreen mode.
        public void WindowResize()
        {
            // TODO: Make menu view move to a new position suitable for the new client bounds.
        }

        /// <summary>
        /// Initialises fields <c>shadowVertexData</c> and <c>shadowIndexData</c>.
        /// </summary>
        void InitializeShadow()
        {
            // The shadow is a rectangle that spans a grid of vertices, each
            // of them black but with different levels of alpha.
            // The origin of the shadow 3D model is at the top center.
            Vector2 shadowDimensions = new Vector2(5000, 5000);
            int gridWidth = 25;
            int gridHeight = 25;
            Curve alphaCurve = new Curve(); // value of alpha as a function of distance in pixels from shadow origin
            alphaCurve.Keys.Add(new CurveKey(   0,   0));
            alphaCurve.Keys.Add(new CurveKey( 500, 120));
            alphaCurve.Keys.Add(new CurveKey(1000, 255));
            alphaCurve.PreLoop = CurveLoopType.Constant;
            alphaCurve.PostLoop = CurveLoopType.Constant;
            alphaCurve.ComputeTangents(CurveTangent.Smooth);
            List<VertexPositionColor> vertexData = new List<VertexPositionColor>();
            List<short> indexData = new List<short>();
            for (int y = 0; y < gridHeight; ++y)
                for (int x = 0; x < gridWidth; ++x)
                {
                    Vector2 posInShadow = shadowDimensions * 
                        new Vector2((float)x / (gridWidth - 1) - 0.5f, (float)-y / (gridHeight - 1));
                    float distance = posInShadow.Length();
                    vertexData.Add(new VertexPositionColor(
                        new Vector3(posInShadow, 0),
                        new Color(0, 0, 0, (byte)alphaCurve.Evaluate(distance))));
                    if (y > 0)
                    {
                        if (x > 0)
                        {
                            indexData.Add((short)(y * gridWidth + x));
                            indexData.Add((short)(y * gridWidth + x - 1));
                            indexData.Add((short)((y - 1) * gridWidth + x));
                        }
                        if (x < gridWidth - 1)
                        {
                            indexData.Add((short)(y * gridWidth + x));
                            indexData.Add((short)((y - 1) * gridWidth + x));
                            indexData.Add((short)((y - 1) * gridWidth + x + 1));
                        }
                    }
                }
            shadowVertexData = vertexData.ToArray();
            shadowIndexData = indexData.ToArray();
        }
    }
}
