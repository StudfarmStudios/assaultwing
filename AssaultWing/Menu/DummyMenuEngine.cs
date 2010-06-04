using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Menu
{
    /// <summary>
    /// A dummy implementation of a menu engine.
    /// </summary>
    public class DummyMenuEngine : IMenuEngine
    {
        bool enabled;
        bool visible;
        int updateOrder;
        int drawOrder;

        #region IMenuEngine Members

        public bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value;
                if (EnabledChanged != null)
                    EnabledChanged(this, new EventArgs());
            }
        }

        public bool Visible
        {
            get { return visible; }
            set
            {
                visible = value;
                if (VisibleChanged != null)
                    VisibleChanged(this, new EventArgs());
            }
        }

        public void Activate() { }

        public void WindowResize() { }

        public void ProgressBarAction(Action asyncAction, Action finishAction) { }

        public void Deactivate() { }

        #endregion

        #region IDrawable Members

        public void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            Viewport screen = gfx.Viewport;
            screen.X = AssaultWing.Instance.ClientBounds.X;
            screen.Y = AssaultWing.Instance.ClientBounds.Y;
            screen.Width = AssaultWing.Instance.ClientBounds.Width;
            screen.Height = AssaultWing.Instance.ClientBounds.Height;
            gfx.Viewport = screen;
            AssaultWing.Instance.GraphicsDevice.Clear(Microsoft.Xna.Framework.Graphics.Color.DarkKhaki);
        }

        public int DrawOrder
        {
            get { return drawOrder; }
            set
            {
                drawOrder = value;
                if (DrawOrderChanged != null)
                    DrawOrderChanged(this, new EventArgs());
            }
        }

        public event EventHandler DrawOrderChanged;

        public event EventHandler VisibleChanged;

        #endregion

        #region IUpdateable Members

        public event EventHandler EnabledChanged;

        public void Update(Microsoft.Xna.Framework.GameTime gameTime) { }

        public event EventHandler UpdateOrderChanged;

        #endregion

        #region IMenuEngine Members

        public int UpdateOrder
        {
            get { return updateOrder; }
            set
            {
                updateOrder = value;
                if (UpdateOrderChanged != null)
                    UpdateOrderChanged(this, new EventArgs());
            }
        }

        #endregion

        #region IGameComponent Members

        public void Initialize() { }

        #endregion
    }
}
