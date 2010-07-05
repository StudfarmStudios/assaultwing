using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;
using AW2.Helpers;

namespace AW2.Graphics
{
    /// <summary>
    /// Overlay graphics component displaying the player's status.
    /// </summary>
    public class PlayerStatusOverlay : OverlayComponent
    {
        private Player _player;
        private Texture2D _statusDisplayTexture;
        private Texture2D _barShipTexture;
        private Texture2D _iconShipTexture;
        private Texture2D _barMainTexture;
        private Texture2D _iconWeaponLoadTexture;
        private Texture2D _barSpecialTexture;
        private Texture2D _barLoadAmountTexture;

        public override Point Dimensions
        {
            get { return new Point(_statusDisplayTexture.Width, _statusDisplayTexture.Height); }
        }

        private Rectangle ExtraDeviceChargeBarRectangle
        {
            get
            {
                float relativeCharge = _player.Ship.ExtraDevice.Charge / _player.Ship.ExtraDevice.ChargeMax;
                int width = (int)Math.Ceiling(relativeCharge * _barMainTexture.Width);
                return new Rectangle(0, 0, width, _barMainTexture.Height);
            }
        }

        private Rectangle SecondaryWeaponChargeBarRectangle
        {
            get
            {
                float relativeCharge = _player.Ship.Weapon2.Charge / _player.Ship.Weapon2.ChargeMax;
                int width = (int)Math.Ceiling(relativeCharge * _barSpecialTexture.Width);
                return new Rectangle(0, 0, width, _barSpecialTexture.Height);
            }
        }

        public PlayerStatusOverlay(PlayerViewport viewport)
            : base(viewport, HorizontalAlignment.Center, VerticalAlignment.Top)
        {
            _player = viewport.Player;
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            // Status display background
            spriteBatch.Draw(_statusDisplayTexture, Vector2.Zero, Color.White);

            DrawShipDamage(spriteBatch);
            DrawPlayerLives(spriteBatch);
            DrawExtraDeviceCharge(spriteBatch);
            DrawExtraDeviceChargeUsage(spriteBatch);
            DrawExtraDeviceLoadedness(spriteBatch);
            DrawSecondaryWeaponCharge(spriteBatch);
            DrawSecondaryWeaponChargeUsage(spriteBatch);
            DrawSecondaryWeaponLoadedness(spriteBatch);
        }

        private void DrawShipDamage(SpriteBatch spriteBatch)
        {
            if (_player.Ship == null) return;
            Rectangle damageBarRect = new Rectangle(0, 0,
                (int)Math.Ceiling((1 - _player.Ship.DamageLevel / _player.Ship.MaxDamageLevel)
                * _barShipTexture.Width),
                _barShipTexture.Height);
            Color damageBarColor = Color.White;
            if (_player.Ship.DamageLevel / _player.Ship.MaxDamageLevel >= 0.8f)
            {
                float seconds = (float)AssaultWing.Instance.GameTime.TotalRealTime.TotalSeconds;
                if (seconds % 0.5f < 0.25f)
                    damageBarColor = Color.Red;
            }
            spriteBatch.Draw(_barShipTexture,
                new Vector2(_statusDisplayTexture.Width, 8 * 2) / 2,
                damageBarRect, damageBarColor, 0,
                new Vector2(_barShipTexture.Width, 0) / 2,
                1, SpriteEffects.None, 0);
        }

        private void DrawPlayerLives(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < _player.Lives; ++i)
                spriteBatch.Draw(_iconShipTexture,
                    new Vector2(_statusDisplayTexture.Width +
                                _barShipTexture.Width + (8 + i * 10) * 2,
                                _barShipTexture.Height + 8 * 2) / 2,
                    null,
                    Color.White,
                    0,
                    new Vector2(0, _iconShipTexture.Height) / 2,
                    1, SpriteEffects.None, 0);
        }

        private void DrawExtraDeviceCharge(SpriteBatch spriteBatch)
        {
            if (_player.Ship == null) return;
            spriteBatch.Draw(_barMainTexture,
                new Vector2(_statusDisplayTexture.Width, 24 * 2) / 2,
                ExtraDeviceChargeBarRectangle, Color.White, 0,
                new Vector2(_barMainTexture.Width, 0) / 2,
                1, SpriteEffects.None, 0);
                    }

        private void DrawExtraDeviceChargeUsage(SpriteBatch spriteBatch)
        {
            if (_player.Ship == null) return;
            if (_player.Ship.ExtraDevice.FireMode != AW2.Game.GobUtils.ShipDevice.FireModeType.Single) return;
            Rectangle loadAmountExtraBarRect = new Rectangle(0, 0,
                (int)Math.Ceiling(_player.Ship.ExtraDevice.FireCharge / _player.Ship.ExtraDevice.ChargeMax
                * _barMainTexture.Width),
                _barMainTexture.Height);
            spriteBatch.Draw(_barLoadAmountTexture,
                new Vector2(_statusDisplayTexture.Width, 24 * 2) / 2 + new Vector2((int)MathHelper.Clamp(ExtraDeviceChargeBarRectangle.Width - loadAmountExtraBarRect.Width, 0, int.MaxValue), 0),
                loadAmountExtraBarRect, Color.White, 0,
                new Vector2(_barMainTexture.Width, 0) / 2,
                1, SpriteEffects.None, 0);
        }

        private void DrawExtraDeviceLoadedness(SpriteBatch spriteBatch)
        {
            if (_player.Ship == null) return;
            if (!_player.Ship.ExtraDevice.Loaded) return;
            float seconds = _player.Ship.ExtraDevice.LoadedTime.SecondsAgoGameTime();
            float scale = 1;
            Color color = Color.White;
            if (seconds < 0.2f)
                color = new Color(Vector4.Lerp(new Vector4(0, 1, 0, 0.1f), Vector4.One, seconds / 0.2f));
            spriteBatch.Draw(_iconWeaponLoadTexture,
                new Vector2(_statusDisplayTexture.Width + _iconWeaponLoadTexture.Width +
                            _barMainTexture.Width + 8 * 2,
                            _barMainTexture.Height + 24 * 2) / 2,
                null, color, 0,
                new Vector2(_iconWeaponLoadTexture.Width, _iconWeaponLoadTexture.Height) / 2,
                scale, SpriteEffects.None, 0);
        }

        private void DrawSecondaryWeaponCharge(SpriteBatch spriteBatch)
        {
            if (_player.Ship == null) return;
            spriteBatch.Draw(_barSpecialTexture,
                new Vector2(_statusDisplayTexture.Width, 40 * 2) / 2,
                SecondaryWeaponChargeBarRectangle, Color.White, 0,
                new Vector2(_barSpecialTexture.Width, 0) / 2,
                1, SpriteEffects.None, 0);
        }

        private void DrawSecondaryWeaponChargeUsage(SpriteBatch spriteBatch)
        {
            if (_player.Ship == null) return;
            if (_player.Ship.Weapon2.FireMode != AW2.Game.GobUtils.ShipDevice.FireModeType.Single) return;
            var loadAmount2BarRect = new Rectangle(0, 0,
                (int)Math.Ceiling((_player.Ship.Weapon2.FireCharge) / _player.Ship.Weapon2.ChargeMax
                * _barSpecialTexture.Width),
                _barSpecialTexture.Height);
            spriteBatch.Draw(_barLoadAmountTexture,
                new Vector2(_statusDisplayTexture.Width, 40 * 2) / 2 + new Vector2((int)MathHelper.Clamp(SecondaryWeaponChargeBarRectangle.Width - loadAmount2BarRect.Width, 0, int.MaxValue), 0),
                loadAmount2BarRect, Color.White, 0,
                new Vector2(_barSpecialTexture.Width, 0) / 2,
                1, SpriteEffects.None, 0);
        }

        private void DrawSecondaryWeaponLoadedness(SpriteBatch spriteBatch)
        {
            if (_player.Ship == null) return;
            if (!_player.Ship.Weapon2.Loaded) return;
            float seconds = _player.Ship.Weapon2.LoadedTime.SecondsAgoGameTime();
            float scale = 1;
            Color color = Color.White;
            if (seconds < 0.2f)
                color = new Color(Vector4.Lerp(new Vector4(0, 1, 0, 0.2f), Vector4.One, seconds / 0.2f));
            spriteBatch.Draw(_iconWeaponLoadTexture,
                new Vector2(_statusDisplayTexture.Width + _iconWeaponLoadTexture.Width +
                            _barSpecialTexture.Width + 8 * 2,
                            _barSpecialTexture.Height + 40 * 2) / 2,
                null, color, 0,
                new Vector2(_iconWeaponLoadTexture.Width, _iconWeaponLoadTexture.Height) / 2,
                scale, SpriteEffects.None, 0);
        }

        public override void LoadContent()
        {
            var content = AssaultWing.Instance.Content;
            _statusDisplayTexture = content.Load<Texture2D>("gui_playerinfo_bg");
            _barShipTexture = content.Load<Texture2D>("gui_playerinfo_bar_ship");
            _iconShipTexture = content.Load<Texture2D>("gui_playerinfo_ship");
            _barMainTexture = content.Load<Texture2D>("gui_playerinfo_bar_main");
            _iconWeaponLoadTexture = content.Load<Texture2D>("gui_playerinfo_loaded");
            _barSpecialTexture = content.Load<Texture2D>("gui_playerinfo_bar_special");
            _barLoadAmountTexture = content.Load<Texture2D>("gui_playerinfo_bar_loadamount");
        }
    }
}
