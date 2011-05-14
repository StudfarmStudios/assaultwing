using System;

namespace AW2.Core
{
    /// <summary>
    /// <seealso cref="Microsoft.Xna.Framework.DrawableGameComponent"/>
    /// </summary>
    public abstract class AWGameComponent : IDisposable
    {
        private bool _enabled, _visible;

        public AssaultWingCore Game { get; private set; }
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                var old = _enabled;
                _enabled = value;
                if (old != _enabled) EnabledOrVisibleChanged();
            }
        }
        public bool Visible
        {
            get { return _visible; }
            set
            {
                var old = _visible;
                _visible = value;
                if (old != _visible) EnabledOrVisibleChanged();
            }
        }
        public int UpdateOrder { get; private set; }

        public AWGameComponent(AssaultWingCore game, int updateOrder)
        {
            Game = game;
            UpdateOrder = updateOrder;
        }

        public virtual void Initialize() { }
        public virtual void LoadContent() { }
        public virtual void UnloadContent() { }
        public virtual void Update() { }
        public virtual void Draw() { }
        public virtual void Dispose() { }

        protected virtual void EnabledOrVisibleChanged() { }
    }
}
