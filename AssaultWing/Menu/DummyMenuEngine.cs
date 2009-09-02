using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AW2.Menu
{
    /// <summary>
    /// A dummy implementation of a menu engine.
    /// </summary>
    class DummyMenuEngine : IMenuEngine
    {
        #region IMenuEngine Members

        public bool Enabled
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                if (EnabledChanged != null)
                    EnabledChanged(this, new EventArgs());
                throw new NotImplementedException();
            }
        }

        public bool Visible
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                if (VisibleChanged != null)
                    VisibleChanged(this, new EventArgs());
                throw new NotImplementedException();
            }
        }

        public void Activate()
        {
            throw new NotImplementedException();
        }

        public void WindowResize()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IDrawable Members

        public void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            throw new NotImplementedException();
        }

        public int DrawOrder
        {
            get { throw new NotImplementedException(); }
            set
            {
                if (DrawOrderChanged != null)
                    DrawOrderChanged(this, new EventArgs());
                throw new NotImplementedException();
            }
        }

        public event EventHandler DrawOrderChanged;

        public event EventHandler VisibleChanged;

        #endregion

        #region IUpdateable Members

        public event EventHandler EnabledChanged;

        public void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            throw new NotImplementedException();
        }

        public event EventHandler UpdateOrderChanged;

        #endregion

        #region IMenuEngine Members

        public int UpdateOrder
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                if (UpdateOrderChanged != null)
                    UpdateOrderChanged(this, new EventArgs());
                throw new NotImplementedException();
            }
        }

        #endregion

        #region IGameComponent Members

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
