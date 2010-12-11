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
        /// The menu is active.
        /// </summary>
        Menu,

        /// <summary>
        /// The game and the menu is active but only the menu is visible.
        /// </summary>
        GameAndMenu,
    }
}
