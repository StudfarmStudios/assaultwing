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
        /// The game is active and playing.
        /// </summary>
        Gameplay,

        /// <summary>
        /// The game is active but stopped.
        /// </summary>
        GameplayStopped,

        /// <summary>
        /// The menu is active.
        /// </summary>
        Menu,

        /// <summary>
        /// The game and the menu is active but only the menu is visible.
        /// </summary>
        GameAndMenu,

        /// <summary>
        /// The game instance is in dedicated server mode and is initialising.
        /// </summary>
        InitializingDedicatedServer,

        /// <summary>
        /// The game instance is in dedicated server mode and is running.
        /// </summary>
        DedicatedServer,
    }
}
