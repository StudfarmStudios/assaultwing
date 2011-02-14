using System;
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

        private MultiControl _controlUp, _controlDown, _controlSelect, _controlSelectLeft;
        private Control _controlBack;
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

        public override Vector2 Center { get { return _pos + new Vector2(700, 495); } }

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

        /// <summary>
        /// Sets up the menu component's controls based on players' current control setup.
        /// </summary>
        private void InitializeControls()
        {
            if (_controlUp != null) _controlUp.Dispose();
            if (_controlDown != null) _controlDown.Dispose();
            if (_controlSelect != null) _controlSelect.Dispose();
            if (_controlSelectLeft != null) _controlSelectLeft.Dispose();
            if (_controlBack != null) _controlBack.Dispose();

            _controlBack = new KeyboardKey(Keys.Escape);
            _controlUp = new MultiControl();
            _controlUp.Add(new KeyboardKey(Keys.Up));
            _controlDown = new MultiControl();
            _controlDown.Add(new KeyboardKey(Keys.Down));
            _controlSelect = new MultiControl();
            _controlSelect.Add(new KeyboardKey(Keys.Enter));
            _controlSelect.Add(new KeyboardKey(Keys.Right));
            _controlSelectLeft = new MultiControl();
            _controlSelectLeft.Add(new KeyboardKey(Keys.Left));

            foreach (var player in MenuEngine.Game.DataEngine.Spectators)
            {
                if (player.IsRemote) continue;
                _controlUp.Add(player.Controls.Thrust);
                _controlDown.Add(player.Controls.Down);
                _controlSelect.Add(player.Controls.Fire1);
                _controlSelect.Add(player.Controls.Right);
                _controlSelectLeft.Add(player.Controls.Left);
            }
        }
    }
}
