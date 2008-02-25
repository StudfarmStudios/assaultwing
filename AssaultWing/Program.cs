using System;

namespace AW2
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
#if !DEBUG
            try
            {
#endif
                AW2.Helpers.Log.Write("Assault Wing started");
                using (AssaultWing game = AssaultWing.Instance)
                {
                    game.Run();
                }
#if !DEBUG
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.ToString(), "Assault Wing fatal error");
            }
#endif
        }
    }
}

