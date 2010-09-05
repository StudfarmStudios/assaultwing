using System;

namespace AW2.Core
{
    /// <summary>
    /// <seealso cref="Microsoft.Xna.Framework.DrawableGameComponent"/>
    /// </summary>
    public abstract class AWGameComponent : IDisposable
    {
        public bool Enabled { get; set; }
        public bool Visible { get; set; }
        public int UpdateOrder { get; set; }

        public virtual void Initialize() { }
        public virtual void LoadContent() { }
        public virtual void UnloadContent() { }
        public virtual void Update() { }
        public virtual void Draw() { }
        public virtual void Dispose() { }
    }
}
