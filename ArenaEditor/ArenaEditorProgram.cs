using System;
using System.Windows;
using Microsoft.Xna.Framework.Input;
using AW2.Core;
using AW2.Game;
using AW2.Helpers;
using AW2.UI;

namespace AW2
{
    public class ArenaEditorProgram
    {
        private GraphicsDeviceService _graphicsDeviceService;
        private ArenaEditor _editor;

        /// <summary>
        /// The main entry point for Assault Wing Arena Editor.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            using (var graphicsDeviceService = new GraphicsDeviceService())
            {
                var editor = new ArenaEditorProgram(graphicsDeviceService, args);
                editor.Run();
            }
        }

        public ArenaEditorProgram(GraphicsDeviceService graphicsDeviceService, string[] args)
        {
            Log.Write("Assault Wing Arena Editor started");
            _graphicsDeviceService = graphicsDeviceService;
            _editor = new ArenaEditor(graphicsDeviceService, args);
        }

        public void Run()
        {
            AssaultWingCore.GetRealClientAreaSize = () => _editor.ArenaView.Size;
            var app = new Application();
            app.Run(_editor);
        }
    }
}
