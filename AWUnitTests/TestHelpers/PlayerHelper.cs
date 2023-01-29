using AW2.Game.Players;
using AW2.Helpers;
using AW2.UI;

namespace AW2.TestHelpers
{
    static class PlayerHelper
    {

        public static Player Make(int id)
        {
            return new Player(game: null,
                pilotId: "pilotId:" + id,
                name: $"Player {id}",
                shipTypeName: CanonicalString.Null,
                weapon2Name: CanonicalString.Null,
                extraDeviceName: CanonicalString.Null,
                controls: new PlayerControls())
            { ID = id };
        }
    }
}
