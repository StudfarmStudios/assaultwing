using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Game;
using AW2.Graphics;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.UI;

namespace AW2.Menu
{
    /// <summary>
    /// The arena selection menu component where players can choose which arenas to play.
    /// </summary>
    public class ArenaMenuComponent : MenuComponent
    {
        private const int MENU_ITEM_COUNT = 8; // number of items that fit in the menu at once

        private Control _controlBack, _controlDone;
        private MultiControl _controlUp, _controlDown;
        private TriggeredCallbackCollection _controlCallbacks;
        private Vector2 _pos; // position of the component's background texture in menu system coordinates
        private SpriteFont _menuBigFont, _menuSmallFont;
        private Texture2D _backgroundTexture, _cursorTexture, _highlightTexture, _tagTexture, _infoBackgroundTexture;

        /// <summary>
        /// Cursor alpha fade curve as a function of time in seconds.
        /// </summary>
        private Curve _cursorFade;

        /// <summary>
        /// Time at which the cursor started fading.
        /// </summary>
        private TimeSpan _cursorFadeStartTime;

        /// <summary>
        /// Index of currently highlighted arena in the arena name list.
        /// </summary>
        private int _currentArena;
        private Arena _selectedArena;

        /// <summary>
        /// Index of first arena in the arena name list that is visible on screen.
        /// </summary>
        private int _arenaListStart;

        private List<ArenaMenuInfo> ArenaInfos { get { return MenuEngine.Game.DataEngine.ArenaInfos; } }

        public override bool Active
        {
            set
            {
                base.Active = value;
                if (value)
                {
                    InitializeControls();
                    InitializeControlCallbacks();
                }
            }
        }

        /// <summary>
        /// The center of the menu component in menu system coordinates.
        /// </summary>
        /// This is a good place to center the menu view to when the menu component
        /// is to be seen well on the screen.
        public override Vector2 Center { get { return _pos + new Vector2(560, 515); } }

        /// <summary>
        /// Creates an arena selection menu component for a menu system.
        /// </summary>
        /// <param name="menuEngine">The menu system.</param>
        public ArenaMenuComponent(MenuEngineImpl menuEngine)
            : base(menuEngine)
        {
            _pos = new Vector2(1220, 698);
            _cursorFade = new Curve();
            _cursorFade.Keys.Add(new CurveKey(0, 1, 0, 0, CurveContinuity.Step));
            _cursorFade.Keys.Add(new CurveKey(0.5f, 0, 0, 0, CurveContinuity.Step));
            _cursorFade.Keys.Add(new CurveKey(1, 1, 0, 0, CurveContinuity.Step));
            _cursorFade.PreLoop = CurveLoopType.Cycle;
            _cursorFade.PostLoop = CurveLoopType.Cycle;
            _currentArena = -1;
        }

        public override void LoadContent()
        {
            var content = MenuEngine.Game.Content;
            _menuBigFont = content.Load<SpriteFont>("MenuFontBig");
            _menuSmallFont = content.Load<SpriteFont>("MenuFontSmall");
            _backgroundTexture = content.Load<Texture2D>("menu_levels_bg");
            _cursorTexture = content.Load<Texture2D>("menu_levels_cursor");
            _highlightTexture = content.Load<Texture2D>("menu_levels_hilite");
            _tagTexture = content.Load<Texture2D>("menu_levels_tag");
            _infoBackgroundTexture = content.Load<Texture2D>("menu_levels_infobox");
        }

        public override void Update()
        {
            if (!Active) return;
            var oldCurrentArena = _currentArena;
            _controlCallbacks.Update();
            _currentArena = _currentArena.Clamp(0, ArenaInfos.Count - 1);
            if (oldCurrentArena != _currentArena)
                _selectedArena = (Arena)TypeLoader.LoadTemplate(ArenaInfos[_currentArena].FileName, typeof(Arena), typeof(TypeParameterAttribute));
            _arenaListStart = _arenaListStart.Clamp(_currentArena - MENU_ITEM_COUNT + 1, _currentArena);
        }

        public override void Draw(Vector2 view, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_backgroundTexture, _pos - view, Color.White);
            if (!Active) return;

            // Draw arena list.
            Vector2 lineDeltaPos = new Vector2(0, 40);
            Vector2 arenaNamePos = _pos - view + new Vector2(147, 237);
            Vector2 arenaTagPos = _pos - view + new Vector2(283, 235);
            for (int i = 0; i < MENU_ITEM_COUNT && _arenaListStart + i < ArenaInfos.Count; ++i)
            {
                int arenaI = _arenaListStart + i;
                spriteBatch.DrawString(_menuSmallFont, ArenaInfos[arenaI].Name, arenaNamePos + i * lineDeltaPos, Color.White);
                if (arenaI == _currentArena)
                    spriteBatch.Draw(_tagTexture, arenaTagPos + i * lineDeltaPos, Color.White);
            }

            // Draw condolences.
            if (ArenaInfos.Count == 0)
                spriteBatch.DrawString(_menuBigFont, "No arenas, can't play, sorry!",
                    _pos - view + new Vector2(540, 297), Color.White);

            // Draw cursor and highlight.
            Vector2 highlightPos = _pos - view + new Vector2(124, 223) + (_currentArena - _arenaListStart) * lineDeltaPos;
            Vector2 cursorPos = highlightPos + new Vector2(2, 1);
            Vector2 arenaPreviewPos = _pos-view + new Vector2(430, 232);
            Vector2 infoBoxPos = arenaPreviewPos + new Vector2(-3, 188);
            Vector2 infoBoxHeaderPos = infoBoxPos + new Vector2(20, 20);
            Vector2 infoBoxContentPos = infoBoxHeaderPos + new Vector2(0, 38);
            Vector2 infoBoxLineHeight = new Vector2(0, 14);
            Vector2 infoBoxColumnWidth = new Vector2(220, 0);
            ArenaMenuInfo menuInfo = _selectedArena.MenuInfo;
            var content = MenuEngine.Game.Content;
            string previewName = content.Exists<Texture2D>(menuInfo.PreviewName) ? menuInfo.PreviewName : "no_preview";
            var previewTexture = content.Load<Texture2D>(previewName);
            spriteBatch.Draw(_highlightTexture, highlightPos, Color.White);
            spriteBatch.Draw(_cursorTexture, cursorPos, Color.Multiply(Color.White, _cursorFade.Evaluate((float)MenuEngine.Game.GameTime.TotalRealTime.TotalSeconds)));

            spriteBatch.Draw(previewTexture, arenaPreviewPos, Color.White);
            spriteBatch.Draw(_infoBackgroundTexture, infoBoxPos, Color.White);
            spriteBatch.DrawString(_menuBigFont, menuInfo.Name, infoBoxHeaderPos, Color.White);
            
            spriteBatch.DrawString(_menuSmallFont, "Size", infoBoxContentPos, Color.White);
            spriteBatch.DrawString(_menuSmallFont, menuInfo.Size.ToString(), infoBoxContentPos + (infoBoxColumnWidth - new Vector2(_menuSmallFont.MeasureString(menuInfo.Size.ToString()).X, 0)), ArenaMenuInfo.GetColorForSize(menuInfo.Size));
            
            spriteBatch.DrawString(_menuSmallFont, "Ideal Players", infoBoxContentPos + (infoBoxLineHeight * 1), Color.White);
            spriteBatch.DrawString(_menuSmallFont, menuInfo.IdealPlayers, infoBoxContentPos + (infoBoxLineHeight * 1) + (infoBoxColumnWidth - new Vector2(_menuSmallFont.MeasureString(menuInfo.IdealPlayers).X, 0)), Color.YellowGreen);

            spriteBatch.DrawString(_menuSmallFont, "Bonus Amount", infoBoxContentPos + (infoBoxLineHeight * 2), Color.White);
            spriteBatch.DrawString(_menuSmallFont, menuInfo.BonusAmount.ToString(), infoBoxContentPos + (infoBoxLineHeight * 2) + (infoBoxColumnWidth - new Vector2(_menuSmallFont.MeasureString(menuInfo.BonusAmount.ToString()).X, 0)), ArenaMenuInfo.GetColorForBonusAmount(menuInfo.BonusAmount));
            
            spriteBatch.DrawString(_menuSmallFont, "Docks", infoBoxContentPos + (infoBoxLineHeight * 3), Color.White);
            spriteBatch.DrawString(_menuSmallFont, menuInfo.Docks, infoBoxContentPos + (infoBoxLineHeight * 3) + (infoBoxColumnWidth - new Vector2(_menuSmallFont.MeasureString(menuInfo.Docks).X, 0)), Color.YellowGreen);
            
            spriteBatch.DrawString(_menuSmallFont, "Flight Easiness", infoBoxContentPos + (infoBoxLineHeight * 4), Color.White);
            spriteBatch.DrawString(_menuSmallFont, menuInfo.FlightEasiness.ToString(), infoBoxContentPos + (infoBoxLineHeight * 4) + (infoBoxColumnWidth - new Vector2(_menuSmallFont.MeasureString(menuInfo.FlightEasiness.ToString()).X, 0)), ArenaMenuInfo.GetColorForFlightEasiness(menuInfo.FlightEasiness));

            spriteBatch.DrawString(_menuSmallFont, menuInfo.InfoText, infoBoxContentPos + infoBoxColumnWidth + new Vector2(16, 0), new Color(218, 159, 33));
        }

        private void InitializeControlCallbacks()
        {
            _controlCallbacks = new TriggeredCallbackCollection();
            _controlCallbacks.TriggeredCallback = () =>
            {
                _cursorFadeStartTime = MenuEngine.Game.GameTime.TotalRealTime;
            };
            _controlCallbacks.Callbacks.Add(new TriggeredCallback(_controlBack, () =>
            { 
                MenuEngine.ActivateComponent(MenuComponentType.Equip); 
            }));
           
            _controlCallbacks.Callbacks.Add(new TriggeredCallback(_controlDone, () =>
            {
                if (_currentArena >= 0 && _currentArena < ArenaInfos.Count)
                {
                    SelectCurrentArena();
                    MenuEngine.Game.SoundEngine.PlaySound("MenuChangeItem");
                    MenuEngine.ActivateComponent(MenuComponentType.Equip);
                }
            }));
            
            _controlCallbacks.Callbacks.Add(new TriggeredCallback(_controlUp, () =>
            {
                --_currentArena;
                MenuEngine.Game.SoundEngine.PlaySound("MenuBrowseItem");
                
            }));
            _controlCallbacks.Callbacks.Add(new TriggeredCallback(_controlDown, () =>
            {
                ++_currentArena;
                MenuEngine.Game.SoundEngine.PlaySound("MenuBrowseItem");
            }));
        }

        private void SelectCurrentArena()
        {
            MenuEngine.Game.DataEngine.SelectedArenaName = ArenaInfos[_currentArena].Name;
        }
        
        /// <summary>
        /// Sets up the menu component's controls based on players' current control setup.
        /// </summary>
        private void InitializeControls()
        {
            if (_controlDone != null) _controlDone.Dispose();
            if (_controlBack != null) _controlBack.Dispose();
            if (_controlUp != null) _controlUp.Dispose();
            if (_controlDown != null) _controlDown.Dispose();

            _controlDone = new KeyboardKey(Keys.Enter);
            _controlBack = new KeyboardKey(Keys.Escape);
            _controlUp = new MultiControl();
            _controlUp.Add(new KeyboardKey(Keys.Up));
            _controlDown = new MultiControl();
            _controlDown.Add(new KeyboardKey(Keys.Down));

            foreach (var player in MenuEngine.Game.DataEngine.Spectators)
            {
                _controlUp.Add(player.Controls.Thrust);
                _controlDown.Add(player.Controls.Down);
            }
        }
    }
}
