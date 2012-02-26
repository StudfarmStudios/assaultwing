using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Core;

namespace AW2.UI
{
    public class QuickStartLogic : UserControlledLogic
    {
        public QuickStartLogic(AssaultWing game)
            : base(game)
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            Game.GameState = GameState.Menu;
            AW2.Menu.Main.MainMenuItemCollections.Click_LocalGame(MenuEngine);
            Game.IsReadyToStartArena = true;
        }
    }
}
