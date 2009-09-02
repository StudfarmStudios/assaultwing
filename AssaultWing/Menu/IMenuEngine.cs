using Microsoft.Xna.Framework;

namespace AW2.Menu
{
    /// <summary>
    /// A menu system consisting of menu components.
    /// </summary>
    public interface IMenuEngine : IDrawable, IUpdateable, IGameComponent
    {
        /// <summary>
        /// Indicates whether GameComponent.Update should be called when Game.Update is called.
        /// </summary>
        new bool Enabled { get; set; }

        /// <summary>
        /// Indicates whether Draw should be called.
        /// </summary>
        new bool Visible { get; set; }

        /// <summary>
        /// Indicates when the game component should be updated relative to other game components.
        /// Lower values are updated first.
        /// </summary>
        new int UpdateOrder { get; set; }

        /// <summary>
        /// Activates the menu system.
        /// </summary>
        void Activate();

        /// <summary>
        /// Reacts to a system window resize.
        /// </summary>
        void WindowResize();
    }
}
