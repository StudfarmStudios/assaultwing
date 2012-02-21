using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;
using AW2.Graphics;
using AW2.Helpers;
using AW2.Menu;
using AW2.UI;

namespace AW2.Core.OverlayComponents
{
    /// <summary>
    /// Content and action data for an overlay dialog displaying standings after
    /// a finished game session.
    /// </summary>
    public class GameOverOverlayDialogData : OverlayDialogData
    {
        private IEnumerable<Standing> _standings;

        private SpriteFont FontHuge { get { return Menu.MenuContent.FontHuge; } }
        private SpriteFont FontSmall { get { return Menu.MenuContent.FontSmall; } }

        public GameOverOverlayDialogData(MenuEngineImpl menu, IEnumerable<Standing> standings)
            : base(menu, new TriggeredCallback(TriggeredCallback.PROCEED_CONTROL, () => { if (menu.Game.GameState == GameState.GameplayStopped) menu.Game.ShowEquipMenu(); }))
        {
            _standings = standings;
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            var gfx = AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice;
            var textCenter = new Vector2(gfx.Viewport.Width / 2, 45); // text line top center
            var titleText = "Arena Standings";
            var titleSize = FontHuge.MeasureString(titleText);
            var titlePos = textCenter - new Vector2(titleSize.X / 2, 0);
            spriteBatch.DrawString(FontHuge, titleText, titlePos.Round(), Color.White);
            AdvanceLine(ref textCenter, FontHuge);
            AdvanceLine(ref textCenter, FontSmall);
            DrawStandings(spriteBatch, ref textCenter);
            AdvanceLine(ref textCenter, FontSmall);
            var infoText = "Press Enter";
            var infoSize = FontSmall.MeasureString(infoText);
            var infoPos = textCenter - new Vector2(infoSize.X / 2, 0);
            spriteBatch.DrawString(FontSmall, infoText, infoPos.Round(), Color.White);
        }

        private Vector2 DrawStandings(SpriteBatch spriteBatch, ref Vector2 textCenter)
        {
            var enWidth = Menu.MenuContent.FontSmallEnWidth;
            var textLeftX = 50f;
            GraphicsEngineImpl.DrawFormattedText(new Vector2(textLeftX, textCenter.Y), enWidth,
                GetScoreCells("Score", "Kills", "Deaths"),
                (textPos, text) => spriteBatch.DrawString(FontSmall, text, textPos, Color.White));
            AdvanceLine(ref textCenter, FontSmall);
            var allSpectatorsAreLocal = _standings.All(entry => entry.IsLocal);
            int line = 0;
            foreach (var entry in _standings)
            {
                line++;
                var column1Pos = new Vector2(textLeftX, textCenter.Y);
                var textColor =
                    allSpectatorsAreLocal ? entry.Color
                    : !entry.IsLocal ? entry.Color
                    : Color.White;
                GraphicsEngineImpl.DrawFormattedText(column1Pos, enWidth,
                    string.Format("{0}\t\x5{1}{2}", line.ToOrdinalString(), entry.Name, GetScoreCells(entry.Score, entry.Kills, entry.Deaths)),
                    (textPos, text) => spriteBatch.DrawString(FontSmall, text, textPos, textColor));
                AdvanceLine(ref textCenter, FontSmall);
            }
            return textCenter;
        }

        private void AdvanceLine(ref Vector2 textPos, SpriteFont font)
        {
            textPos += new Vector2(0, font.LineSpacing);
        }

        private string GetScoreCells(object scoreText, object killsText, object deathsText)
        {
            return string.Format("\t\x16{0}\t\x1c{1}\t\x22{2}", scoreText, killsText, deathsText);
        }
    }
}
