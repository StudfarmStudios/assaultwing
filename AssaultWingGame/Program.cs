using System;
using AW2.Helpers;
using AW2.Menu;

namespace AW2
{
    static class Program
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
                using (var game = AssaultWing.Instance)
                {
                    game.Run();
                }
#if !DEBUG
            }
            catch (Exception e)
            {
                Log.Write("Assault Wing fatal error");
                Log.Write("Please send the following information to the developers:\n" + e.ToString());
                System.Windows.Forms.MessageBox.Show(e.ToString(), "Assault Wing fatal error");
            }
#endif
        }
    }
}
