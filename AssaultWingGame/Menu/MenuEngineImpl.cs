using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Core;
using AW2.Game;
using AW2.Graphics;
using AW2.Helpers;
using AW2.Sound;
using AW2.UI;

namespace AW2.Menu
{
    /// <summary>
    /// A menu system consisting of menu components.
    /// </summary>
    /// The menu system has its own coordinate system, where positive X is to the right
    /// and positive Y is down. Menu components lie in this coordinate space all
    /// at the same time. The menu system maintains a view to the menu space. The view
    /// is defined by the coordinates of the top left coordinates of the viewport
    /// in the menu coordinate system.
    /// 
    /// The menu system draws some static text on top of the menu view. This includes
    /// an optional help text and an optional progress bar.
    public class MenuEngineImpl : AWGameComponent
    {
        /// <summary>
        /// Cursor fade curve as a function of time in seconds.
        /// Values range from 0 (transparent) to 1 (opaque).
        /// </summary>
        private static Curve g_cursorFade;

        private MenuComponentType _activeComponent;
        private MenuComponent[] _components;
        private bool _activeComponentActivatedOnce, _activeComponentSoundPlayedOnce;
        private bool _showHelpText, _showProgressBar;
        private string _helpText;
        private Action _finishAction; // what to do when progress bar finishes
        private Vector2 _view; // center of menu view in menu system coordinates
        private Vector2 _viewTarget;
        private MovementCurve _viewCurve;
        private SoundInstance _menuChangeSound;
        private int _viewWidth, _viewHeight; // how many pixels to show scaled down to the screen
        private int _screenWidth, _screenHeight; // last known dimensions of client bounds
        private TimeSpan _cursorFadeStartTime;

        // The menu system draws a shadow on the screen as this transparent 3D object.
        private VertexPositionColor[] _shadowVertexData;
        private int[] _shadowIndexData; // stored as a triangle list
        private VertexDeclaration _vertexDeclaration;
        private BasicEffect _effect;

        private SpriteBatch _spriteBatch;
        private Texture2D _backgroundTexture;
        private SpriteFont _smallFont;

        public MenuContent MenuContent { get; private set; }

        /// <summary>
        /// Is the help text visible.
        /// </summary>
        /// <seealso cref="HelpText"/>
        public bool IsHelpTextVisible { get { return _showHelpText; } set { _showHelpText = value; } }

        /// <summary>
        /// The help text. Assigning <c>null</c> will restore the default help text.
        /// </summary>
        /// <seealso cref="IsHelpTextVisible"/>
        public string HelpText { get { return _helpText; } set { _helpText = value ?? "Enter to proceed, Esc to return to previous"; } }

        /// <summary>
        /// Is the progress bar visible.
        /// </summary>
        /// <seealso cref="DataEngine.ProgressBar"/>
        public bool IsProgressBarVisible
        {
            get { return _showProgressBar; }
            set
            {
                _showProgressBar = value;
                if (value)
                {
                    var progressBar = AssaultWingCore.Instance.DataEngine.ProgressBar;
                    progressBar.HorizontalAlignment = HorizontalAlignment.Center;
                    progressBar.VerticalAlignment = VerticalAlignment.Bottom;
                    progressBar.CustomAlignment = new Vector2(0, -2);
                }
            }
        }

        static MenuEngineImpl()
        {
            g_cursorFade = new Curve();
            g_cursorFade.Keys.Add(new CurveKey(0, 1, 0, 0, CurveContinuity.Step));
            g_cursorFade.Keys.Add(new CurveKey(0.5f, 0, 0, 0, CurveContinuity.Step));
            g_cursorFade.Keys.Add(new CurveKey(1, 1, 0, 0, CurveContinuity.Step));
            g_cursorFade.PreLoop = CurveLoopType.Cycle;
            g_cursorFade.PostLoop = CurveLoopType.Cycle;
        }

        public MenuEngineImpl()
        {
            MenuContent = new MenuContent();
            _components = new MenuComponent[Enum.GetValues(typeof(MenuComponentType)).Length];
            // The components are created in Initialize() when other resources are ready.

            HelpText = null; // initialises helpText to default value
        }

        public float GetCursorFade()
        {
            float cursorTime = (float)(AssaultWingCore.Instance.GameTime.TotalRealTime - _cursorFadeStartTime).TotalSeconds;
            return g_cursorFade.Evaluate(cursorTime);
        }

        public void ResetCursorFade()
        {
            _cursorFadeStartTime = AssaultWingCore.Instance.GameTime.TotalRealTime;
        }

        public override void LoadContent()
        {
            var gfx = AssaultWingCore.Instance.GraphicsDevice;
            _spriteBatch = new SpriteBatch(AssaultWingCore.Instance.GraphicsDevice);
            _vertexDeclaration = new VertexDeclaration(gfx, VertexPositionColor.VertexElements); 
            _effect = new BasicEffect(gfx, null);
            _effect.FogEnabled = false;
            _effect.LightingEnabled = false;
            _effect.View = Matrix.CreateLookAt(100 * Vector3.UnitZ, Vector3.Zero, Vector3.Up);
            _effect.World = Matrix.Identity;
            _effect.VertexColorEnabled = true;
            _effect.TextureEnabled = false;
            _backgroundTexture = AssaultWingCore.Instance.Content.Load<Texture2D>("menu_rustywall_bg");
            _smallFont = AssaultWingCore.Instance.Content.Load<SpriteFont>("MenuFontSmall");

            // Propagate LoadContent to other menu components that are known to
            // contain references to graphics content.
            MenuContent.LoadContent();
            foreach (var component in _components) component.LoadContent();
            AssaultWingCore.Instance.DataEngine.ProgressBar.LoadContent();

            base.LoadContent();
        }

        public override void UnloadContent()
        {
            if (_spriteBatch != null)
            {
                _spriteBatch.Dispose();
                _spriteBatch = null;
            }
            if (_vertexDeclaration != null)
            {
                _vertexDeclaration.Dispose();
                _vertexDeclaration = null;
            }
            if (_effect != null)
            {
                _effect.Dispose();
                _effect = null;
            }
            // The textures and font we reference will be disposed by GraphicsEngine.

            // Propagate LoadContent to other menu components that are known to
            // contain references to graphics content.
            foreach (MenuComponent component in _components)
                if (component != null) component.UnloadContent();
            AssaultWingCore.Instance.DataEngine.ProgressBar.UnloadContent();

            base.UnloadContent();
        }

        public override void Initialize()
        {
            _screenWidth = AssaultWingCore.Instance.ClientBounds.Width;
            _screenHeight = AssaultWingCore.Instance.ClientBounds.Height;

            _components[(int)MenuComponentType.Main] = new MainMenuComponent(this);
            _components[(int)MenuComponentType.Equip] = new EquipMenuComponent(this);
            _components[(int)MenuComponentType.Arena] = new ArenaMenuComponent(this);
            _viewCurve = new MovementCurve(_components[(int)MenuComponentType.Main].Center);

            WindowResize();
            base.Initialize();
        }

        public void Activate()
        {
            ActivateComponent(MenuComponentType.Main);
            AssaultWingCore.Instance.SoundEngine.PlayMusic("menu music", 1);
        }

        public void Deactivate()
        {
            AssaultWingCore.Instance.SoundEngine.StopMusic(TimeSpan.FromSeconds(2));
            _components[(int)_activeComponent].Active = false;
        }

        /// <summary>
        /// Activates a menu component after deactivating the previously active menu component
        /// and moving the menu view to the new component.
        /// </summary>
        /// <param name="component">The menu component to activate and move menu view to.</param>
        public void ActivateComponent(MenuComponentType component)
        {
            _components[(int)_activeComponent].Active = false;
            _activeComponent = component;
            MenuComponent newComponent = _components[(int)_activeComponent];

            // Make menu view scroll to the new component's position.
            _viewTarget = newComponent.Center;
            float duration = RandomHelper.GetRandomFloat(0.9f, 1.1f);
            _viewCurve.SetTarget(_viewTarget, AssaultWingCore.Instance.GameTime.TotalRealTime, duration,
                MovementCurve.Curvature.FastSlow);

            if (_menuChangeSound != null)
            {
                _menuChangeSound.Stop();
                _menuChangeSound.Dispose();
            }
            _menuChangeSound = AssaultWingCore.Instance.SoundEngine.PlaySound("MenuChangeStart");

            // The new component will be activated in 'Update()' when the view is closer to its center.
            _activeComponentSoundPlayedOnce = _activeComponentActivatedOnce = false;
        }

        /// <summary>
        /// Performs an action asynchronously, visualising progress with the progress bar.
        /// After calling this method, call <c>ProgressBar.SetSubtaskCount(int)</c> with 
        /// a suitable value.
        /// </summary>
        /// This method is provided as a helpful service for menu components.
        /// <param name="asyncAction">The action to perform asynchronously.</param>
        /// <param name="finishAction">Action to perform synchronously
        /// when the asynchronous action completes.</param>
        public void ProgressBarAction(Action asyncAction, Action finishAction)
        {
            var data = AssaultWingCore.Instance.DataEngine;
            this._finishAction = finishAction;
            IsHelpTextVisible = false;
            IsProgressBarVisible = true;
            data.ProgressBar.Task = asyncAction;
            data.ProgressBar.SetSubtaskCount(10); // just something until someone else sets the real value
            data.ProgressBar.StartTask();
        }

        public override void Update()
        {
            if (IsProgressBarVisible && AssaultWingCore.Instance.DataEngine.ProgressBar.TaskCompleted)
            {
                AssaultWingCore.Instance.DataEngine.ProgressBar.FinishTask();
                IsProgressBarVisible = false;
                IsHelpTextVisible = true;
                _finishAction();
            }
            _view = _viewCurve.Evaluate(AssaultWingCore.Instance.GameTime.TotalRealTime);

            // Activate 'activeComponent' if the view has just come close enough to its center.
            if (!_activeComponentActivatedOnce && Vector2.Distance(_view, _viewTarget) < 200)
            {
                _activeComponentActivatedOnce = true;
                _components[(int)_activeComponent].Active = true;
                IsProgressBarVisible = false;
                IsHelpTextVisible = true;
            }
            if (!_activeComponentSoundPlayedOnce && Vector2.Distance(_view, _viewTarget) < 1)
            {
                _activeComponentSoundPlayedOnce = true;
                if (_menuChangeSound != null) _menuChangeSound.Stop();
                AssaultWingCore.Instance.SoundEngine.PlaySound("MenuChangeEnd");
            }

            // Update menu components.
            foreach (MenuComponent component in _components)
                component.Update();
        }

        public override void Draw()
        {
            GraphicsDevice gfx = AssaultWingCore.Instance.GraphicsDevice;
            Viewport screen = gfx.Viewport;
            screen.X = 0;
            screen.Y = 0;
            screen.Width = _viewWidth;
            screen.Height = _viewHeight;

            // If client bounds are very small, render everything
            // to a separate render target of reasonable size, 
            // then scale the target to the screen.
            RenderTarget2D renderTarget = null;
            DepthStencilBuffer oldDepthStencilBuffer = null;
            if (_screenWidth < _viewWidth || _screenHeight < _viewHeight)
            {
                renderTarget = new RenderTarget2D(gfx, _viewWidth, _viewHeight, 1, gfx.DisplayMode.Format);

                // Set up graphics device.
                oldDepthStencilBuffer = gfx.DepthStencilBuffer;
                gfx.DepthStencilBuffer = null;

                // Set our own render target.
                gfx.SetRenderTarget(0, renderTarget);
            }

            // Begin drawing.
            gfx.Viewport = screen;
            gfx.Clear(ClearOptions.Target, Color.DimGray, 0, 0);
            _spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);

            // Draw background looping all over the screen.
            float yStart = _view.Y < 0
                ? -(_view.Y % _backgroundTexture.Height) - _backgroundTexture.Height
                : -(_view.Y % _backgroundTexture.Height);
            float xStart = _view.X < 0
                ? -(_view.X % _backgroundTexture.Width) - _backgroundTexture.Width
                : -(_view.X % _backgroundTexture.Width);
            for (float y = yStart; y < screen.Height; y += _backgroundTexture.Height)
                for (float x = xStart; x < screen.Width; x += _backgroundTexture.Width)
                    _spriteBatch.Draw(_backgroundTexture, new Vector2(x, y), Color.White);

            // Draw menu components.
            foreach (MenuComponent component in _components)
                component.Draw(_view - new Vector2(_viewWidth, _viewHeight) / 2, _spriteBatch);

            _spriteBatch.End();

            // Draw menu focus shadow.
            gfx.VertexDeclaration = _vertexDeclaration;
            _effect.Projection = Matrix.CreateOrthographicOffCenter(
                -screen.Width / 2, screen.Width / 2,
                -screen.Height, 0,
                1, 500);
            _effect.Begin();
            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Begin();
                gfx.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.TriangleList,
                    _shadowVertexData, 0, _shadowVertexData.Length,
                    _shadowIndexData, 0, _shadowIndexData.Length / 3);
                pass.End();
            }
            _effect.End();

            // Draw progress bar if wanted.
            if (_showProgressBar)
                AssaultWingCore.Instance.DataEngine.ProgressBar.Draw(_spriteBatch);

            // Draw static text.
            _spriteBatch.Begin();
            _spriteBatch.DrawString(_smallFont, "4th Milestone 2010-05-15",
                new Vector2(10, _viewHeight - _smallFont.LineSpacing), Color.White);
            if (_showHelpText)
            {
                Vector2 helpTextPos = new Vector2(
                    (int)(((float)_viewWidth - _smallFont.MeasureString(_helpText).X) / 2),
                    _viewHeight - _smallFont.LineSpacing);
                _spriteBatch.DrawString(_smallFont, _helpText, helpTextPos, Color.White);
            }
            string copyrightText = "Studfarm Studios";
            Vector2 copyrightTextPos = new Vector2(
                _viewWidth - (int)_smallFont.MeasureString(copyrightText).X - 10,
                _viewHeight - _smallFont.LineSpacing);
            _spriteBatch.DrawString(_smallFont, copyrightText, copyrightTextPos, Color.White);
            _spriteBatch.End();

            // If we're stretching the view, take the temporary render target
            // and draw its contents to the screen.
            if (_screenWidth < _viewWidth || _screenHeight < _viewHeight)
            {
                // Restore render target so what we can extract drawn pixels.
                gfx.SetRenderTarget(0, null);
                Texture2D renderTexture = renderTarget.GetTexture();

                // Restore the graphics device to the real backbuffer.
                screen.Width = _screenWidth;
                screen.Height = _screenHeight;
                gfx.Viewport = screen;
                gfx.DepthStencilBuffer = oldDepthStencilBuffer;

                // Draw the texture stretched on the screen.
                Rectangle destination = new Rectangle(0, 0, screen.Width, screen.Height);
                _spriteBatch.Begin();
                _spriteBatch.Draw(renderTexture, destination, Color.White);
                _spriteBatch.End();

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
            _screenWidth = AssaultWingCore.Instance.ClientBounds.Width;
            _screenHeight = AssaultWingCore.Instance.ClientBounds.Height;

            int oldViewWidth = _viewWidth;
            int oldViewHeight = _viewHeight;
            SetViewDimensions();
            InitializeShadow();
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
            _viewWidth = AssaultWingCore.Instance.ClientBounds.Width;
            _viewHeight = AssaultWingCore.Instance.ClientBounds.Height;

            if (_screenWidth == 0 || _screenHeight == 0)
                return;

            int screenWidthMin = 1; // least screen width that doesn't require scaling
            int screenHeightMin = 1; // least screen height that doesn't require scaling
            if (_screenWidth < screenWidthMin)
            {
                _viewWidth = screenWidthMin;
                _viewHeight = _viewWidth * _screenHeight / _screenWidth;
            }
            if (_screenHeight < screenHeightMin)
            {
                // Follow the stronger scale if there is a limitation both by width and by height.
                if (_viewHeight < screenHeightMin)
                {
                    _viewHeight = screenHeightMin;
                    _viewWidth = _viewHeight * _screenWidth / _screenHeight;
                }
            }
        }

        /// <summary>
        /// Initialises fields <c>shadowVertexData</c> and <c>shadowIndexData</c>
        /// to a shadow 3D model that fills the current client bounds.
        /// </summary>
        private void InitializeShadow()
        {
            // The shadow is a rectangle that spans a grid of vertices, each
            // of them black but with different levels of alpha.
            // The origin of the shadow 3D model is at the top center.
            Vector2 shadowDimensions = new Vector2(_viewWidth, _viewHeight);
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
            _shadowVertexData = vertexData.ToArray();
            _shadowIndexData = indexData.ToArray();
        }
    }
}
