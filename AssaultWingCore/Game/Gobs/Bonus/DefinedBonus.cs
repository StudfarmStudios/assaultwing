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
            _gameAction.Player = player;
            _gameAction.SetDuration(_duration);
            if (!_gameAction.DoAction())
            {
                player.Messages.Add(new PlayerMessage("Useless bonus discarded", PlayerMessage.DEFAULT_COLOR));
                return;
            }

            Gob.CreateGob<ArenaMessage>(Game, (CanonicalString)"bonusmessage", gob =>
            {
                gob.ResetPos(Pos, Vector2.Zero, Gob.DEFAULT_ROTATION);
                gob.Message = _gameAction.BonusText;
                gob.IconName = _gameAction.BonusIconName;
                gob.DrawColor = _gameAction.Player.PlayerColor;
                Game.DataEngine.Arena.Gobs.Add(gob);
            });
            player.BonusActions.AddOrReplace(_gameAction);
            player.Messages.Add(new PlayerMessage("You collected " + _gameAction.BonusText, player.PlayerColor));
        }
    }
}
