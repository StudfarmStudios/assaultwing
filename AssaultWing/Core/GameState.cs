namespace AW2.Core
{
    /// <summary>
    /// The state of the game.
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// The game is initialising.
        /// </summary>
        Initializing,

        /// <summary>
        /// Introductory animations are playing.
        /// </summary>
        Intro,

        /// <summary>
        /// The game is active.
        /// </summary>
        Gameplay,

        /// <summary>
        /// The game overlay dialog is visible, game is active but paused.
        /// </summary>
        OverlayDialog,

        /// <summary>
        /// The menu is active.
        /// </summary>
        Menu,
    }
}
