using System;
using AW2.Helpers.Serialization;

namespace AW2.Game.Arenas
{
    /// <summary>
    /// Handles loading and saving template instances that represent user-defined types
    /// of some class hierarchy such as Gob and its subclasses or Weapon and its subclasses.
    /// </summary>
    public class ArenaTypeLoader : TypeLoader
    {
        public ArenaTypeLoader(Type baseClass, string definitionDir)
            : base(baseClass, definitionDir)
        {
        }

        protected override object LoadTemplate(string filename, bool tolerant)
        {
            var arena = (Arena)base.LoadTemplate(filename, tolerant);
            if (arena == null) throw new ArenaLoadException("Failed to load arena (" + filename + ")");
            arena.Info.FileName = filename;
            return arena;
        }
    }
}
