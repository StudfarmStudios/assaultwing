using System;
using System.Linq;
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
            gameAction.DoAction();

            Gob.CreateGob((CanonicalString)"bonusmessage", gob =>
            {
                gob.ResetPos(Pos, gob.Move, gob.Rotation);
                ((BonusMessage)gob).Message = gameAction.BonusText;
                ((BonusMessage)gob).IconName = gameAction.BonusIconName;
                AssaultWing.Instance.DataEngine.Arena.Gobs.Add(gob);
            });
            player.BonusActions.AddOrReplace(gameAction);     
        }
    }
}
