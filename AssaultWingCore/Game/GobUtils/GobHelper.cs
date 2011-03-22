using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Helpers;

namespace AW2.Game.GobUtils
{
    public static class GobHelper
    {
        public static void CreateGobs(IEnumerable<CanonicalString> typeNames, Arena arena, Vector2 pos)
        {
            foreach (var typeName in typeNames)
            {
                Gob.CreateGob<Gob>(arena.Game, typeName, gob =>
                {
                    gob.ResetPos(pos, Vector2.Zero, Gob.DEFAULT_ROTATION);
                    arena.Gobs.Add(gob);
                });
            }
        }

        public static void CreatePengs(IEnumerable<CanonicalString> typeNames, Gob leader)
        {
            foreach (var typeName in typeNames)
            {
                Gob.CreateGob<Peng>(leader.Game, typeName, peng =>
                {
                    peng.ResetPos(leader.Pos, Vector2.Zero, Gob.DEFAULT_ROTATION);
                    peng.Leader = leader;
                    leader.Arena.Gobs.Add(peng);
                });
            }
        }
    }
}
