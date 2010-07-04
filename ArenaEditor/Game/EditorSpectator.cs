using System;
using Microsoft.Xna.Framework;
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

        public EditorSpectator(PlayerControls controls)
            : base(controls)
        {
        }

        public override AWViewport CreateViewport(Rectangle onScreen)
        {
            var viewport = new EditorViewport(this, onScreen, () => new CanonicalString[0]);
            if (ViewportCreated != null) ViewportCreated(viewport);
            return viewport;
        }

        public override void Update()
        {
            float moveSpeed = 10;
            LookAtPos +=
                Vector2.UnitY * moveSpeed * Controls.Thrust.Force
                - Vector2.UnitY * moveSpeed * Controls.Down.Force
                + Vector2.UnitX * moveSpeed * Controls.Right.Force
                - Vector2.UnitX * moveSpeed * Controls.Left.Force;
        }
    }
}
