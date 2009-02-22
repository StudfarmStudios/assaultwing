using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using AW2.Helpers;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;

namespace AW2.Game
{
    /// <summary>
    /// Handles loading and saving template instances that represent user-defined types
    /// of some class hierarchy such as Gob and its subclasses or Weapon and its subclasses.
    /// </summary>
    class ArenaTypeLoader : TypeLoader
    {
        public ArenaTypeLoader(Type baseClass, string definitionDir) :
            base(baseClass, definitionDir)
        {

        }

        protected override object ParseAndLoadFile(FileInfo fi)
        {
            Arena arena = (Arena)base.ParseAndLoadFile(fi);
            arena.FileName = fi.Name;
            return arena;
            

        }
    }
}
