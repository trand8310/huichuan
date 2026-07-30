/******************************************************
 * 项目名称: HitMan.Win32
 * 项目描述:
 * 类 名 称: Win32Api
 * 版 本 号:
 * 说    明:
 * 作    者：刘伟
 * 创建时间：2020/10/30 10:32:04
 *******************************************************
 * Copyright @ 元拓 2020. All right reserved.
******************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Win32
{
    public class NativeMethod
    {
        public const int WM_COPYDATA = 0x004A;
        public const int WM_MYSYMPLE = 0x005A;
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr parentWindow, IntPtr previousChildWindow, string windowClass, string windowTitle);
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowThreadProcessId(IntPtr window, out int process);
        private IntPtr[] GetProcessWindows(int process, string title)
        {
            IntPtr[] apRet = (new IntPtr[256]);
            int iCount = 0;
            IntPtr pLast = IntPtr.Zero;
            do
            {
                pLast = FindWindowEx(IntPtr.Zero, pLast, null, title);
                int iProcess_;
                GetWindowThreadProcessId(pLast, out iProcess_);
                if (iProcess_ == process) apRet[iCount++] = pLast;
            } while (pLast != IntPtr.Zero);
            System.Array.Resize(ref apRet, iCount);
            return apRet;
        }



        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool CreateProcess(
           string lpApplicationName,
           string lpCommandLine,
           ref SECURITY_ATTRIBUTES lpProcessAttributes,
           ref SECURITY_ATTRIBUTES lpThreadAttributes,
           bool bInheritHandles,
           uint dwCreationFlags,
           IntPtr lpEnvironment,
           string lpCurrentDirectory,
           [In] ref STARTUPINFO lpStartupInfo,
           out PROCESS_INFORMATION lpProcessInformation);


        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        public static extern int SendMessage(int hWnd, int msg, int wParam, ref COPYDATASTRUCT lParam);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(int hWnd, int Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);


        [DllImport("psapi.dll")]
        public static extern int EmptyWorkingSet(IntPtr hwProc);


        [DllImport("user32.dll", EntryPoint = "keybd_event")]
        public static extern void keybd_event(

           byte bVk,    //虚拟键值
           byte bScan,// 一般为0
           int dwFlags,  //这里是整数类型  0 为按下，2为释放
           int dwExtraInfo  //这里是整数类型 一般情况下设成为 0
       );


    }
}