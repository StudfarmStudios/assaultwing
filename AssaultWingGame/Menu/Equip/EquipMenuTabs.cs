using System;
using System.Collections.Generic;
using System.Linq;

namespace AW2.Menu.Equip
{
    [Obsolete("Remove!!!")]
    public class EquipMenuTabs
    {
        private MenuEngineImpl _menuEngine;

        public EquipMenuTab Equipment { get; private set; }
        public EquipMenuTab Players { get; private set; }
        public EquipMenuTab Chat { get; private set; }
        public EquipMenuTab GameSettings { get; private set; }

        public EquipMenuTabs(MenuEngineImpl menuEngine)
        {
            _menuEngine = menuEngine;
/*            Equipment = new EquipTab(menuEngine);
            Players = new EquipMenuTab(menuEngine);
            Chat = new EquipMenuTab(menuEngine);
            GameSettings = new EquipMenuTab(menuEngine);*/
            // TODO
        }
    }
}
