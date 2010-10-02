using System;
using System.Windows;
using AW2.Core;
using AW2.Helpers;

namespace AW2
{
    public class ArenaEditorProgram
    {
        private ArenaEditor _editor;

        /// <summary>
        /// The main entry point for Assault Wing Arena Editor.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            new ArenaEditorProgram(args).Run();
        }

        public ArenaEditorProgram(string[] args)
        {
            Log.Write("Assault Wing Arena Editor started");
            _editor = new ArenaEditor(args);
        }

        public void Run()
        {
            var app = new Application();
            app.Run(_editor);
        }
    }
}
