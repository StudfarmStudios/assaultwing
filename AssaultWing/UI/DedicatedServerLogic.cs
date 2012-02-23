using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Core;
using AW2.Core.GameComponents;

namespace AW2.UI
{
    public class DedicatedServerLogic : ProgramLogic
    {
        public DedicatedServerLogic(AssaultWing game)
            : base(game)
        {
            var dedicatedServer = new DedicatedServer(game, 13);
            game.Components.Add(dedicatedServer);
            dedicatedServer.Enabled = true;
        }
    }
}
