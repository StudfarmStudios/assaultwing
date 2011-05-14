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
        /// <summary>
        /// The state of input controls in the previous frame.
        /// </summary>
        private InputState _oldState;

        /// <summary>
        /// True iff mouse input is eaten by the game.
        /// </summary>
        private bool _eatMouse;

        private Stack<IEnumerable<Control>> _exclusiveControls;

        /// <summary>
        /// If mouse input is being consumed for the purposes of using the mouse
        /// for game controls. Such consumption prevents other programs from using
        /// the mouse in any practical manner. Defaults to <b>false</b>.
        /// </summary>
        public bool MouseControlsEnabled { get { return _eatMouse; } set { _eatMouse = value; } }

        public UIEngineImpl(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
            _oldState = InputState.GetState();
            _eatMouse = false;
            _exclusiveControls = new Stack<IEnumerable<Control>>();
        }

        public override void Update()
        {
            if (_eatMouse)
            {
                // Reset mouse cursor to the middle of the game window.
                var viewport = Game.GraphicsDeviceService.GraphicsDevice.Viewport;
                Mouse.SetPosition(viewport.Width / 2, viewport.Height / 2);
            }

            var newState = InputState.GetState();
            if (_exclusiveControls.Any())
            {
                Control.SetGlobalState(InputState.EMPTY, InputState.EMPTY);
                foreach (var control in _exclusiveControls.Peek())
                    control.SetLocalState(_oldState, newState);
            }
            else
                Control.SetGlobalState(_oldState, newState);
            _oldState = newState;
        }

        public void PushExclusiveControls(IEnumerable<Control> controls)
        {
            _exclusiveControls.Push(controls);
        }

        public void PopExclusiveControls()
        {
            _exclusiveControls.Pop();
        }

        private string ExclusiveControlStackToString()
        {
            if (!_exclusiveControls.Any()) return "  <empty>";
            var str = new System.Text.StringBuilder();
            foreach (var controls in _exclusiveControls)
                str.Append("\n  { " + string.Join(", ", controls.Select(x => x.ToString())) + " }");
            return str.ToString();
        }
    }
}
