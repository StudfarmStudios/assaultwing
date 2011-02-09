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
        }

        public override void Update()
        {
            var newState = InputState.GetState();

            // Reset mouse cursor to the middle of the game window.
            if (_eatMouse)
                Microsoft.Xna.Framework.Input.Mouse.SetPosition(Game.GraphicsDeviceService.GraphicsDevice.Viewport.Width / 2, Game.GraphicsDeviceService.GraphicsDevice.Viewport.Height / 2);

            Control.SetState(ref _oldState, ref newState);
            _oldState = newState;
        }
    }
}
