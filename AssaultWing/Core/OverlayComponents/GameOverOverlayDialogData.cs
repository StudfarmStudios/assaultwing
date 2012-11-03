using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.Logic;
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
        private const float TextLeftX = 50;
        private const float TextTopY = 45;

        private Tuple<Standing, Standing[]>[] _standings;

        private SpriteFont FontHuge { get { return Menu.MenuContent.FontHuge; } }
        private SpriteFont FontSmall { get { return Menu.MenuContent.FontSmall; } }

        public GameOverOverlayDialogData(MenuEngineImpl menu, Tuple<Standing, Standing[]>[] standings, params TriggeredCallback[] actions)
            : base(menu, actions)
        {
            _standings = standings;
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            var gfx = AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice;
            var textCenter = new Vector2(gfx.Viewport.Width / 2, TextTopY); // text line top center
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

        private void DrawStandings(SpriteBatch spriteBatch, ref Vector2 textCenter)
        {
            GraphicsEngineImpl.DrawFormattedText(new Vector2(TextLeftX, textCenter.Y), Menu.MenuContent.FontSmallEnWidth,
                GetScoreCells("Score", "Kills", "Deaths"),
                (textPos, text) => spriteBatch.DrawString(FontSmall, text, textPos, Color.White));
            AdvanceLine(ref textCenter, FontSmall);
            int position = 1;
            foreach (var entry in _standings)
            {
                DrawStandingsLine(spriteBatch, ref textCenter, entry.Item1, position++, indent: 0);
                foreach (var subEntry in entry.Item2) DrawStandingsLine(spriteBatch, ref textCenter, subEntry, position: null, indent: 1);
            }
        }

        private void DrawStandingsLine(SpriteBatch spriteBatch, ref Vector2 textCenter, Standing standing, int? position, int indent)
        {
            var column1Pos = new Vector2(TextLeftX, textCenter.Y);
            var entrySpec = Game.DataEngine.FindSpectator(standing.ID);
            var useHighlighting = Game.NetworkMode != NetworkMode.Standalone;
            var isHighlighted = entrySpec != null && entrySpec.IsLocal;
            var textColor = useHighlighting && isHighlighted ? Color.White : standing.Color;
            GraphicsEngineImpl.DrawFormattedText(column1Pos, Menu.MenuContent.FontSmallEnWidth,
                string.Format("{0}\t{1}{2}{3}",
                    position.HasValue ? position.Value.ToOrdinalString() : "",
                    (char)(5 + indent),
                    standing.Name,
                    GetScoreCells(standing.Score, standing.Kills, standing.Deaths)),
                (textPos, text) => spriteBatch.DrawString(FontSmall, text, textPos, textColor));
            AdvanceLine(ref textCenter, FontSmall);
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
