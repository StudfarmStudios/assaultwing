using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Core;
using AW2.Graphics;
using AW2.Helpers;
using AW2.Menu.Main;
using AW2.UI;

namespace AW2.Menu
{
    /// <summary>
    /// The main menu component where the user can choose to go play, go setup, or go away.
    /// </summary>
    public class MainMenuComponent : MenuComponent
    {
        private const int MENU_ITEM_COUNT = 6; // number of items that fit in the menu at once

        /// <summary>
        /// Access only through <see cref="ItemCollections"/>.
        /// </summary>
        private MainMenuItemCollections _itemCollections;

        private Stack<Tuple<MainMenuItemCollection, ScrollableList>> _currentItemsHistory; // CurrentItems, CurrentItemIndexer
        private MainMenuItemCollection CurrentItems { get { return _currentItemsHistory.Any() ? _currentItemsHistory.Peek().Item1 : null; } }
        private ScrollableList CurrentItemIndexer { get { return _currentItemsHistory.Any() ? _currentItemsHistory.Peek().Item2 : null; } }

        private TriggeredCallbackCollection _commonCallbacks;
        private Vector2 _pos; // position of the component's background texture in menu system coordinates

        public override bool Active
        {
            set
            {
                base.Active = value;
                if (value)
                {
                    ResetItems();
                    MenuEngine.Game.Settings.ToFile();
                }
            }
        }

        public override Vector2 Center { get { return _pos + new Vector2(700, 455); } }
        public override string HelpText { get { return "Arrows move, Enter proceeds, Esc cancels"; } }

        public MainMenuItem CurrentItem { get { return CurrentItems[CurrentItemIndexer.CurrentIndex]; } }
        public bool IsActive(MainMenuItemCollection items) { return CurrentItems == items; }
        public MainMenuItemCollections ItemCollections
        {
            get
            {
                if (_itemCollections == null) _itemCollections = new MainMenuItemCollections(this);
                return _itemCollections;
            }
        }
        private MenuControls Controls { get { return MenuEngine.Controls; } }

        public MainMenuComponent(MenuEngineImpl menuEngine)
            : base(menuEngine)
        {
            _pos = new Vector2(0, 698);
            _currentItemsHistory = new Stack<Tuple<MainMenuItemCollection, ScrollableList>>();
            InitializeControlCallbacks();
        }

        public void PushItems(MainMenuItemCollection items)
        {
            _currentItemsHistory.Push(Tuple.Create(items, new ScrollableList(MENU_ITEM_COUNT, () => items.Count)));
        }

        private void PopItems()
        {
            if (_currentItemsHistory.Count == 1) return; // Already at top level.
            if (CurrentItems == _itemCollections.LoginItems || CurrentItems == _itemCollections.SetupItems)
                MenuEngine.Game.Settings.ToFile();
            if (_currentItemsHistory.Count == 2)
            {
                // Returning to top level.
                MenuEngine.Game.CutNetworkConnections();
                ApplyGraphicsSettings();
                ApplyControlsSettings();
            }
            _currentItemsHistory.Pop();
            MenuEngine.Game.SoundEngine.PlaySound("MenuChangeItem");
        }

        public override void Update()
        {
            if (!Active) return;
            MenuEngine.Game.WebData.LoginErrors.Do(queue =>
            {
                while (queue.Any()) MenuEngine.Game.ShowInfoDialog(queue.Dequeue());
            });
            if (CurrentItems != ItemCollections.NetworkItems && MenuEngine.Game.NetworkMode != NetworkMode.Standalone)
                throw new ApplicationException("Unexpected NetworkMode " + MenuEngine.Game.NetworkMode + " in " + CurrentItems.Name);
            _commonCallbacks.Update();
            foreach (var menuItem in CurrentItems) menuItem.Update();
            CurrentItems.Update();
        }

        public override void Draw(Vector2 view, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(MenuEngine.MenuContent.MainBackground, _pos - view, Color.White);
            if (!_currentItemsHistory.Any()) return;
            var titlePos = _pos - view + new Vector2(585, 320);
            var title = string.Join(" > ",
                (from items in _currentItemsHistory.Reverse()
                 let name = items.Item1.Name
                 where name != ""
                 select name).ToArray());
            spriteBatch.DrawString(MenuEngine.MenuContent.FontBig, title, Vector2.Round(titlePos), Color.LightGray);
            CurrentItemIndexer.ForEachVisible((realIndex, visibleIndex, isSelected) =>
            {
                if (isSelected) CurrentItems[realIndex].DrawHighlight(spriteBatch, _pos - view, visibleIndex);
                CurrentItems[realIndex].Draw(spriteBatch, _pos - view, visibleIndex);
            });
            if (CurrentItems == ItemCollections.NetworkItems)
            {
                DrawAdditionalMessageBox(view, spriteBatch);
                DrawPilotLoginStatus(view, spriteBatch);
                DrawScheduledBattleDisplay(view, spriteBatch);
            }
            var scrollUpPos = _pos - view + new Vector2(653, 260);
            var scrollDownPos = _pos - view + new Vector2(653, 580);
            if (CurrentItemIndexer.IsScrollableUp) spriteBatch.Draw(Content.ScrollUpTexture, scrollUpPos, Color.White);
            if (CurrentItemIndexer.IsScrollableDown) spriteBatch.Draw(Content.ScrollDownTexture, scrollDownPos, Color.White);
        }

        private void DrawAdditionalMessageBox(Vector2 view, SpriteBatch spriteBatch)
        {
            var backgroundPos = _pos - view + new Vector2(440, 600);
            spriteBatch.Draw(MenuEngine.MenuContent.SmallStatusPaneTexture, backgroundPos, Color.White);
        }

        private void DrawPilotLoginStatus(Vector2 view, SpriteBatch spriteBatch)
        {
            var ballPos = _pos - view + new Vector2(790, 355);
            var localPlayer = MenuEngine.Game.DataEngine.Players.FirstOrDefault(plr => plr.IsLocal);
            var statusBall = localPlayer != null && localPlayer.GetStats().IsLoggedIn
                ? MenuEngine.MenuContent.PlayerLoginStatusGreen
                : MenuEngine.MenuContent.PlayerLoginStatusRed;
            spriteBatch.Draw(statusBall, ballPos, Color.White);
        }

        private void DrawScheduledBattleDisplay(Vector2 view, SpriteBatch spriteBatch)
        {
            var backgroundPos = _pos - view + new Vector2(440, 600);
            var textStartPos = Vector2.Round(backgroundPos + new Vector2(50, 43));
            spriteBatch.DrawString(Content.FontBig, "Next Scheduled Game in:", textStartPos, Color.White);
            spriteBatch.DrawString(Content.FontSmall, "Everybody's Welcome to Join!\n\nYou can find all our Scheduled Games by selecting\n\"Find more in Forums\" in this menu.  (and you are of\ncourse free to play whenever you want)", textStartPos + new Vector2(0, 27), Color.White);
            var currentTime = DateTime.Now;
            var nextGame = MenuEngine.Game.WebData.NextScheduledGame;
            var text =
                !nextGame.HasValue || nextGame + TimeSpan.FromHours(2) <= currentTime ? "Not yet scheduled"
                : nextGame <= currentTime ? "Now! Join in!"
                : (nextGame.Value - currentTime).ToDurationString("d", "h", "min", null, usePlurals: false);
            if (text == "") text = "< 1 min";
            spriteBatch.DrawString(Content.FontBig, text, textStartPos + new Vector2(260, 6), Color.YellowGreen);
        }

        private void ResetItems()
        {
            _currentItemsHistory.Clear();
            PushItems(ItemCollections.StartItems);
        }

        private void InitializeControlCallbacks()
        {
            _commonCallbacks = new TriggeredCallbackCollection
            {
                TriggeredCallback = MenuEngine.ResetCursorFade
            };
            _commonCallbacks.Callbacks.Add(new TriggeredCallback(Controls.Dirs.Up, () =>
            {
                CurrentItemIndexer.CurrentIndex--;
                MenuEngine.Game.SoundEngine.PlaySound("MenuBrowseItem");
            }));
            _commonCallbacks.Callbacks.Add(new TriggeredCallback(Controls.Dirs.Down, () =>
            {
                CurrentItemIndexer.CurrentIndex++;
                MenuEngine.Game.SoundEngine.PlaySound("MenuBrowseItem");
            }));
            _commonCallbacks.Callbacks.Add(new TriggeredCallback(Controls.Activate, () => CurrentItem.Action()));
            _commonCallbacks.Callbacks.Add(new TriggeredCallback(Controls.Dirs.Left, () => CurrentItem.ActionLeft()));
            _commonCallbacks.Callbacks.Add(new TriggeredCallback(Controls.Dirs.Right, () => CurrentItem.ActionRight()));
            _commonCallbacks.Callbacks.Add(new TriggeredCallback(Controls.Back, PopItems));
        }

        private void ApplyGraphicsSettings()
        {
            var window = MenuEngine.Game.Window;
            var gfxSetup = MenuEngine.Game.Settings.Graphics;
            var clientBounds = window.Impl.GetClientBounds();
            if (window.Impl.GetFullScreen() &&
                !(clientBounds.Width == gfxSetup.FullscreenWidth && clientBounds.Height == gfxSetup.FullscreenHeight))
            {
                window.Impl.SetFullScreen(gfxSetup.FullscreenWidth, gfxSetup.FullscreenHeight);
            }
            if (gfxSetup.IsVerticalSynced && !window.Impl.IsVerticalSynced()) window.Impl.EnableVerticalSync();
            if (!gfxSetup.IsVerticalSynced && window.Impl.IsVerticalSynced()) window.Impl.DisableVerticalSync();
        }

        private void ApplyControlsSettings()
        {
            var players = MenuEngine.Game.DataEngine.Players;
            var controls = new[] { MenuEngine.Game.Settings.Controls.Player1, MenuEngine.Game.Settings.Controls.Player2 };
            players.Zip(controls, (plr, ctrls) => plr.Controls = PlayerControls.FromSettings(ctrls)).ToArray();
            MenuEngine.Game.ChatStartControl = MenuEngine.Game.Settings.Controls.Chat.GetControl();
        }
    }
}
