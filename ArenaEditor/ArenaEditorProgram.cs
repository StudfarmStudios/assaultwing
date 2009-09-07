using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using AW2.Helpers;

namespace AW2
{
    static class ArenaEditorProgram
    {
        static ArenaEditor editor;

        /// <summary>
        /// The main entry point for Assault Wing Arena Editor.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Log.Write("Assault Wing Arena Editor started");
            editor = new ArenaEditor();
            editor.Show(); // needed for retrieving the window's handle
            var app = new Application();
            AssaultWing.WindowInitializing += g => editor.arenaView;
            var game = AssaultWing.Instance;
            game.AllowDialogs = false;
            game.ClientBoundsMin = new Microsoft.Xna.Framework.Rectangle(0, 0, 1, 1);
            game.GameStateChanged += GetArenaNames;
            var xnaWindow = System.Windows.Forms.Control.FromHandle(((Microsoft.Xna.Framework.Game)game).Window.Handle);
            xnaWindow.VisibleChanged += (sender, eventArgs) => xnaWindow.Visible = false;
            app.Startup += (sender, eventArgs) => game.Run();
            app.Exit += (sender, eventArgs) => game.Exit();
            app.Run(editor);
        }

        static void GetArenaNames(GameState state)
        {
            if (state == GameState.Initializing) return;
            editor.arenaName.Items.Clear();
            foreach (var arenaInfo in AssaultWing.Instance.DataEngine.ArenaInfos)
                editor.arenaName.Items.Add(arenaInfo.Name);
            AssaultWing.Instance.GameStateChanged -= GetArenaNames;
        }
    }
}
