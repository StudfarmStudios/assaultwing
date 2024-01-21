using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using AW2.Core;

namespace AW2.UI
{
    /// <summary>
    /// Basic user interface implementation.
    /// </summary>
    public class UIEngineImpl : AWGameComponent
    {
        private static readonly TimeSpan MOUSE_HIDE_DELAY = TimeSpan.FromSeconds(3);

        private InputState _oldState, _newState;
        private Stack<IEnumerable<Control>> _exclusiveControls;
        private TimeSpan _lastMouseActivity; // in real time

        /// <summary>
        /// If mouse input is being consumed for the purposes of using the mouse
        /// for game controls. Such consumption prevents other programs from using
        /// the mouse in any practical manner. Defaults to <b>false</b>.
        /// </summary>
        public bool MouseControlsEnabled { get; set; }
        public InputState InputState { get { return _newState; } }
        public InputState PreviousInputState { get { return _oldState; } }

        public UIEngineImpl(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
            _exclusiveControls = new Stack<IEnumerable<Control>>();
            UpdateInputState(); // to avoid null _oldState on first Update()
        }

        public override void Update()
        {
            UpdateInputState();
            UpdateMouse();
            UpdateControls();
        }

        public void PushExclusiveControls(IEnumerable<Control> controls)
        {
            _exclusiveControls.Push(controls);
        }

        public void PopExclusiveControls()
        {
            _exclusiveControls.Pop();
        }

        private void UpdateInputState()
        {
            _oldState = _newState;
            _newState = InputState.GetState();
        }

        private void UpdateMouse()
        {
            if (MouseControlsEnabled)
            {
                // Reset mouse cursor to the middle of the game window.
                var viewport = Game.GraphicsDeviceService.GraphicsDevice.Viewport;
                Mouse.SetPosition(viewport.Width / 2, viewport.Height / 2);
            }
            else
            {
                // Keep mouse cursor hidden if it hasn't moved for some time.
                if (_oldState.Mouse != _newState.Mouse) _lastMouseActivity = Game.GameTime.TotalRealTime;
                if (_lastMouseActivity + MOUSE_HIDE_DELAY < Game.GameTime.TotalRealTime)
                    Game.Window.Impl.EnsureCursorHidden();
                else
                    Game.Window.Impl.EnsureCursorShown();
            }
        }

        private void UpdateControls()
        {
            if (_exclusiveControls.Any())
            {
                Control.SetGlobalState(InputState.EMPTY, InputState.EMPTY);
                foreach (var control in _exclusiveControls.Peek())
                    control.SetLocalState(_oldState, _newState);
            }
            else
                Control.SetGlobalState(_oldState, _newState);
        }
    }
}
