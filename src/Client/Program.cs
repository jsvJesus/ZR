using System;

namespace Client
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            using var game = new GameHost();
            game.Run();
        }
    }
}
