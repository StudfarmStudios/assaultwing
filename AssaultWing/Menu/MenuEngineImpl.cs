using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Core.OverlayComponents;
using AW2.Game.Players;
using AW2.Graphics;
using AW2.Helpers;
using AW2.Sound;
using AW2.Stats;
using System.Linq;

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

        private MenuComponentType _activeComponentType;
        private MenuComponent[] _components;
        private bool _activeComponentActivatedOnce, _activeComponentSoundPlayedOnce;
        private Vector2 _view; // center of menu view in menu system coordinates
        private Vector2 _viewTarget;
        private MovementCurve _viewCurve;
        private SoundInstance _menuChangeSound;
        private TimeSpan _cursorFadeStartTime;
        private TimeSpan _loggedInPlayerAnimationStartTime;

        // The menu system draws a shadow on the screen as this transparent 3D object.
        private VertexPositionColor[] _shadowVertexData;
        private short[] _shadowIndexData; // stored as a triangle list
        private BasicEffect _effect;
        private Point _shadowSize;

        private SpriteBatch _spriteBatch;
        private Texture2D _backgroundTexture;
        private Texture2D _loggedInPilot;

        public new AssaultWing<ClientEvent> Game { get { return (AssaultWing<ClientEvent>)base.Game; } }
        public MenuContent MenuContent { get; private set; }
        public ProgressBar ProgressBar { get; private set; }
        public MenuControls Controls { get; private set; }

        private int ViewportWidth { get { return Game.GraphicsDeviceService.GraphicsDevice.Viewport.Width; } }
        private int ViewportHeight { get { return Game.GraphicsDeviceService.GraphicsDevice.Viewport.Height; } }
        private bool IsHelpTextVisible { get { return ProgressBar.IsFinished; } }
        private MenuComponent ActiveComponent { get { return _components[(int)_activeComponentType]; } }
        public MainMenuComponent MainMenu { get { return (MainMenuComponent)_components[(int)MenuComponentType.Main]; } }
        public EquipMenuComponent EquipMenu { get { return (EquipMenuComponent)_components[(int)MenuComponentType.Equip]; } }
        public ArenaMenuComponent ArenaMenu { get { return (ArenaMenuComponent)_components[(int)MenuComponentType.Arena]; } }

        private static Curve g_loggedInPilot;

        static MenuEngineImpl()
        {
            g_cursorFade = new Curve();
            g_cursorFade.Keys.Add(new CurveKey(0, 1, 0, 0, CurveContinuity.Step));
            g_cursorFade.Keys.Add(new CurveKey(0.5f, 0, 0, 0, CurveContinuity.Step));
            g_cursorFade.Keys.Add(new CurveKey(1, 1, 0, 0, CurveContinuity.Step));
            g_cursorFade.PreLoop = CurveLoopType.Cycle;
            g_cursorFade.PostLoop = CurveLoopType.Cycle;

            g_loggedInPilot = new Curve();
            g_loggedInPilot.Keys.Add(new CurveKey(0, 0, 0, 0, CurveContinuity.Smooth));
            g_loggedInPilot.Keys.Add(new CurveKey(1.7f, 1, 0, 0, CurveContinuity.Smooth));
        }

        public MenuEngineImpl(AssaultWing<ClientEvent> game, int updateOrder)
            : base(game, updateOrder)
        {
            Controls = new MenuControls();
            MenuContent = new MenuContent();
            ProgressBar = new ProgressBar(this)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                CustomAlignment = () => new Vector2(0, -2),
            };
        }

        public float GetCursorFade()
        {
            float cursorTime = (float)(Game.GameTime.TotalRealTime - _cursorFadeStartTime).TotalSeconds;
            return g_cursorFade.Evaluate(cursorTime);
        }

        public void ResetCursorFade()
        {
            _cursorFadeStartTime = Game.GameTime.TotalRealTime;
        }

        public float GetLoggedInPlayerAnimationMultiplier()
        {
            var animationTime = (float)(Game.GameTime.TotalRealTime - _loggedInPlayerAnimationStartTime).TotalSeconds;
            return g_loggedInPilot.Evaluate(animationTime);
        }

        public override void LoadContent()
        {
            var gfx = Game.GraphicsDeviceService.GraphicsDevice;
            _spriteBatch = new SpriteBatch(Game.GraphicsDeviceService.GraphicsDevice);
            _effect = new BasicEffect(gfx);
            _effect.FogEnabled = false;
            _effect.LightingEnabled = false;
            _effect.View = Matrix.CreateLookAt(100 * Vector3.UnitZ, Vector3.Zero, Vector3.Up);
            _effect.World = Matrix.Identity;
            _effect.VertexColorEnabled = true;
            _effect.TextureEnabled = false;
            _backgroundTexture = Game.Content.Load<Texture2D>("menu_rustywall_bg");
            _loggedInPilot = Game.Content.Load<Texture2D>("logged_in_pilot_bg");

            // Propagate LoadContent to other menu components that are known to
            // contain references to graphics content.
            MenuContent.LoadContent();
            foreach (var component in _components) component.LoadContent();
        }

        public override void UnloadContent()
        {
            if (_spriteBatch != null) _spriteBatch.Dispose();
            _spriteBatch = null;
            if (_effect != null) _effect.Dispose();
            _effect = null;
            // The textures and font we reference will be disposed by GraphicsEngine.

            // Propagate LoadContent to other menu components that are known to
            // contain references to graphics content.
            if (_components != null)
            {
                foreach (var component in _components)
                    if (component != null) component.UnloadContent();
            }
        }

        public override void Initialize()
        {
            _components = new MenuComponent[Enum.GetValues(typeof(MenuComponentType)).Length];
            _components[(int)MenuComponentType.Dummy] = new DummyMenuComponent(this);
            _components[(int)MenuComponentType.Main] = new MainMenuComponent(this);
            _components[(int)MenuComponentType.Equip] = new EquipMenuComponent(this);
            _components[(int)MenuComponentType.Arena] = new ArenaMenuComponent(this);
            _viewCurve = new MovementCurve(_components[(int)MenuComponentType.Main].Center);
            base.Initialize();
        }

        /// <summary>
        /// Activates a menu component after deactivating the previously active menu component
        /// and moving the menu view to the new component.
        /// </summary>
        /// <param name="component">The menu component to activate and move menu view to.</param>
        public void Activate(MenuComponentType component)
        {
            if (component == _activeComponentType && ActiveComponent.Active) return;
            ActiveComponent.Active = false;
            _activeComponentType = component;
            var newComponent = ActiveComponent;

            // Make menu view scroll to the new component's position.
            _viewTarget = newComponent.Center;
            float duration = RandomHelper.GetRandomFloat(0.9f, 1.1f);
            _viewCurve.SetTarget(_viewTarget, Game.GameTime.TotalRealTime, duration, MovementCurve.Curvature.FastSlow);

            if (_menuChangeSound != null)
            {
                _menuChangeSound.Stop();
                _menuChangeSound.Dispose();
            }
            _menuChangeSound = Game.SoundEngine.PlaySound("menuChangeStart");

            // The new component will be activated in 'Update()' when the view is closer to its center.
            _activeComponentSoundPlayedOnce = _activeComponentActivatedOnce = false;
        }

        public void Deactivate()
        {
            ActiveComponent.Active = false;
        }

        /// <summary>
        /// Deactivates all menu components except main menu.
        /// </summary>
        public void DeactivateComponentsExceptMainMenu()
        {
            foreach (int component in Enum.GetValues(typeof(MenuComponentType)))
                if ((MenuComponentType)component != MenuComponentType.Main)
                    _components[component].Active = false;
        }

        public override void Update()
        {
            _view = _viewCurve.Evaluate(Game.GameTime.TotalRealTime);

            // Activate 'activeComponent' if the view has just come close enough to its center.
            if (!_activeComponentActivatedOnce && Vector2.Distance(_view, _viewTarget) < 200)
            {
                _activeComponentActivatedOnce = true;
                ActiveComponent.Active = true;
            }
            if (!_activeComponentSoundPlayedOnce && Vector2.Distance(_view, _viewTarget) < 1)
            {
                _activeComponentSoundPlayedOnce = true;
                _menuChangeSound.Stop();
                Game.SoundEngine.PlaySound("menuChangeEnd");
            }

            foreach (var component in _components) component.Update();
        }

        public override void Draw()
        {
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            DrawBackground();
            DrawMenuComponents();
            _spriteBatch.End();
            DrawShadow();
            if (!ProgressBar.IsFinished) ProgressBar.Draw(_spriteBatch);
            DrawStaticText();
            DrawLoggedInPilot();
        }

        /// <summary>
        /// Initialises fields <c>shadowVertexData</c> and <c>shadowIndexData</c>
        /// to a shadow 3D model that fills the current client bounds.
        /// </summary>
        private void InitializeShadow()
        {
            var alphaCurve = new Curve(); // value of alpha as a function of distance in pixels from shadow origin
            alphaCurve.Keys.Add(new CurveKey(0, 0));
            alphaCurve.Keys.Add(new CurveKey(300, 0.07f));
            alphaCurve.Keys.Add(new CurveKey(500, 0.32f));
            alphaCurve.Keys.Add(new CurveKey(700, 0.65f));
            alphaCurve.Keys.Add(new CurveKey(900, 0.91f));
            alphaCurve.Keys.Add(new CurveKey(1200, 0.97f));
            alphaCurve.PreLoop = CurveLoopType.Constant;
            alphaCurve.PostLoop = CurveLoopType.Constant;
            alphaCurve.ComputeTangents(CurveTangent.Smooth);

            _shadowSize = new Point(ViewportWidth, ViewportHeight);
            // The shadow is a rectangle that spans a grid of vertices, each
            // of them black but with different levels of alpha.
            // The origin of the shadow 3D model is at the view center.
            var shadowDimensions = new Vector2(ViewportWidth, ViewportHeight);
            int gridWidth = (int)shadowDimensions.X / 30;
            int gridHeight = (int)shadowDimensions.Y / 30;
            var vertexData = new List<VertexPositionColor>();
            var indexData = new List<short>();
            for (int y = 0; y < gridHeight; ++y)
                for (int x = 0; x < gridWidth; ++x)
                {
                    var posInShadow = shadowDimensions *
                        new Vector2((float)x / (gridWidth - 1) - 0.5f, (float)y / (gridHeight - 1) - 0.5f);
                    float curvePos = (posInShadow * new Vector2(1, 1.5f)).Length();
                    vertexData.Add(new VertexPositionColor(
                        new Vector3(posInShadow, 0),
                        Color.Multiply(Color.Black, alphaCurve.Evaluate(curvePos))));
                    if (y > 0)
                        checked
                        {
                            if (x > 0)
                            {
                                indexData.Add((short)(y * gridWidth + x));
                                indexData.Add((short)((y - 1) * gridWidth + x));
                                indexData.Add((short)(y * gridWidth + x - 1));
                            }
                            if (x < gridWidth - 1)
                            {
                                indexData.Add((short)(y * gridWidth + x));
                                indexData.Add((short)((y - 1) * gridWidth + x + 1));
                                indexData.Add((short)((y - 1) * gridWidth + x));
                            }
                        }
                }
            _shadowVertexData = vertexData.ToArray();
            _shadowIndexData = indexData.ToArray();
        }

        private void DrawBackground()
        {
            float yStart = _view.Y < 0
                ? -(_view.Y % _backgroundTexture.Height) - _backgroundTexture.Height
                : -(_view.Y % _backgroundTexture.Height);
            float xStart = _view.X < 0
                ? -(_view.X % _backgroundTexture.Width) - _backgroundTexture.Width
                : -(_view.X % _backgroundTexture.Width);
            for (float y = yStart; y < ViewportHeight; y += _backgroundTexture.Height)
                for (float x = xStart; x < ViewportWidth; x += _backgroundTexture.Width)
                    _spriteBatch.Draw(_backgroundTexture, new Vector2(x, y), Color.White);
        }

        private void DrawMenuComponents()
        {
            foreach (var component in _components)
                component.Draw(_view - new Vector2(ViewportWidth, ViewportHeight) / 2, _spriteBatch);
        }

        private void DrawShadow()
        {
            if (_shadowSize != new Point(ViewportWidth, ViewportHeight))
                InitializeShadow();
            if (_shadowIndexData.Length == 0) return; // happens if the view is very shallow
            _effect.Projection = Matrix.CreateOrthographicOffCenter(
                -ViewportWidth / 2, ViewportWidth / 2,
                -ViewportHeight / 2, ViewportHeight / 2,
                1, 500);
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Game.GraphicsDeviceService.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(
                    PrimitiveType.TriangleList,
                    _shadowVertexData, 0, _shadowVertexData.Length,
                    _shadowIndexData, 0, _shadowIndexData.Length / 3);
            }
        }

        private void DrawStaticText()
        {
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            var versionText = "Assault Wing " + MiscHelper.Version;
            _spriteBatch.DrawString(MenuContent.FontSmall, versionText,
                Vector2.Round(new Vector2(10, ViewportHeight - MenuContent.FontSmall.LineSpacing)), Color.White);
            if (IsHelpTextVisible)
            {
                var helpTextPos = new Vector2(
                    (int)(((float)ViewportWidth - MenuContent.FontSmall.MeasureString(ActiveComponent.HelpText).X) / 2),
                    ViewportHeight - MenuContent.FontSmall.LineSpacing);
                _spriteBatch.DrawString(MenuContent.FontSmall, ActiveComponent.HelpText, Vector2.Round(helpTextPos), Color.White);
            }
            var copyrightText = "Studfarm Studios";
            var copyrightTextPos = new Vector2(
                ViewportWidth - (int)MenuContent.FontSmall.MeasureString(copyrightText).X - 10,
                ViewportHeight - MenuContent.FontSmall.LineSpacing);
            _spriteBatch.DrawString(MenuContent.FontSmall, copyrightText, Vector2.Round(copyrightTextPos), Color.White);
            _spriteBatch.End();
        }

        private void DrawLoggedInPilot()
        {
            var localPlayer = Game.DataEngine.LocalPlayer;

            if (localPlayer is null)
            {
                return;
            }

            var ranking = Game.Components.OfType<LocalPilotRankingHandler>()?.FirstOrDefault()?.LocalPilotRanking ?? new PilotRanking();

            var playerRank = ranking.RankString;

            var playerRating = string.Format(CultureInfo.InvariantCulture, "rating {0}", ranking.Rating.ToString());

            var backgroundPos = new Vector2(ViewportWidth - _loggedInPilot.Width + 4, -_loggedInPilot.Height * (1 - GetLoggedInPlayerAnimationMultiplier()));

            var nameSize = MenuContent.FontSmall.MeasureString(localPlayer.Name);
            var rankSize = MenuContent.FontBig.MeasureString(playerRank);
            var ratingSize = MenuContent.FontSmall.MeasureString(playerRating);

            var namePos = backgroundPos + new Vector2((_loggedInPilot.Width - nameSize.X) / 2 + 12, 10);
            var rankPos = backgroundPos + new Vector2((_loggedInPilot.Width - rankSize.X) / 2 + 12, 25);
            var ratingPos = backgroundPos + new Vector2((_loggedInPilot.Width - ratingSize.X) / 2 + 12, 54);

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            _spriteBatch.Draw(_loggedInPilot, backgroundPos, Color.White);

            ModelRenderer.DrawBorderedText(_spriteBatch, MenuContent.FontSmall, localPlayer.Name, namePos, Color.White, 1, 1);

            ModelRenderer.DrawBorderedText(_spriteBatch, MenuContent.FontBig, playerRank, rankPos, Color.White, 1, 1);

            ModelRenderer.DrawBorderedText(_spriteBatch, MenuContent.FontSmall, playerRating, ratingPos, Color.White, 1, 1);

            _spriteBatch.End();
        }
    }
}
