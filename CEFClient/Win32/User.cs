using System.Runtime.InteropServices;

namespace Win32
{

    public struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpData;
    }

    public class User
    {
        public const int WM_COPYDATA = 0x004A;

        public const int WM_MYSYMPLE = 0x005A;

        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        public static extern int SendMessage(int hWnd, int msg, int wParam, ref COPYDATASTRUCT lParam);

        [DllImport("user32.dll", EntryPoint = "PostMessage")]
        public static extern bool PostMessage(int hWnd, int Msg, int wParam, ref COPYDATASTRUCT lParam);


        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public static extern int FindWindow(string lpClassName, string lpWindowName);


    }
}
