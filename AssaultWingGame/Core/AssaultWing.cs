using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Menu;

namespace AW2.Core
{
    public class AssaultWing : AssaultWingCore
    {
        public MenuEngineImpl MenuEngine { get; private set; }

        public AssaultWing()
        {
            MenuEngine = new MenuEngineImpl();
            Components.Add(MenuEngine);
        }
    }
}
