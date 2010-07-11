using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AW2.Helpers;
using AW2.UI;
using AW2.Game;
using Microsoft.Xna.Framework.Input;

namespace AW2
{
    static class ArenaEditorProgram
    {
        private static ArenaEditor editor;

        /// <summary>
        /// The main entry point for Assault Wing Arena Editor.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            Log.Write("Assault Wing Arena Editor started");
            editor = new ArenaEditor();
            editor.Show(); // needed for retrieving the window's handle
            var app = new Application();
            AssaultWing.GetRealClientAreaSize = () => editor.ArenaView.Size;
            AssaultWing.WindowInitializing += g => editor.ArenaView;
            var game = AssaultWing.Instance;
            game.DoNotFreezeCanonicalStrings = true;
            game.SoundEngine.Enabled = false;
            game.CommandLineArgs = args;
            game.AllowDialogs = false;
            game.ClientBoundsMin = new Microsoft.Xna.Framework.Rectangle(0, 0, 1, 1);
            game.RunBegan += Initialize;
            var xnaWindow = System.Windows.Forms.Control.FromHandle(((Microsoft.Xna.Framework.Game)game).Window.Handle);
            xnaWindow.VisibleChanged += (sender, eventArgs) => xnaWindow.Visible = false;
            app.Startup += (sender, eventArgs) => game.Run();
            app.Exit += (sender, eventArgs) => game.Exit();
            app.Run(editor);
        }

        private static void Initialize()
        {
            AssaultWing.Instance.DataEngine.Spectators.Clear();
            var spectatorControls = new PlayerControls
            {
                Thrust = new KeyboardKey(Keys.Up),
                Left = new KeyboardKey(Keys.Left),
                Right = new KeyboardKey(Keys.Right),
                Down = new KeyboardKey(Keys.Down),
                Fire1 = new KeyboardKey(Keys.RightControl),
                Fire2 = new KeyboardKey(Keys.RightShift),
                Extra = new KeyboardKey(Keys.Enter)
            };
            var spectator = new EditorSpectator(spectatorControls);
            AssaultWing.Instance.DataEngine.Spectators.Add(spectator);
        }
    }
}
