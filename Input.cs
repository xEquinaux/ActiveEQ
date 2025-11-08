using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace cotf.Base
{
    public class Input
    {
        //https://www.pinvoke.net/default.aspx/user32/GetKeyState.html
        internal enum VirtualKeyStates : int
        {
            VK_LBUTTON = 0x01,
            VK_RBUTTON = 0x02,
            VK_CANCEL = 0x03,
            VK_MBUTTON = 0x04,
        }
        [DllImport("USER32.dll")]
        static extern short GetKeyState(VirtualKeyStates nVirtKey);
        private const int KEY_PRESSED = 0x8000;
        public static bool IsLeftPressed()
        {
            return Convert.ToBoolean(GetKeyState(VirtualKeyStates.VK_LBUTTON) & KEY_PRESSED);
        }
        public static bool IsRightPressed()
        {
            return Convert.ToBoolean(GetKeyState(VirtualKeyStates.VK_RBUTTON) & KEY_PRESSED);
        }
    }
}
