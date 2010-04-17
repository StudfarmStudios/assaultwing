using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Graphics;
using AW2.Helpers;
using AW2.UI;

namespace AW2.Game
{
    internal class EditorSpectator : Spectator
    {
        public event Action<EditorViewport> ViewportCreated;

        public EditorSpectator(PlayerControls controls)
            : base(controls)
        {
        }

        public override AWViewport CreateViewport(Rectangle onScreen)
        {
            var viewport = new EditorViewport(onScreen, LookAt, () => new CanonicalString[0]);
            if (ViewportCreated != null) ViewportCreated(viewport);
            return viewport;
        }
    }
}
