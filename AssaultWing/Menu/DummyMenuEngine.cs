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
        public override void Activate() { }
        public override void WindowResize() { }
        public override void ProgressBarAction(Action asyncAction, Action finishAction) { }
        public override void Deactivate() { }
    }
}
