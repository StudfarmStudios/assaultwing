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
        int viewWidth, viewHeight; // how many pixels to show scaled down to the screen
        int screenWidth, screenHeight; // last known dimensions of client bounds

        /// <summary>
        /// Movement curve of 'view' from 'viewFrom' to 'viewTo' as a function of 
        /// seconds from start of movement to linear interpolation weight (0..1) of 
        /// 'viewTo' against 'viewFrom'.
        /// </summary>
        Curve viewMoveCurve;

        // The menu system draws a shadow on the screen as this transparent 3D object.
        VertexPositionColor[] shadowVertexData;
        int[] shadowIndexData; // stored as a triangle list
        VertexDeclaration vertexDeclaration;
        BasicEffect effect;

        SpriteBatch spriteBatch;
        Texture2D backgroundTexture;
        SpriteFont smallFont;

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

            screenWidth = AssaultWing.Instance.ClientBounds.Width;
            screenHeight = AssaultWing.Instance.ClientBounds.Height;
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
            smallFont = data.GetFont(FontName.MenuFontSmall);
            components[(int)MenuComponentType.Main] = new MainMenuComponent(this);
            components[(int)MenuComponentType.Equip] = new EquipMenuComponent(this);
            components[(int)MenuComponentType.Arena] = new ArenaMenuComponent(this);

            WindowResize();
            ActivateComponent(MenuComponentType.Main);
            base.Initialize();
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
            viewFrom = view;
            viewTo = newComponent.Center - new Vector2(viewWidth, viewHeight) / 2;
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
            screen.Width = viewWidth;
            screen.Height = viewHeight;

            // If client bounds are very small, render everything
            // to a separate render target of reasonable size, 
            // then scale the target to the screen.
            RenderTarget2D renderTarget = null;
            DepthStencilBuffer oldDepthStencilBuffer = null;
            if (screenWidth < viewWidth || screenHeight < viewHeight)
            {
                renderTarget = new RenderTarget2D(gfx, viewWidth, viewHeight, 1, gfx.DisplayMode.Format);

                // Set up graphics device.
                oldDepthStencilBuffer = gfx.DepthStencilBuffer;
                gfx.DepthStencilBuffer = null;

                // Set our own render target.
                gfx.SetRenderTarget(0, renderTarget);
            }

            // Begin drawing.
            gfx.Viewport = screen;
            gfx.Clear(ClearOptions.Target, Color.DimGray, 0, 0);
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

            // Draw static text.
            spriteBatch.Begin();
            spriteBatch.DrawString(smallFont, "First Playable Demo 2008-04-27",
                new Vector2(10, viewHeight - smallFont.LineSpacing), Color.White);
            string helpText = "Enter to proceed, Esc to return to previous";
            Vector2 helpTextPos = new Vector2(
                ((float)viewWidth-smallFont.MeasureString(helpText).X) / 2,
                viewHeight - smallFont.LineSpacing);
            spriteBatch.DrawString(smallFont, helpText, helpTextPos, Color.White);
            string copyrightText = "Studfarm Studios";
            Vector2 copyrightTextPos = new Vector2(
                viewWidth - (int)smallFont.MeasureString(copyrightText).X - 10,
                viewHeight - smallFont.LineSpacing);
            spriteBatch.DrawString(smallFont, copyrightText, copyrightTextPos, Color.White);
            spriteBatch.End();

            // If we're stretching the view, take the temporary render target
            // and draw its contents to the screen.
            if (screenWidth < viewWidth || screenHeight < viewHeight)
            {
                // Restore render target so what we can extract drawn pixels.
                gfx.SetRenderTarget(0, null);
                Texture2D renderTexture = renderTarget.GetTexture();

                // Restore the graphics device to the real backbuffer.
                screen.Width = screenWidth;
                screen.Height = screenHeight;
                gfx.Viewport = screen;
                gfx.DepthStencilBuffer = oldDepthStencilBuffer;

                // Draw the texture stretched on the screen.
                Rectangle destination = new Rectangle(0, 0, screen.Width, screen.Height);
                spriteBatch.Begin();
                spriteBatch.Draw(renderTexture, destination, Color.White);
                spriteBatch.End();

                // Dispose of needless data.
                renderTexture.Dispose();
                renderTarget.Dispose();
            }
        }

        /// <summary>
        /// Reacts to a system window resize.
        /// </summary>
        /// This method should be called after the window size changes in windowed mode,
        /// or after the screen resolution changes in fullscreen mode,
        /// or after switching between windowed and fullscreen mode.
        public void WindowResize()
        {
            screenWidth = AssaultWing.Instance.ClientBounds.Width;
            screenHeight = AssaultWing.Instance.ClientBounds.Height;

            int oldViewWidth = viewWidth;
            int oldViewHeight = viewHeight;
            SetViewDimensions();
            InitializeShadow();

            // Make menu view move to a new position suitable for the new client bounds.
            Vector2 displacement = new Vector2(oldViewWidth - viewWidth, oldViewHeight - viewHeight) / 2;
            viewFrom += displacement;
            viewTo += displacement;
        }

        /// <summary>
        /// Sets view dimensions (<c>viewWidth</c> and <c>viewHeight</c>)
        /// based on current screen dimensions (<c>screenWidth</c> and <c>screenHeight</c>).
        /// </summary>
        /// This method decides if the menu view will be shrunk down to fit
        /// more information on a small screen.
        private void SetViewDimensions()
        {
            // If client bounds are very small, scale the menu view down
            // to fit more in the screen.
            viewWidth = AssaultWing.Instance.ClientBounds.Width;
            viewHeight = AssaultWing.Instance.ClientBounds.Height;
            int screenWidthMin = 1; // least screen width that doesn't require scaling
            int screenHeightMin = 1; // least screen height that doesn't require scaling
            if (screenWidth < screenWidthMin)
            {
                viewWidth = screenWidthMin;
                viewHeight = viewWidth * screenHeight / screenWidth;
            }
            if (screenHeight < screenHeightMin)
            {
                // Follow the stronger scale if there is a limitation both by width and by height.
                if (viewHeight < screenHeightMin)
                {
                    viewHeight = screenHeightMin;
                    viewWidth = viewHeight * screenWidth / screenHeight;
                }
            }
        }

        /// <summary>
        /// Initialises fields <c>shadowVertexData</c> and <c>shadowIndexData</c>
        /// to a shadow 3D model that fills the current client bounds.
        /// </summary>
        void InitializeShadow()
        {
            // The shadow is a rectangle that spans a grid of vertices, each
            // of them black but with different levels of alpha.
            // The origin of the shadow 3D model is at the top center.
            Vector2 shadowDimensions = new Vector2(viewWidth, viewHeight);
            int gridWidth = (int)shadowDimensions.X / 30;
            int gridHeight = (int)shadowDimensions.Y / 30;
            Curve alphaCurve = new Curve(); // value of alpha as a function of distance in pixels from shadow origin
            alphaCurve.Keys.Add(new CurveKey(0, 0));
            alphaCurve.Keys.Add(new CurveKey(500, 120));
            alphaCurve.Keys.Add(new CurveKey(1000, 255));
            alphaCurve.PreLoop = CurveLoopType.Constant;
            alphaCurve.PostLoop = CurveLoopType.Constant;
            alphaCurve.ComputeTangents(CurveTangent.Smooth);
            List<VertexPositionColor> vertexData = new List<VertexPositionColor>();
            List<int> indexData = new List<int>();
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
                            indexData.Add(y * gridWidth + x);
                            indexData.Add(y * gridWidth + x - 1);
                            indexData.Add((y - 1) * gridWidth + x);
                        }
                        if (x < gridWidth - 1)
                        {
                            indexData.Add(y * gridWidth + x);
                            indexData.Add((y - 1) * gridWidth + x);
                            indexData.Add((y - 1) * gridWidth + x + 1);
                        }
                    }
                }
            shadowVertexData = vertexData.ToArray();
            shadowIndexData = indexData.ToArray();
        }
    }
}
