using System;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Graphics;
using AW2.Helpers;
using AW2.UI;

namespace AW2.Game
{
    internal class EditorSpectator : Spectator
    {
        public event Action<EditorViewport> ViewportCreated;

        /// <summary>
        /// Center of the view in game world coordinates.
        /// </summary>
        public Vector2 LookAtPos { get; set; }

        public override bool NeedsViewport { get { return true; } }

        public EditorSpectator(AssaultWingCore game)
            : base(game)
        {
        }

        public override AWViewport CreateViewport(Rectangle onScreen)
        {
            var viewport = new EditorViewport(this, onScreen, () => new CanonicalString[0]);
            if (ViewportCreated != null) ViewportCreated(viewport);
            return viewport;
        }

        public override void ResetForArena()
        {
            // Do nothing.
        }
    }
}
