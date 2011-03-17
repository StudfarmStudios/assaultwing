using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Game;
using AW2.Game.Arenas;
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

        private Control _controlBack, _controlDone, _controlUp, _controlDown;
        private TriggeredCallbackCollection _controlCallbacks;
        private Vector2 _pos; // position of the component's background texture in menu system coordinates
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

        /// <summary>
        /// Index of first arena in the arena name list that is visible on screen.
        /// </summary>
        private int _arenaListStart;

        private List<ArenaInfo> ArenaInfos { get; set; }
        private Vector2 ArenaPreviewPos { get { return _pos + new Vector2(430, 232); } }
        private Vector2 InfoBoxPos { get { return ArenaPreviewPos + new Vector2(-3, 188); } }
        private Vector2 InfoBoxHeaderPos { get { return InfoBoxPos + new Vector2(20, 20); } }
        private Vector2 GetInfoBoxLinePos(int line)
        {
            var infoBoxLineHeight = new Vector2(0, 14);
            var infoBoxContentPos = InfoBoxHeaderPos + new Vector2(0, 38);
            return infoBoxContentPos + infoBoxLineHeight * line;
        }

        public override bool Active
        {
            set
            {
                base.Active = value;
                if (value)
                {
                    InitializeControls();
                    InitializeControlCallbacks();
                    ArenaInfos = MenuEngine.Game.DataEngine.GetTypeTemplates<Arena>().Select(a => a.Info).ToList();
                }
            }
        }

        /// <summary>
        /// The center of the menu component in menu system coordinates.
        /// </summary>
        /// This is a good place to center the menu view to when the menu component
        /// is to be seen well on the screen.
        public override Vector2 Center { get { return _pos + new Vector2(560, 475); } }

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
            _arenaListStart = _arenaListStart.Clamp(_currentArena - MENU_ITEM_COUNT + 1, _currentArena);
        }

        public override void Draw(Vector2 view, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_backgroundTexture, _pos - view, Color.White);
            if (!Active) return;

            // Draw arena list.
            var lineDeltaPos = new Vector2(0, 40);
            var firstArenaNamePos = _pos - view + new Vector2(147, 237);
            var firstArenaTagPos = _pos - view + new Vector2(283, 235);
            for (int i = 0; i < MENU_ITEM_COUNT && _arenaListStart + i < ArenaInfos.Count; ++i)
            {
                int arenaI = _arenaListStart + i;
                var arenaNamePos = firstArenaNamePos + i * lineDeltaPos;
                spriteBatch.DrawString(Content.FontSmall, ArenaInfos[arenaI].Name, arenaNamePos.Round(), Color.White);
                var arenaTagPos = firstArenaTagPos + i * lineDeltaPos;
                if (arenaI == _currentArena) spriteBatch.Draw(_tagTexture, arenaTagPos.Round(), Color.White);
            }

            // Draw condolences.
            if (ArenaInfos.Count == 0)
            {
                var condolencesPos = _pos - view + new Vector2(540, 297);
                spriteBatch.DrawString(Content.FontBig, "No arenas, can't play, sorry!", condolencesPos.Round(), Color.White);
            }

            // Draw cursor and highlight.
            var highlightPos = _pos - view + new Vector2(124, 223) + (_currentArena - _arenaListStart) * lineDeltaPos;
            var cursorPos = highlightPos + new Vector2(2, 1);
            var infoBoxColumnWidth = new Vector2(220, 0);
            var info = ArenaInfos[_currentArena];
            var previewName = MenuEngine.Game.Content.Exists<Texture2D>(info.PreviewName) ? info.PreviewName : "no_preview";
            var previewTexture = MenuEngine.Game.Content.Load<Texture2D>(previewName);
            spriteBatch.Draw(_highlightTexture, highlightPos, Color.White);
            spriteBatch.Draw(_cursorTexture, cursorPos, Color.Multiply(Color.White, _cursorFade.Evaluate((float)MenuEngine.Game.GameTime.TotalRealTime.TotalSeconds)));

            spriteBatch.Draw(previewTexture, ArenaPreviewPos - view, Color.White);
            spriteBatch.Draw(_infoBackgroundTexture, InfoBoxPos - view, Color.White);
            spriteBatch.DrawString(Content.FontBig, info.Name, (InfoBoxHeaderPos - view).Round(), Color.White);
            Action<string, string, int, Color> drawInfoLine = (item, value, line, valueColor) =>
            {
                var itemPos = GetInfoBoxLinePos(line) - view;
                var valuePos = itemPos + infoBoxColumnWidth - new Vector2(Content.FontSmall.MeasureString(value).X, 0);
                spriteBatch.DrawString(Content.FontSmall, item, itemPos.Round(), Color.White);
                spriteBatch.DrawString(Content.FontSmall, value, valuePos.Round(), valueColor);
            };
            drawInfoLine("Size", info.Size.ToString(), 0, ArenaInfo.GetColorForSize(info.Size));
            drawInfoLine("Ideal Players", info.IdealPlayers, 1, Color.YellowGreen);
            drawInfoLine("Bonus Amount", info.BonusAmount.ToString(), 2, ArenaInfo.GetColorForBonusAmount(info.BonusAmount));
            drawInfoLine("Docks", info.Docks, 3, Color.YellowGreen);
            drawInfoLine("Flight Easiness", info.FlightEasiness.ToString(), 4, ArenaInfo.GetColorForFlightEasiness(info.FlightEasiness));

            spriteBatch.DrawString(Content.FontSmall, info.InfoText, (GetInfoBoxLinePos(0) - view + infoBoxColumnWidth + new Vector2(16, 0)).Round(), new Color(218, 159, 33));
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
            MenuEngine.Game.SelectedArenaName = ArenaInfos[_currentArena].Name;
        }

        /// <summary>
        /// Sets up the menu component's controls based on players' current control setup.
        /// </summary>
        private void InitializeControls()
        {
            _controlDone = new KeyboardKey(Keys.Enter);
            _controlBack = new KeyboardKey(Keys.Escape);
            _controlUp = new KeyboardKey(Keys.Up);
            _controlDown = new KeyboardKey(Keys.Down);
        }
    }
}
