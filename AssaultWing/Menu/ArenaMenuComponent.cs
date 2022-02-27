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

        private ScrollableList _currentArena;

        private MenuControls Controls { get { return MenuEngine.Controls; } }
        private ArenaInfo[] ArenaInfos { get; set; }
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
                    ArenaInfos =
                        (from arenaTemplate in MenuEngine.Game.DataEngine.GetTypeTemplates<Arena>()
                        join gameModeArena in MenuEngine.Game.DataEngine.GameplayMode.Arenas
                        on arenaTemplate.Info.Name equals gameModeArena
                        select arenaTemplate.Info).ToArray();
            }
        }

        public override Vector2 Center { get { return _pos + new Vector2(560, 475); } }
        public override string HelpText { get { return "Arrows select, Enter selects, Esc backs out"; } }

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
            _currentArena = new ScrollableList(MENU_ITEM_COUNT, () => ArenaInfos.Length);
            InitializeControlCallbacks();
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
            _controlCallbacks.Update();
        }

        public override void Draw(Vector2 view, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_backgroundTexture, _pos - view, Color.White);
            if (!Active) return;
            DrawArenaList(view, spriteBatch);
            DrawCurrentArenaInfo(view, spriteBatch);
        }

        private void DrawArenaList(Vector2 view, SpriteBatch spriteBatch)
        {
            var lineDeltaPos = new Vector2(0, 40);
            var firstArenaNamePos = _pos - view + new Vector2(147, 237);
            var firstArenaTagPos = _pos - view + new Vector2(283, 235);
            var scrollUpPos = _pos - view + new Vector2(172, 199);
            var scrollDownPos = _pos - view + new Vector2(172, 555);
            _currentArena.ForEachVisible((realIndex, visibleIndex, isSelected) =>
            {
                var arenaNamePos = firstArenaNamePos + visibleIndex * lineDeltaPos;
                spriteBatch.DrawString(Content.FontSmall, ArenaInfos[realIndex].Name, Vector2.Round(arenaNamePos), Color.White);
                if (isSelected)
                {
                    var arenaTagPos = firstArenaTagPos + visibleIndex * lineDeltaPos;
                    spriteBatch.Draw(_tagTexture, Vector2.Round(arenaTagPos), Color.White);

                    // Draw cursor and highlight.
                    var highlightPos = _pos - view + new Vector2(124, 223) + visibleIndex * lineDeltaPos;
                    var cursorPos = highlightPos + new Vector2(2, 1);
                    spriteBatch.Draw(_highlightTexture, highlightPos, Color.White);
                    spriteBatch.Draw(_cursorTexture, cursorPos, Color.Multiply(Color.White, _cursorFade.Evaluate((float)MenuEngine.Game.GameTime.TotalRealTime.TotalSeconds)));
                }
            });
            if (_currentArena.IsScrollableUp) spriteBatch.Draw(Content.ScrollUpTexture, scrollUpPos, Color.White);
            if (_currentArena.IsScrollableDown) spriteBatch.Draw(Content.ScrollDownTexture, scrollDownPos, Color.White);
        }

        private void DrawCurrentArenaInfo(Vector2 view, SpriteBatch spriteBatch)
        {
            var info = ArenaInfos[_currentArena.CurrentIndex];
            var infoBoxColumnWidth = new Vector2(220, 0);
            var previewName = MenuEngine.Game.Content.Exists<Texture2D>(info.PreviewName) ? info.PreviewName : "no_preview";
            var previewTexture = MenuEngine.Game.Content.Load<Texture2D>(previewName);
            spriteBatch.Draw(previewTexture, ArenaPreviewPos - view, Color.White);
            spriteBatch.Draw(_infoBackgroundTexture, InfoBoxPos - view, Color.White);
            spriteBatch.DrawString(Content.FontBig, info.Name, Vector2.Round(InfoBoxHeaderPos - view), Color.White);
            Action<string, string, int, Color> drawInfoLine = (item, value, line, valueColor) =>
            {
                var itemPos = GetInfoBoxLinePos(line) - view;
                var valuePos = itemPos + infoBoxColumnWidth - new Vector2(Content.FontSmall.MeasureString(value).X, 0);
                spriteBatch.DrawString(Content.FontSmall, item, Vector2.Round(itemPos), Color.White);
                spriteBatch.DrawString(Content.FontSmall, value, Vector2.Round(valuePos), valueColor);
            };
            drawInfoLine("Size", info.Size.ToString(), 0, ArenaInfo.GetColorForSize(info.Size));
            drawInfoLine("Ideal Players", info.IdealPlayers, 1, Color.YellowGreen);
            drawInfoLine("Bonus Amount", info.BonusAmount.ToString(), 2, ArenaInfo.GetColorForBonusAmount(info.BonusAmount));
            drawInfoLine("Docks", info.Docks, 3, Color.YellowGreen);
            drawInfoLine("Flight Easiness", info.FlightEasiness.ToString(), 4, ArenaInfo.GetColorForFlightEasiness(info.FlightEasiness));
            spriteBatch.DrawString(Content.FontSmall, info.InfoText, Vector2.Round(GetInfoBoxLinePos(0) - view + infoBoxColumnWidth + new Vector2(16, 0)), new Color(218, 159, 33));
        }

        private void InitializeControlCallbacks()
        {
            _controlCallbacks = new TriggeredCallbackCollection();
            _controlCallbacks.TriggeredCallback = () =>
            {
                _cursorFadeStartTime = MenuEngine.Game.GameTime.TotalRealTime;
            };
            _controlCallbacks.Callbacks.Add(new TriggeredCallback(Controls.Back, () =>
            {
                MenuEngine.Activate(MenuComponentType.Equip);
            }));
            _controlCallbacks.Callbacks.Add(new TriggeredCallback(Controls.Activate, () =>
            {
                if (_currentArena.IsCurrentValidIndex)
                {
                    SelectCurrentArena();
                    MenuEngine.Game.SoundEngine.PlaySound("MenuChangeItem");
                    MenuEngine.Activate(MenuComponentType.Equip);
                }
            }));
            _controlCallbacks.Callbacks.Add(new TriggeredCallback(Controls.Dirs.Up, () =>
            {
                _currentArena.CurrentIndex--;
                MenuEngine.Game.SoundEngine.PlaySound("MenuBrowseItem");

            }));
            _controlCallbacks.Callbacks.Add(new TriggeredCallback(Controls.Dirs.Down, () =>
            {
                _currentArena.CurrentIndex++;
                MenuEngine.Game.SoundEngine.PlaySound("MenuBrowseItem");
            }));
        }

        private void SelectCurrentArena()
        {
            MenuEngine.Game.SelectedArenaName = ArenaInfos[_currentArena.CurrentIndex].Name;
        }
    }
}
