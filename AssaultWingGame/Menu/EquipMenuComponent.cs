using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game.GobUtils;
using AW2.Menu.Equip;

namespace AW2.Menu
{
    /// <summary>
    /// The equip menu component where players can choose their ships and weapons.
    /// </summary>
    /// The equip menu consists of four panes, one for each player.
    /// Each pane consists of a top that indicates the player and the main body where
    /// the menu content lies.
    /// Each pane, in its main mode, displays the player's selection of equipment.
    /// Each player can control their menu individually, and their current position
    /// in the menu main display is indicated by a cursor and a highlight.
    public class EquipMenuComponent : MenuComponent
    {
        private static Curve g_tabFade;
        private static Curve g_readyFade;

        private EquipMenuTab _tab;
        private List<EquipMenuTab> _tabs;
        private int _tabIndex;
        private bool _readyPressed;
        private TimeSpan _tabFadeStartTime;
        private TimeSpan _readyFadeStartTime;

        private Texture2D _backgroundTexture;
        private Texture2D _buttonReadyTexture, _buttonReadyHiliteTexture;

        public static Curve CursorFade { get; private set; }
        public EquipMenuControls Controls { get; private set; }
        public TimeSpan ListCursorFadeStartTime { get; set; }

        public override bool Active
        {
            set
            {
                base.Active = value;
                if (value) ResetEquipMenu();
            }
        }

        /// <summary>
        /// The center of the menu component in menu system coordinates.
        /// </summary>
        /// This is a good place to center the menu view to when the menu component
        /// is to be seen well on the screen.
        public override Vector2 Center { get { return Pos + new Vector2(750, 460); } }

        static EquipMenuComponent()
        {
            CursorFade = new Curve();
            CursorFade.Keys.Add(new CurveKey(0, 1f, 0, 0, CurveContinuity.Step));
            CursorFade.Keys.Add(new CurveKey(0.5f, 0, 0, 0, CurveContinuity.Step));
            CursorFade.Keys.Add(new CurveKey(1f, 1f, 0, 0, CurveContinuity.Step));
            CursorFade.PreLoop = CurveLoopType.Cycle;
            CursorFade.PostLoop = CurveLoopType.Cycle;
            g_tabFade = new Curve();
            g_tabFade.Keys.Add(new CurveKey(0f, 1f));
            g_tabFade.Keys.Add(new CurveKey(1f, 0.588f));
            g_tabFade.Keys.Add(new CurveKey(2.2f, 1f));
            g_tabFade.PreLoop = CurveLoopType.Cycle;
            g_tabFade.PostLoop = CurveLoopType.Cycle;
            g_readyFade = new Curve();
            g_readyFade.Keys.Add(new CurveKey(0f, 0.941f));
            g_readyFade.Keys.Add(new CurveKey(1f, 0.118f));
            g_readyFade.Keys.Add(new CurveKey(2f, 0.941f));
            g_readyFade.PreLoop = CurveLoopType.Cycle;
            g_readyFade.PostLoop = CurveLoopType.Cycle;
        }

        public EquipMenuComponent(MenuEngineImpl menuEngine)
            : base(menuEngine)
        {
            Controls = new EquipMenuControls();
            ListCursorFadeStartTime = MenuEngine.Game.GameTime.TotalRealTime;
            _tabFadeStartTime = MenuEngine.Game.GameTime.TotalRealTime;
            _readyFadeStartTime = MenuEngine.Game.GameTime.TotalRealTime;
            Pos = Vector2.Zero;
            ResetEquipMenu();
        }

        public override void LoadContent()
        {
            var content = MenuEngine.Game.Content;
            _backgroundTexture = content.Load<Texture2D>("menu_equip_bg");
            _buttonReadyTexture = content.Load<Texture2D>("menu_equip_btn_ready");
            _buttonReadyHiliteTexture = content.Load<Texture2D>("menu_equip_btn_ready_hilite");
        }

        public override void UnloadContent()
        {
            // The textures and fonts we reference will be disposed by GraphicsEngine.
        }

        public override void Update()
        {
            if (!Active) return;
            _tab.Update();
            CheckGeneralControls();
            CheckArenaStart();
        }

        private void CheckArenaStart()
        {
            bool okToStart = MenuEngine.Game.NetworkMode == NetworkMode.Client
                ? MenuEngine.Game.IsClientAllowedToStartArena && _readyPressed
                : _readyPressed;
            if (!okToStart) return;
            MenuEngine.Deactivate();
            if (MenuEngine.Game.NetworkMode == NetworkMode.Client)
                AssaultWingCore.Instance.StartArena(); // arena prepared in MainMenuItemCollections.HandleStartGameMessage
            else
                MenuEngine.ProgressBarAction(
                    () => MenuEngine.Game.PrepareArena(MenuEngine.Game.SelectedArenaName),
                    AssaultWingCore.Instance.StartArena);
        }

        private void ResetEquipMenu()
        {
            _tabs = new List<EquipMenuTab>();
            _tabs.Add(new EquipTab(this));
            _tabs.Add(new PlayersTab(this));
            if (MenuEngine.Game.NetworkMode != NetworkMode.Standalone) _tabs.Add(new ChatTab(this));
            _tabs.Add(new MatchTab(this));
            _tab = _tabs[_tabIndex = 0];
            _readyPressed = false;
        }

        private void CheckGeneralControls()
        {
            if (Controls.Tab.Pulse) ChangeTab();
            else if (Controls.Back.Pulse) BackToMainMenu();
            else if (Controls.StartGame.Pulse) _readyPressed = !_readyPressed;
        }

        private void ChangeTab()
        {
            _tabIndex = (_tabIndex + 1) % _tabs.Count;
            _tab = _tabs[_tabIndex];
            MenuEngine.Game.SoundEngine.PlaySound("MenuChangeItem");
            _tabFadeStartTime = MenuEngine.Game.GameTime.TotalRealTime;
        }

        private void BackToMainMenu()
        {
            MenuEngine.Game.CutNetworkConnections();
            MenuEngine.ActivateComponent(MenuComponentType.Main);
        }

        #region Drawing methods

        public override void Draw(Vector2 view, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_backgroundTexture, Pos - view, Color.White);
            DrawTabsAndButtons(view, spriteBatch);
            DrawStatusDisplay(view, spriteBatch);
            _tab.Draw(view, spriteBatch);
        }

        private void DrawTabsAndButtons(Vector2 view, SpriteBatch spriteBatch)
        {
            var firstTabPos = Pos - view + new Vector2(341, 123);
            var tabWidth = new Vector2(97, 0);
            var tabPos = firstTabPos;
            foreach (var tab in _tabs)
            {
                spriteBatch.Draw(tab.TabTexture, tabPos, Color.White);
                tabPos += tabWidth;
            }

            // Draw tab hilite
            float fadeTime = (float)(MenuEngine.Game.GameTime.TotalRealTime - _tabFadeStartTime).TotalSeconds;
            spriteBatch.Draw(Content.TabHilite, firstTabPos + tabWidth * _tabIndex, Color.Multiply(Color.White, g_tabFade.Evaluate(fadeTime)));

            // Draw ready button
            var readyButtonPos = firstTabPos + new Vector2(419, 0);
            spriteBatch.Draw(_buttonReadyTexture, readyButtonPos, Color.White);
            var highlightAlpha = _readyPressed
                ? 1f
                : g_readyFade.Evaluate((float)(MenuEngine.Game.GameTime.TotalRealTime - _readyFadeStartTime).TotalSeconds);
            spriteBatch.Draw(_buttonReadyHiliteTexture, readyButtonPos, Color.Multiply(Color.White, highlightAlpha));
        }

        private void DrawStatusDisplay(Vector2 view, SpriteBatch spriteBatch)
        {
            var data = MenuEngine.Game.DataEngine;

            // Setup positions for statusdisplay texts
            Vector2 statusDisplayTextPos = Pos - view + new Vector2(885, 618);
            Vector2 statusDisplayRowHeight = new Vector2(0, 12);
            Vector2 statusDisplayColumnWidth = new Vector2(75, 0);

            // Setup statusdisplay texts
            string statusDisplayPlayerAmount = "" + data.Players.Count();
            string statusDisplayArenaName = MenuEngine.Game.SelectedArenaName;
            string statusDisplayStatus = MenuEngine.Game.NetworkMode == NetworkMode.Server
                ? "server"
                : "connected";

            // Draw common statusdisplay texts for all modes
            spriteBatch.DrawString(Content.FontSmall, "Players", statusDisplayTextPos, Color.White);
            spriteBatch.DrawString(Content.FontSmall, statusDisplayPlayerAmount, statusDisplayTextPos + statusDisplayColumnWidth, Color.GreenYellow);
            spriteBatch.DrawString(Content.FontSmall, "Arena", statusDisplayTextPos + statusDisplayRowHeight * 4, Color.White);
            spriteBatch.DrawString(Content.FontSmall, statusDisplayArenaName, statusDisplayTextPos + statusDisplayRowHeight * 5, Color.GreenYellow);

            // Draw network game statusdisplay texts
            if (MenuEngine.Game.NetworkMode != NetworkMode.Standalone)
            {
                spriteBatch.DrawString(Content.FontSmall, "Status", statusDisplayTextPos + statusDisplayRowHeight, Color.White);
                spriteBatch.DrawString(Content.FontSmall, statusDisplayStatus, statusDisplayTextPos + statusDisplayColumnWidth + statusDisplayRowHeight, Color.GreenYellow);
            }

            // Draw client statusdisplay texts
            if (MenuEngine.Game.NetworkMode == NetworkMode.Client)
            {
                spriteBatch.DrawString(Content.FontSmall, "Ping", statusDisplayTextPos + statusDisplayRowHeight * 2, Color.White);
                var textAndColor = GetPingTextAndColor();
                spriteBatch.DrawString(Content.FontSmall, textAndColor.Item1, statusDisplayTextPos + statusDisplayColumnWidth + statusDisplayRowHeight * 2, textAndColor.Item2);
            }
        }

        private Tuple<string, Color> GetPingTextAndColor()
        {
            if (MenuEngine.Game.NetworkMode != NetworkMode.Client ||
                !MenuEngine.Game.NetworkEngine.IsConnectedToGameServer)
                return Tuple.Create("???", EquipInfo.GetColorForAmountType(EquipInfo.EquipInfoAmountType.Average));
            var ping = MenuEngine.Game.NetworkEngine.GameServerConnection.PingInfo.PingTime.TotalMilliseconds;
            if (ping < 35) return Tuple.Create("Excellent", EquipInfo.GetColorForAmountType(EquipInfo.EquipInfoAmountType.Great));
            if (ping < 70) return Tuple.Create("Good", EquipInfo.GetColorForAmountType(EquipInfo.EquipInfoAmountType.High));
            if (ping < 120) return Tuple.Create("Sufficient", EquipInfo.GetColorForAmountType(EquipInfo.EquipInfoAmountType.Average));
            if (ping < 200) return Tuple.Create("Poor", EquipInfo.GetColorForAmountType(EquipInfo.EquipInfoAmountType.Low));
            return Tuple.Create("Dreadful", EquipInfo.GetColorForAmountType(EquipInfo.EquipInfoAmountType.Poor));
        }

        #endregion Drawing methods
    }
}
