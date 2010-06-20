using System;
using AW2.Helpers;
using AW2.Menu;

namespace AW2
{
    static class AssaultWingProgram
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
#if !DEBUG
            try
            {
#endif
                Log.Write("Assault Wing started");
                AssaultWing.MenuEngineInitializing += game => new MenuEngineImpl(game);
                AssaultWing.WindowInitializing += game => new AWGameWindow(((Microsoft.Xna.Framework.Game)game).Window, game.GraphicsDeviceManager);
                using (var game = AssaultWing.Instance)
                {
                    game.CommandLineArgs = args;
                    game.Run();
                }
#if !DEBUG
            }
            catch (Exception e)
            {
                Log.Write("Assault Wing fatal error");
                Log.Write("Please send the following information to the developers:\n" + e.ToString());
                AssaultWing.Instance.IsMouseVisible = true;
                System.Windows.Forms.MessageBox.Show(e.ToString(), "Oops, something went wrong! Please send us this information to help fix it.");
            }
#endif
        }
    }
}
