using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;


//https://github.com/icemanCZ/winforms-monitor-disabler/blob/943c0a1232813e374acea45dbcabef91dc306d51/winforms-monitor-disabler/Native.cs
namespace MonitorSwitch
{
    public class WinAPI
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szPhysicalMonitorDescription;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;

            public void Init()
            {
                cbSize = Marshal.SizeOf(this);
                szDevice = new string(' ', 32);
            }
        }

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", EntryPoint = "MonitorFromWindow")]
        public static extern IntPtr MonitorFromWindow([In] IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", EntryPoint = "EnumDisplayMonitors")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lpRect, MonitorEnumProc callback, int dwData);

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hDCMonitor, ref RECT pRectangle, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hmonitor, ref MONITORINFOEX info);

        [DllImport("dxva2.dll", EntryPoint = "SetVCPFeature", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool _SetVCPFeature(IntPtr hMonitor, byte bVCPCode, short dwNewValue);

        [DllImport("dxva2.dll", EntryPoint = "GetPhysicalMonitorsFromHMONITOR", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", EntryPoint = "GetNumberOfPhysicalMonitorsFromHMONITOR", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, ref uint pdwNumberOfPhysicalMonitors);


        [DllImport("dxva2.dll", EntryPoint = "DestroyPhysicalMonitors", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, ref PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        private const byte PowerMode = 0xD6;  // VCP Code defined in VESA Monitor Control Command Set (MCCS) standard
        private const short PowerOn = 0x01;
        private const short PowerOff = 0x04;

        private const byte VCPInputSelectCode = 0x60;
        // Correct for asus vg248qe
        // Use something like https://www.nirsoft.net/utils/control_my_monitor.html to get values for monitor.
        public const short DisplayPort = 0xF;
        public const short DVI = 0x3;
        public const short HDMI = 0x11;


        public static bool SetVCPFeatureByWindow(IntPtr windowHandle, byte bVCPCode, short dwNewValue)
        {
            IntPtr hMonitor = MonitorFromWindow(windowHandle, 1);
            return SetVCPFeatureByMonitor(hMonitor, bVCPCode, dwNewValue);
        }

        public static bool SetVCPFeatureByMonitor(IntPtr hMonitor, byte bVCPCode, short dwNewValue)
        {
            uint pdwNumberOfPhysicalMonitors = 0;
            bool res = GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, ref pdwNumberOfPhysicalMonitors);
            if (!res || pdwNumberOfPhysicalMonitors == 0)
            {
                return false;
            }

            var pPhysicalMonitorArray = new PHYSICAL_MONITOR[pdwNumberOfPhysicalMonitors];
            GetPhysicalMonitorsFromHMONITOR(hMonitor, pdwNumberOfPhysicalMonitors, pPhysicalMonitorArray);
            res = _SetVCPFeature(pPhysicalMonitorArray[0].hPhysicalMonitor, bVCPCode, dwNewValue);
            DestroyPhysicalMonitors(pdwNumberOfPhysicalMonitors, ref pPhysicalMonitorArray);
            return res;
        }

        public static void Test()
        {
            foreach (var screen in Screen.AllScreens)
            {
                FieldInfo fi = typeof(Screen).GetField("hmonitor", BindingFlags.NonPublic | BindingFlags.Instance);
                IntPtr hMonitor = (IntPtr)fi.GetValue(screen);
                var primary = screen.Primary;
                var name = screen.DeviceName;

                switch (name)
                {
                    case @"\\.\DISPLAY1":
                        SetVCPFeatureByMonitor(hMonitor, VCPInputSelectCode, HDMI);
                        break;
                    default:
                        break;
                }
            }
        }
        public static void SetDisplayMonitors(bool connected)
        {
            foreach (var screen in Screen.AllScreens)
            {
                FieldInfo fi = typeof(Screen).GetField("hmonitor", BindingFlags.NonPublic | BindingFlags.Instance);
                IntPtr hMonitor = (IntPtr)fi.GetValue(screen);
                var primary = screen.Primary;
                var name = screen.DeviceName;

                switch (name)
                {
                    case @"\\.\DISPLAY1":
                        if (connected)
                        {
                            SetVCPFeatureByMonitor(hMonitor, VCPInputSelectCode, DisplayPort);
                        }
                        else
                        {
                            SetVCPFeatureByMonitor(hMonitor, VCPInputSelectCode, HDMI);
                        }
                        break;
                    case @"\\.\DISPLAY2":
                        if (connected)
                        {
                            SetVCPFeatureByMonitor(hMonitor, VCPInputSelectCode, DisplayPort);
                        }
                        else
                        {
                            //SetVCPFeatureByMonitor(hMonitor, VCPInputSelectCode, DVI);
                        }
                        break;
                    case @"\\.\DISPLAY3":
                        if (connected)
                        {
                            SetVCPFeatureByMonitor(hMonitor, VCPInputSelectCode, DisplayPort);
                        }
                        else
                        {
                            SetVCPFeatureByMonitor(hMonitor, VCPInputSelectCode, HDMI);
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }
}