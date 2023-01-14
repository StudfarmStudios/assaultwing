using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AW2.Core
{
    static public class WinMm
    {
        [DllImport("winmm.dll")]
        public static extern uint timeBeginPeriod(uint period);
        [DllImport("winmm.dll")]
        public static extern uint timeEndPeriod(uint period);
    }
}
