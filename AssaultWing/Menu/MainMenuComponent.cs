using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Core;
using AW2.Menu.Main;
using AW2.UI;
using AW2.Helpers;

namespace AW2.Menu
{
    /// <summary>
    /// The main menu component where the user can choose to go play, go setup, or go away.
    /// </summary>
    public class MainMenuComponent : MenuComponent
    {
        private MainMenuItemCollections _itemCollections;
        private MainMenuItemCollection _currentItems;

        /// <summary>
        /// Index of the currently active menu item.
        /// </summary>
        private int _currentItem = 0;

        private Control _controlUp, _controlDown, _controlSelect, _controlSelectLeft, _controlBack;
        private TriggeredCallbackCollection _commonCallbacks;
        private Vector2 _pos; // position of the component's background texture in menu system coordinates

        public override bool Active
        {
            set
            {
                base.Active = value;
                if (value)
                {
                    // Update our controls to players' possibly changed controls.
                    InitializeControls();
                    InitializeControlCallbacks();

                    SetItems(_itemCollections.StartItems);
                }
            }
        }

        public override Vector2 Center { get { return _pos + new Vector2(700, 455); } }

        private MainMenuItem CurrentItem
        {
            get
            {
                _currentItem = _currentItem.Clamp(0, _currentItems.Count - 1);
                return _currentItems[_currentItem];
            }
        }

        public MainMenuComponent(MenuEngineImpl menuEngine)
            : base(menuEngine)
        {
            _itemCollections = new MainMenuItemCollections(menuEngine);
            _pos = new Vector2(0, 698);
            SetItems(_itemCollections.StartItems);
        }

        public void SetItems(MainMenuItemCollection items)
        {
            _currentItems = items;
            _currentItem = 0;
        }

        public override void Update()
        {
            if (!Active) return;
            if (_currentItems != _itemCollections.NetworkItems && MenuEngine.Game.NetworkMode != NetworkMode.Standalone) throw new ApplicationException("Unexpected NetworkMode " + MenuEngine.Game.NetworkMode);
            _commonCallbacks.Update();
            foreach (var menuItem in _currentItems) menuItem.Update();
            _currentItems.Update();
        }

        public override void Draw(Vector2 view, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(MenuEngine.MenuContent.MainBackground, _pos - view, Color.White);
            CurrentItem.DrawHighlight(spriteBatch, _pos - view);
            for (int i = 0; i < _currentItems.Count; ++i)
                _currentItems[i].Draw(spriteBatch, _pos - view);
        }

        private void InitializeControlCallbacks()
        {
            _commonCallbacks = new TriggeredCallbackCollection
            {
                TriggeredCallback = MenuEngine.ResetCursorFade
            };
            _commonCallbacks.Callbacks.Add(new TriggeredCallback(_controlUp, () =>
            {
                if (_currentItem > 0) --_currentItem;
                MenuEngine.Game.SoundEngine.PlaySound("MenuBrowseItem");
            }));
            _commonCallbacks.Callbacks.Add(new TriggeredCallback(_controlDown, () =>
            {
                if (_currentItem < _currentItems.Count - 1) ++_currentItem;
                MenuEngine.Game.SoundEngine.PlaySound("MenuBrowseItem");
            }));
            _commonCallbacks.Callbacks.Add(new TriggeredCallback(_controlSelect, () => CurrentItem.Action(this)));
            _commonCallbacks.Callbacks.Add(new TriggeredCallback(_controlSelectLeft, () => CurrentItem.ActionLeft(this)));
            _commonCallbacks.Callbacks.Add(new TriggeredCallback(_controlBack, () =>
            {
                MenuEngine.Game.CutNetworkConnections();
                ApplyGraphicsSettings();
                ApplyPlayerControlsSettings();
                _currentItems = _itemCollections.StartItems;
            }));
        }

        private void ApplyGraphicsSettings()
        {
            var window = MenuEngine.Game.Window;
            var gfxSetup = MenuEngine.Game.Settings.Graphics;
            if (window.IsFullScreen &&
                !(window.ClientBounds.Width == gfxSetup.FullscreenWidth && window.ClientBounds.Height == gfxSetup.FullscreenHeight))
            {
                window.SetFullScreen(gfxSetup.FullscreenWidth, gfxSetup.FullscreenHeight);
            }
            if (gfxSetup.IsVerticalSynced && !window.IsVerticalSynced()) window.EnableVerticalSync();
            if (!gfxSetup.IsVerticalSynced && window.IsVerticalSynced()) window.DisableVerticalSync();
        }

        private void ApplyPlayerControlsSettings()
        {
            var players = MenuEngine.Game.DataEngine.Players;
            var controls = new[] { MenuEngine.Game.Settings.Controls.Player1, MenuEngine.Game.Settings.Controls.Player2 };
            players.Zip(controls, (plr, ctrls) => plr.Controls = PlayerControls.FromSettings(ctrls)).ToArray();
        }

        /// <summary>
        /// Sets up the menu component's controls based on players' current control setup.
        /// </summary>
        private void InitializeControls()
        {
            _controlBack = new KeyboardKey(Keys.Escape);
            _controlUp = new KeyboardKey(Keys.Up);
            _controlDown = new KeyboardKey(Keys.Down);
            _controlSelect = new MultiControl { new KeyboardKey(Keys.Enter), new KeyboardKey(Keys.Right) };
            _controlSelectLeft = new KeyboardKey(Keys.Left);
        }
    }
}
