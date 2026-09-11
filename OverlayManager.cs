using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DigitalMosquito
{
    public class OverlayManager
    {
        private const int GWL_EXSTYLE = -20;

        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        private IntPtr _hwnd = IntPtr.Zero;
        private bool _isClickThrough = true;

        public bool IsClickThrough => _isClickThrough;

        public void InitializeOverlay(Window window)
        {
            var helper = new WindowInteropHelper(window);
            _hwnd = helper.EnsureHandle();

            // Set screen dimensions to cover the primary screen exactly
            window.Left = 0;
            window.Top = 0;
            window.Width = SystemParameters.PrimaryScreenWidth;
            window.Height = SystemParameters.PrimaryScreenHeight;

            // Configure Win32 styles: ToolWindow, Layered, NoActivate, Transparent initially
            long currentStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
            long newStyle = currentStyle
                | WS_EX_LAYERED
                | WS_EX_TOOLWINDOW
                | WS_EX_TOPMOST
                | WS_EX_NOACTIVATE
                | WS_EX_TRANSPARENT;

            SetWindowLong(_hwnd, GWL_EXSTYLE, newStyle);

            _isClickThrough = true;

            // Enforce Topmost without activating or stealing focus
            SetWindowPos(
                _hwnd,
                HWND_TOPMOST,
                0,
                0,
                0,
                0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        public void SetClickThrough(bool clickThrough)
        {
            if (_hwnd == IntPtr.Zero || _isClickThrough == clickThrough)
                return;

            long currentStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
            long newStyle = currentStyle;

            if (clickThrough)
            {
                newStyle |= WS_EX_TRANSPARENT;
            }
            else
            {
                newStyle &= ~WS_EX_TRANSPARENT;
            }

            SetWindowLong(_hwnd, GWL_EXSTYLE, newStyle);
            _isClickThrough = clickThrough;
        }

        public void EnsureTopmost()
        {
            if (_hwnd != IntPtr.Zero)
            {
                SetWindowPos(
                    _hwnd,
                    HWND_TOPMOST,
                    0,
                    0,
                    0,
                    0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
        }

        private static long GetWindowLong(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex).ToInt64();
            else
                return GetWindowLong32(hWnd, nIndex);
        }

        private static void SetWindowLong(IntPtr hWnd, int nIndex, long dwNewLong)
        {
            if (IntPtr.Size == 8)
                SetWindowLongPtr64(hWnd, nIndex, new IntPtr(dwNewLong));
            else
                SetWindowLong32(hWnd, nIndex, (int)dwNewLong);
        }
    }
}
