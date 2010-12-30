using Microsoft.Xna.Framework.Input;
using AW2.UI;

namespace AW2.Menu.Equip
{
    public class EquipMenuControls
    {
        public Control Back { get; private set; }
        public Control Activate { get; private set; }
        public Control Tab { get; private set; }
        public Control ListUp { get; private set; }
        public Control ListDown { get; private set; }
        public Control StartGame { get; private set; }

        public EquipMenuControls()
        {
            Activate = new KeyboardKey(Keys.Enter);
            Back = new KeyboardKey(Keys.Escape);
            Tab = new KeyboardKey(Keys.Tab);
            ListUp = new KeyboardKey(Keys.Up);
            ListDown = new KeyboardKey(Keys.Down);
            StartGame = new KeyboardKey(Keys.F10);
        }
    }
}
