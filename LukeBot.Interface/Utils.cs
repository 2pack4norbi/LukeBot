using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace LukeBot.Interface
{
    class Utils
    {
    #if (WINDOWS)
        // WinAPI "reconstruction" to al
        private const int STD_INPUT_HANDLE = -10;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CancelIoEx(IntPtr handle, IntPtr lpOverlapped);
    #elif (LINUX)
    #else
        #error "Target platform unsupported"
    #endif

        public static void CancelConsoleIO()
        {
        #if (WINDOWS)
            IntPtr handle = GetStdHandle(STD_INPUT_HANDLE);
            CancelIoEx(handle, IntPtr.Zero);
        #elif (LINUX)
        #endif
        }
    }
}
