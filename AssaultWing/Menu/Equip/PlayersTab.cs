using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Game.Players;
using AW2.Helpers;

namespace AW2.Menu.Equip
{
    public class PlayersTab : EquipMenuTab
    {
        public override Texture2D TabTexture { get { return Content.TabPlayersTexture; } }
        public override string HelpText { get { return "Arrows select, " + BasicHelpText; } }

        public PlayersTab(EquipMenuComponent menuComponent)
            : base(menuComponent)
        {
        }

        public override void Update()
        {
            if (Controls.Dirs.Down.Pulse)
            {
                PlayerList.CurrentIndex++;
                MenuEngine.Game.SoundEngine.PlaySound("MenuBrowseItem");
                MenuComponent.ListCursorFadeStartTime = MenuEngine.Game.GameTime.TotalRealTime;
            }
            if (Controls.Dirs.Up.Pulse)
            {
                PlayerList.CurrentIndex--;
                MenuEngine.Game.SoundEngine.PlaySound("MenuBrowseItem");
                MenuComponent.ListCursorFadeStartTime = MenuEngine.Game.GameTime.TotalRealTime;
            }
        }

        public override void Draw(Vector2 view, SpriteBatch spriteBatch)
        {
            DrawLargeStatusBackground(view, spriteBatch);
            DrawPlayerListDisplay(view, spriteBatch, drawCursor: true);
            if (MenuEngine.Game.DataEngine.Players.Any())
                DrawPlayerInfoDisplay(view, spriteBatch, MenuEngine.Game.DataEngine.Players.ElementAt(PlayerList.CurrentIndex));
        }

        private void DrawPlayerInfoDisplay(Vector2 view, SpriteBatch spriteBatch, Player player)
        {
            var weapon = (Weapon)MenuEngine.Game.DataEngine.GetTypeTemplate(player.Weapon2Name);
            var weaponInfo = weapon.DeviceInfo;
            var device = (ShipDevice)MenuEngine.Game.DataEngine.GetTypeTemplate(player.ExtraDeviceName);
            var deviceInfo = device.DeviceInfo;
            var ship = (Ship)MenuEngine.Game.DataEngine.GetTypeTemplate(player.ShipName);
            var shipInfo = ship.ShipInfo;
            var infoDisplayPos = MenuComponent.Pos - view + new Vector2(570, 191);

            var shipPicture = MenuEngine.Game.Content.Load<Texture2D>(shipInfo.PictureName);
            var shipTitlePicture = MenuEngine.Game.Content.Load<Texture2D>(shipInfo.TitlePictureName);
            var weaponPicture = MenuEngine.Game.Content.Load<Texture2D>(weaponInfo.PictureName);
            var weaponTitlePicture = MenuEngine.Game.Content.Load<Texture2D>(weaponInfo.TitlePictureName);
            var devicePicture = MenuEngine.Game.Content.Load<Texture2D>(deviceInfo.PictureName);
            var deviceTitlePicture = MenuEngine.Game.Content.Load<Texture2D>(deviceInfo.TitlePictureName);

            spriteBatch.Draw(shipPicture, infoDisplayPos, null, Color.White, 0,
                new Vector2(0, 0), 0.6f, SpriteEffects.None, 0);
            spriteBatch.DrawString(Content.FontBig, "Ship", Vector2.Round(infoDisplayPos + new Vector2(149, 15)), Color.White);
            spriteBatch.Draw(shipTitlePicture, infoDisplayPos + new Vector2(140, 37), Color.White);

            spriteBatch.Draw(devicePicture, infoDisplayPos + new Vector2(0, 120), null, Color.White, 0,
                new Vector2(0, 0), 0.6f, SpriteEffects.None, 0);
            spriteBatch.DrawString(Content.FontBig, "Ship Modification", Vector2.Round(infoDisplayPos + new Vector2(0, 120) + new Vector2(149, 15)), Color.White);
            spriteBatch.Draw(deviceTitlePicture, infoDisplayPos + new Vector2(0, 120) + new Vector2(140, 37), Color.White);

            spriteBatch.Draw(weaponPicture, infoDisplayPos + new Vector2(0, 240), null, Color.White, 0,
                new Vector2(0, 0), 0.6f, SpriteEffects.None, 0);
            spriteBatch.DrawString(Content.FontBig, "Special Weapon", Vector2.Round(infoDisplayPos + new Vector2(0, 240) + new Vector2(149, 15)), Color.White);
            spriteBatch.Draw(weaponTitlePicture, infoDisplayPos + new Vector2(0, 240) + new Vector2(140, 37), Color.White);
        }
    }
}
