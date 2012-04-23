#region File Description
//-----------------------------------------------------------------------------
// GraphicsDeviceControl.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Drawing;
using System.Windows.Forms;

namespace AW2.Core
{
    /// <summary>
    /// Custom control uses the XNA Framework GraphicsDevice to render onto
    /// a Windows Form. Derived classes can override the Initialize and Draw
    /// methods to add their own drawing code.
    /// </summary>
    public class GraphicsDeviceControl : Control
    {
        /// <summary>
        /// The GraphicsDeviceService to use. Must be set after construction.
        /// </summary>
        public GraphicsDeviceService GraphicsDeviceService { get; set; }

        /// <summary>
        /// Getter of the size of drawable area. Must be set after construction.
        /// </summary>
        public Func<Size> GetClientSize { get; set; }

        public event Action<Message> ExternalWndProc;
        public event Action Draw;

        protected override void WndProc(ref Message m)
        {
            if (ExternalWndProc != null) ExternalWndProc(m);
            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var clientSize = GetClientSize();
            var beginDrawError = DesignMode
                ? Text + "\n\n" + GetType()
                : GraphicsDeviceService.BeginDraw(clientSize, false);
            if (beginDrawError == null)
            {
                if (Draw != null) Draw();
                GraphicsDeviceService.EndDraw(clientSize, Handle);
            }
            else
            {
                GraphicsDeviceService.PaintUsingSystemDrawing(e.Graphics, Font, ClientRectangle, beginDrawError);
            }
        }

        /// <summary>
        /// Ignores WinForms paint-background messages. The default implementation
        /// would clear the control to the current background color, causing
        /// flickering when our OnPaint implementation then immediately draws some
        /// other color over the top using the XNA Framework GraphicsDevice.
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
        }
    }
}
