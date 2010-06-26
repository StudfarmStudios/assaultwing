using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Core;
using AW2.UI;

namespace AW2.Menu
{
    /// <summary>
    /// The main menu component where the user can choose to go play, go setup, or go away.
    /// </summary>
    public class MainMenuComponent : MenuComponent
    {
        #region Fields

        private MainMenuItemCollections _itemCollections;
        private MainMenuItemCollection _currentItems;

        /// <summary>
        /// Index of the currently active menu item.
        /// </summary>
        private int _currentItem = 0;

        private MultiControl _controlUp, _controlDown, _controlSelect;
        private Control _controlBack;
        private TriggeredCallbackCollection _commonCallbacks;
        private Vector2 _pos; // position of the component's background texture in menu system coordinates

        #endregion Fields

        #region Properties

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
                    CutNetworkConnections();
                }
            }
        }

        public override Vector2 Center { get { return _pos + new Vector2(700, 495); } }

        #endregion Properties

        #region Constructor

        /// <summary>
        /// Creates a main menu component for a menu system.
        /// </summary>
        /// <param name="menuEngine">The menu system.</param>
        public MainMenuComponent(MenuEngineImpl menuEngine)
            : base(menuEngine)
        {
            _itemCollections = new MainMenuItemCollections(menuEngine);
            _pos = new Vector2(0, 698);
            SetItems(_itemCollections.StartItems);
        }

        #endregion Constructor

        #region Public methods

        public void SetItems(MainMenuItemCollection items)
        {
            _currentItems = items;
            _currentItem = 0;
        }

        public override void Update()
        {
            if (!Active) return;
            if (_currentItems != _itemCollections.NetworkItems && AssaultWing.Instance.NetworkMode != NetworkMode.Standalone) throw new ApplicationException("Unexpected NetworkMode " + AssaultWing.Instance.NetworkMode);
            _commonCallbacks.Update();
            foreach (var menuItem in _currentItems) menuItem.Update();
        }

        public override void Draw(Vector2 view, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(MenuEngine.MenuContent.MainBackground, _pos - view, Color.White);
            _currentItems[_currentItem].DrawHighlight(spriteBatch, _pos - view);
            for (int i = 0; i < _currentItems.Count; ++i)
                _currentItems[i].Draw(spriteBatch, _pos - view);
        }

        #endregion Public methods

        #region Private methods

        private void InitializeControlCallbacks()
        {
            _commonCallbacks = new TriggeredCallbackCollection();
            _commonCallbacks.TriggeredCallback = () =>
            {
                MenuEngine.ResetCursorFade();
            };
            _commonCallbacks.Callbacks.Add(new TriggeredCallback(_controlUp, () =>
            {
                if (_currentItem > 0)
                    --_currentItem;
                AssaultWing.Instance.SoundEngine.PlaySound("MenuBrowseItem");
            }));
            _commonCallbacks.Callbacks.Add(new TriggeredCallback(_controlDown, () =>
            {
                if (_currentItem < _currentItems.Count - 1)
                    ++_currentItem;
                AssaultWing.Instance.SoundEngine.PlaySound("MenuBrowseItem");
            }));
            _commonCallbacks.Callbacks.Add(new TriggeredCallback(_controlSelect, () =>
            {
                _currentItems[_currentItem].Action(this);
            }));
            _commonCallbacks.Callbacks.Add(new TriggeredCallback(_controlBack, () =>
            {
                CutNetworkConnections();
                _currentItems = _itemCollections.StartItems;
            }));
        }

        /// <summary>
        /// Sets up the menu component's controls based on players' current control setup.
        /// </summary>
        private void InitializeControls()
        {
            if (_controlUp != null) _controlUp.Dispose();
            if (_controlDown != null) _controlDown.Dispose();
            if (_controlSelect != null) _controlSelect.Dispose();
            if (_controlBack != null) _controlBack.Dispose();

            _controlBack = new KeyboardKey(Keys.Escape);
            _controlUp = new MultiControl();
            _controlUp.Add(new KeyboardKey(Keys.Up));
            _controlDown = new MultiControl();
            _controlDown.Add(new KeyboardKey(Keys.Down));
            _controlSelect = new MultiControl();
            _controlSelect.Add(new KeyboardKey(Keys.Enter));

            foreach (var player in AssaultWing.Instance.DataEngine.Spectators)
            {
                if (player.IsRemote) continue;
                _controlUp.Add(player.Controls.Thrust);
                _controlDown.Add(player.Controls.Down);
                _controlSelect.Add(player.Controls.Fire1);
            }
        }

        private static void CutNetworkConnections()
        {
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client)
                AssaultWing.Instance.StopClient();
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
                AssaultWing.Instance.StopServer();
        }

        #endregion Private methods
    }
}
