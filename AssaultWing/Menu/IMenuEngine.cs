using System;
using Microsoft.Xna.Framework;
using AW2.Core;

namespace AW2.Menu
{
    /// <summary>
    /// A menu system consisting of menu components.
    /// </summary>
    public abstract class IMenuEngine : AWGameComponent
    {
        /// <summary>
        /// Activates the menu system.
        /// </summary>
        public abstract void Activate();

        /// <summary>
        /// Reacts to a system window resize.
        /// </summary>
        public abstract void WindowResize();

        public abstract void ProgressBarAction(Action asyncAction, Action finishAction);
        public abstract void Deactivate();
    }
}
