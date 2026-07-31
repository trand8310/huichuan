using System.Runtime.InteropServices;
using System.Text;

namespace System.Win32
{

    public struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpData;
    }

    public class Win32Api
    {
        public const int WM_CLOSE = 0x0010;

        public const int WM_COPYDATA = 0x004A;

        public const int WM_MYSYMPLE = 0x005A;

        [DllImport("User32.dll", CharSet = CharSet.Auto,EntryPoint = "SendMessage")]
        public static extern int SendMessage(int hWnd, int msg, int wParam, ref COPYDATASTRUCT lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "SendMessage")]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);


        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(
            int hWnd,
            int msg,
            IntPtr wParam,
            ref COPYDATASTRUCT lParam,
            uint flags,
            uint timeout,
            out IntPtr result);

        [DllImport("user32.dll", EntryPoint = "PostMessage")]
        public static extern bool PostMessage(int hWnd, int Msg, int wParam, ref COPYDATASTRUCT lParam);


        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public static extern int FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        public static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);


        // 声明 Windows API 函数
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, [Out] StringBuilder lpString, int nMaxCount);
        // 获取窗口标题
        public static string GetWindowTitle(IntPtr hWnd)
        {
            const int nChars = 256;
            var buff = new System.Text.StringBuilder(nChars);
            if (GetWindowText(hWnd, buff, nChars) > 0)
            {
                return buff.ToString();
            }
            return string.Empty;
        }


    }
}
