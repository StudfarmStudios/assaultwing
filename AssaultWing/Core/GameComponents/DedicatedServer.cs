using System;
using System.Collections.Generic;
using System.Linq;

namespace AW2.Core.GameComponents
{
    public class DedicatedServer : AWGameComponent
    {
        public new AssaultWing Game { get; private set; }

        public DedicatedServer(AssaultWing game, int updateOrder)
            : base(game, updateOrder)
        {
            Game = game;
        }

        public override void Update()
        {
            base.Update();
        }
    }
}
