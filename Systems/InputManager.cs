using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DigitalMosquito
{
    public class InputManager : IDisposable
    {
        private const int WH_MOUSE_LL = 14;

        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private readonly LowLevelMouseProc _proc;
        private IntPtr _hookId = IntPtr.Zero;
        private bool _isDisposed = false;

        private readonly Window _window;
        private readonly OverlayManager _overlayManager;
        private readonly Func<Creature?> _getActiveCreature;
        private readonly BloodSplatterSystem _bloodSystem;
        private readonly GameState _gameState;

        private bool _isLeftMouseDown = false;
        private Point _lastWipePoint;

        public event Action<Point>? SwatClicked;
        public event Action<Point>? Wiping;
        public Func<Point, bool>? IsOverHud { get; set; }

        public InputManager(
            Window window,
            OverlayManager overlayManager,
            Func<Creature?> getActiveCreature,
            BloodSplatterSystem bloodSystem,
            GameState gameState)
        {
            _window = window;
            _overlayManager = overlayManager;
            _getActiveCreature = getActiveCreature;
            _bloodSystem = bloodSystem;
            _gameState = gameState;

            _proc = HookCallback;
            InstallHook();
        }

        public InputManager(
            Window window,
            OverlayManager overlayManager,
            Creature creature,
            BloodSplatterSystem bloodSystem,
            GameState gameState)
            : this(window, overlayManager, () => creature, bloodSystem, gameState)
        {
        }

        private void InstallHook()
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            if (curModule?.ModuleName != null)
            {
                _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private Point ToClientPoint(Point screenPt)
        {
            try
            {
                if (PresentationSource.FromVisual(_window) != null)
                {
                    return _window.PointFromScreen(screenPt);
                }
            }
            catch
            {
                // Fallback if visual presentation source is not yet ready
            }

            // High-DPI fallback conversion
            try
            {
                var dpi = VisualTreeHelper.GetDpi(_window);
                if (dpi.DpiScaleX > 0.01 && dpi.DpiScaleY > 0.01)
                {
                    return new Point(screenPt.X / dpi.DpiScaleX, screenPt.Y / dpi.DpiScaleY);
                }
            }
            catch
            {
            }

            return screenPt;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && !_isDisposed)
            {
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                int msg = wParam.ToInt32();

                // Physical screen coordinates converted to WPF window DIPs
                var screenPt = new Point(hookStruct.pt.X, hookStruct.pt.Y);
                var clientPt = ToClientPoint(screenPt);

                bool isOverHud = IsOverHud != null && IsOverHud(clientPt);

                // Intercept clicks on creature or blood to prevent passing through to underlying apps.
                // If over HUD, allow the click to hit our own window's buttons.
                if (msg == WM_LBUTTONDOWN && !isOverHud)
                {
                    _isLeftMouseDown = true;
                    if (_gameState.CurrentPhase == GamePhase.Flying)
                    {
                        var creature = _getActiveCreature();
                        if (creature != null && creature.IsHit(clientPt))
                        {
                            _window.Dispatcher.Invoke(() =>
                            {
                                SwatClicked?.Invoke(clientPt);
                            });
                            // Suppress click passing through to underlying window
                            return (IntPtr)1;
                        }
                    }
                    else if (_gameState.CurrentPhase == GamePhase.DeadSplatter ||
                             _gameState.CurrentPhase == GamePhase.Cleaning)
                    {
                        if (_bloodSystem.IsOverSplatter(clientPt))
                        {
                            _window.Dispatcher.Invoke(() =>
                            {
                                Wiping?.Invoke(clientPt);
                            });
                            // Suppress click passing through so user can scrub without selecting background text
                            return (IntPtr)1;
                        }
                    }
                }
                else if (msg == WM_LBUTTONUP)
                {
                    _isLeftMouseDown = false;
                }

                // Process cursor hover and dynamic click-through state
                ProcessInput(msg, clientPt);
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private void ProcessInput(int msg, Point clientPt)
        {
            if (_isDisposed) return;

            bool isOverInteractive = false;
            Cursor targetCursor = Cursors.Arrow;

            bool isOverHud = IsOverHud != null && IsOverHud(clientPt);

            if (isOverHud)
            {
                isOverInteractive = true;
                targetCursor = Cursors.Arrow;
            }
            else if (_gameState.CurrentPhase == GamePhase.Flying)
            {
                var creature = _getActiveCreature();
                isOverInteractive = creature != null && creature.IsHit(clientPt);
                if (isOverInteractive)
                {
                    targetCursor = Cursors.Cross; // Crosshair / Swatter indicator
                }
            }
            else if (_gameState.CurrentPhase == GamePhase.DeadSplatter ||
                     _gameState.CurrentPhase == GamePhase.Cleaning)
            {
                isOverInteractive = _bloodSystem.IsOverSplatter(clientPt);
                if (isOverInteractive)
                {
                    targetCursor = Cursors.Hand; // Hand / Cleaning indicator

                    // If mouse is moving over blood or holding button, trigger wiping
                    double dist = (clientPt - _lastWipePoint).Length;
                    if (dist > 3.0 || _isLeftMouseDown)
                    {
                        _lastWipePoint = clientPt;
                        _window.Dispatcher.InvokeAsync(() =>
                        {
                            Wiping?.Invoke(clientPt);
                        }, System.Windows.Threading.DispatcherPriority.Input);
                    }
                }
            }

            // Set dynamic click-through:
            // When over interactive elements, clickThrough = false (catches mouse)
            // When outside interactive elements, clickThrough = true (passes through to underlying apps)
            _overlayManager.SetClickThrough(!isOverInteractive);

            if (_window.Cursor != targetCursor)
            {
                _window.Cursor = targetCursor;
            }
        }

        public void CheckCursorState()
        {
            // Heartbeat check called from 60fps render loop
            if (GetCursorPos(out POINT pt))
            {
                var screenPt = new Point(pt.X, pt.Y);
                var clientPt = ToClientPoint(screenPt);
                ProcessInput(WM_MOUSEMOVE, clientPt);
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                if (_hookId != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hookId);
                    _hookId = IntPtr.Zero;
                }
            }
        }
    }
}
