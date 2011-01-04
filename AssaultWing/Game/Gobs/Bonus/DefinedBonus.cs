using Microsoft.Xna.Framework;
using AW2.Helpers;

namespace AW2.Game.Gobs.Bonus
{
    /// <summary>
    /// A bonus that can be collected by a player.
    /// </summary>
    public class DefinedBonus : Bonus
    {
        /// This constructor is only for serialisation.
        public DefinedBonus()
        {
        }

        public DefinedBonus(CanonicalString typeName)
            : base(typeName)
        {
        }

        protected override void DoBonusAction(Player player)
        {
            gameAction.Player = player;
            gameAction.SetDuration(duration);
            if (!gameAction.DoAction())
            {
                player.Messages.Add(new PlayerMessage("Useless bonus discarded", Player.DEFAULT_COLOR));
                return;
            }

            Gob.CreateGob<ArenaMessage>(Game, (CanonicalString)"bonusmessage", gob =>
            {
                gob.ResetPos(Pos, Vector2.Zero, Gob.DEFAULT_ROTATION);
                gob.Message = gameAction.BonusText;
                gob.IconName = gameAction.BonusIconName;
                gob.DrawColor = gameAction.Player.PlayerColor;
                Game.DataEngine.Arena.Gobs.Add(gob);
            });
            player.BonusActions.AddOrReplace(gameAction);
            player.Messages.Add(new PlayerMessage("You collected " + gameAction.BonusText, player.PlayerColor));
        }
    }
}
