using System;
using AW2.Helpers;

namespace AW2.Game
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

        protected override object LoadTemplate(string filename)
        {
            var arena = (Arena)base.LoadTemplate(filename);
            if (arena == null) throw new ArenaLoadException("Failed to load arena (" + filename + ")");
            arena.FileName = filename;
            return arena;
        }
    }
}
