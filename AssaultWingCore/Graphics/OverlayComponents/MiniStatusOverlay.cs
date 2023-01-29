using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game;
using AW2.Game.Players;
using AW2.Helpers;

namespace AW2.Graphics.OverlayComponents
{
    /// <summary>
    /// Overlay graphics component displaying a small damage bar and health
    /// percentage below a player's ship.
    /// </summary>
    public class MiniStatusOverlay : OverlayComponent
    {
        private const float OPAQUE_DURATION = 2; // component's opaque state duration in seconds
        private const float FADEOUT_DURATION = 1; // component's transparent fadeout duration in seconds

        private float _lastRelativeHealth;
        private TimeSpan _fadeoutFinishTime; // in game time
        private Player _player;
        private Texture2D _barFillTexture, _barBackgroundTexture;
        private SpriteFont _healthFont;
        private Rectangle _customAlignmentDampBox;
        private Vector2? _previousCustomAlignment;

        public override Point Dimensions
        {
            get { return new Point(_barBackgroundTexture.Width, _barBackgroundTexture.Height + _healthFont.LineSpacing); }
        }

        public MiniStatusOverlay(PlayerViewport viewport)
            : base(viewport, HorizontalAlignment.Center, VerticalAlignment.Center)
        {
            _player = viewport.Owner;
            _customAlignmentDampBox = new Rectangle(0, 0, 2, 2);
            CustomAlignment = GetCustomAlignment;
        }

        public override void LoadContent()
        {
            var content = AssaultWingCore.Instance.Content;
            _barFillTexture = content.Load<Texture2D>("mini_hpbar_fill");
            _barBackgroundTexture = content.Load<Texture2D>("mini_hpbar_bg");
            _healthFont = content.Load<SpriteFont>("ConsoleFont");
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            if (_player.Ship == null) return;
            // Calculate alpha level based on changes in player's ship damage.
            float relativeHealth = 1 - _player.Ship.DamageLevel / _player.Ship.MaxDamageLevel;
            if (_lastRelativeHealth != relativeHealth)
            {
                _fadeoutFinishTime = AssaultWingCore.Instance.DataEngine.ArenaTotalTime +
                    TimeSpan.FromSeconds(OPAQUE_DURATION + FADEOUT_DURATION);
                _lastRelativeHealth = relativeHealth;
            }
            float alpha = MathHelper.Clamp((float)(_fadeoutFinishTime - AssaultWingCore.Instance.DataEngine.ArenaTotalTime).TotalSeconds / FADEOUT_DURATION, 0, 1);
            DrawHealthBar(spriteBatch, relativeHealth, alpha);
            DrawHealthPercentage(spriteBatch, relativeHealth, alpha);
        }

        private Vector2 GetCustomAlignment()
        {
            if (_player.Ship == null) return Vector2.Zero;
            // Note: Screen Y axis points down and game Y axis points up.
            var newCustomAlignment = new Vector2(0, 40) + (_player.Ship.Pos + _player.Ship.DrawPosOffset - Viewport.CurrentLookAt).MirrorY();
            var newCustomAlignmentPoint = Vector2.Round(newCustomAlignment).ToPoint();
            if (!_previousCustomAlignment.HasValue || !_customAlignmentDampBox.Contains(newCustomAlignmentPoint))
            {
                _customAlignmentDampBox = _customAlignmentDampBox.MoveToContain(newCustomAlignmentPoint);
                _previousCustomAlignment = newCustomAlignment;
                return newCustomAlignment;
            }
            return _previousCustomAlignment.Value;
        }

        private void DrawHealthBar(SpriteBatch spriteBatch, float relativeHealth, float alpha)
        {
            var color = Color.Multiply(Color.White, alpha);
            int width = (int)Math.Ceiling(relativeHealth * _barFillTexture.Width);
            var healthBarRect = new Rectangle(0, 0, width, _barFillTexture.Height);
            spriteBatch.Draw(_barBackgroundTexture, Vector2.Zero, color);
            spriteBatch.Draw(_barFillTexture, new Vector2(4, 3), healthBarRect, color);
        }

        private Color DrawHealthPercentage(SpriteBatch spriteBatch, float relativeHealth, float alpha)
        {
            var halfColor = Color.Multiply(Color.White, alpha * 1f);
            var halfBlackColor = Color.Multiply(Color.Black, alpha * 0.6f);
            var healthText = ((int)Math.Ceiling(relativeHealth * 100)).ToString() + "%";
            var textSize = _healthFont.MeasureString(healthText);
            var textPos = new Vector2((Dimensions.X - textSize.X) / 2, (_barBackgroundTexture.Height / 2) - (textSize.Y / 2) + 1);
            spriteBatch.DrawString(_healthFont, healthText, Vector2.Round(textPos) - Vector2.One, halfBlackColor);
            spriteBatch.DrawString(_healthFont, healthText, Vector2.Round(textPos) + new Vector2(1, -1), halfBlackColor);
            spriteBatch.DrawString(_healthFont, healthText, Vector2.Round(textPos) + Vector2.One, halfBlackColor);
            spriteBatch.DrawString(_healthFont, healthText, Vector2.Round(textPos) + new Vector2(-1, 1), halfBlackColor);
            spriteBatch.DrawString(_healthFont, healthText, Vector2.Round(textPos), halfColor);
            return halfColor;
        }
    }
}
