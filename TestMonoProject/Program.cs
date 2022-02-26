using System;
using PeterMonoLibTest;

namespace TestMonoProject
{
  public static class Program
  {
    [STAThread]
    static void Main()
    {
      Console.WriteLine(Class1.five);
      using (var game = new Game1())
        game.Run();
    }
  }
}
