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
            : base()
        {
            this.lifetime = 10;
            this.deathTime = new TimeSpan(0, 1, 20);
        }

        public DefinedBonus(CanonicalString typeName)
            : base(typeName)
        {
        }

        protected override void DoBonusAction(Player player)
        {
            gameAction.Player = player;
            gameAction.DoAction(duration);

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
