using System;
using System.Windows;
using Microsoft.Xna.Framework.Input;
using AW2.Core;
using AW2.Game;
using AW2.Helpers;
using AW2.UI;

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
            var xnaWindow = System.Windows.Forms.Control.FromHandle(editor.ArenaViewHost.Handle);
            xnaWindow.VisibleChanged += (sender, eventArgs) => xnaWindow.Visible = false;
            var runner = new AWGameRunner(game,
                () => editor.ArenaView.BeginInvoke((Action)editor.ArenaView.Invalidate),
                gameTime => editor.ArenaView.BeginInvoke((Action)(() => AssaultWing.Instance.Update(gameTime))));
            app.Startup += (sender, eventArgs) => runner.Run();
            app.Exit += (sender, eventArgs) => runner.Exit();
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
