using Display;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RhinoWASD
{
    public static class Interceptor
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_NCMOUSEMOVE = 0x00A0;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_MOUSEWHEEL = 0x020A;

        private const int TIMER_INVERVAL = 1;
        private const int MOUSE_SENSITIVITY = 8000;

        private static LowLevelProc _proc = HookCallback;
        private static IntPtr _kHook = IntPtr.Zero, _mHook = IntPtr.Zero;
        private static Timer timer;
        private static Point3d BeforeLocation;
        private static Vector3d BeforeDirection;
        private static double speed = Properties.Settings.Default.Speed;
        private static System.Drawing.Point CursorPositionBuffer = System.Drawing.Point.Empty;
        private static System.Drawing.Point LastPos = System.Drawing.Point.Empty;
        private static double LastTargetDistance = double.NaN;
        private static System.Drawing.Rectangle ScreenRect;

        private static System.Drawing.Point MidPoint
        {
            get
            {
                return new System.Drawing.Point(
                  ScreenRect.Left + ScreenRect.Width / 2,
                  ScreenRect.Top + ScreenRect.Height / 2);
            }
        }

        public static bool Q = false,
            W = false,
            E = false,
            S = false,
            A = false,
            D = false,
            Shift = false,
            Esc = false,
            Enter = false;

        public static System.Drawing.Point MouseOffset = System.Drawing.Point.Empty;

        public static void ShowSpeedMessage()
        {
            string speedText = "";
            if (speed >= 100)
                speedText = Math.Round(speed, 0).ToString();
            else if (speed >= 10)
                speedText = Math.Round(speed, 1).ToString();
            else if (speed >= 1)
                speedText = Math.Round(speed, 2).ToString();
            else if (speed >= 0.1)
                speedText = Math.Round(speed, 3).ToString();
            else if (speed >= 0.01)
                speedText = Math.Round(speed, 4).ToString();
            Overlay.ShowMessage("Speed " + speedText);
        }

        public static void StartWASD()
        {
            //Save Mouse Position & set cursor to primaryscreen center
            ShowCursor(false);
            CursorPositionBuffer = new System.Drawing.Point(Cursor.Position.X, Cursor.Position.Y);
            LastTargetDistance =
                RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation.DistanceTo(
                    RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraTarget
                );

            ScreenRect = Screen.PrimaryScreen.Bounds;
            Cursor.Position = MidPoint;

            RhinoViewport vp = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;
            speed = Properties.Settings.Default.Speed;
            BeforeLocation = vp.CameraLocation;
            BeforeDirection = new Vector3d(vp.CameraDirection);

            Q = W = E = S = A = D = Shift = Esc = Enter = false;
            if (timer == null)
            {
                timer = new Timer();
                timer.Interval = TIMER_INVERVAL;
                timer.Tick += OnTick;
                timer.Start();
            }

            _kHook = SetHook(_proc, true);
            _mHook = SetHook(_proc, false);

            ShowSpeedMessage();
        }

        public static void StopWASD(bool ShouldKeepView)
        {
            RhinoViewport vp = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;

            if (!ShouldKeepView)
            {
                vp.SetCameraDirection(BeforeDirection, false);
                vp.SetCameraLocation(BeforeLocation, false);
            }

            Overlay.ShowImage(null);

            if (timer != null)
            {
                timer.Stop();
                timer.Tick -= OnTick;
                timer.Dispose();
                timer = null;
            }

            UnhookWindowsHookEx(_kHook);
            UnhookWindowsHookEx(_mHook);

            if (!double.IsNaN(LastTargetDistance))
            {
                Point3d newTarget = vp.CameraLocation + vp.CameraDirection * LastTargetDistance;
                vp.SetCameraTarget(newTarget, false);
            }
            LastTargetDistance = double.NaN;

            Cursor.Position = CursorPositionBuffer;
            ShowCursor(true);

            Q = W = E = S = A = D = Shift = Esc = Enter = false;
        }

        private static void OnTick(object sender, EventArgs args)
        {
            if (MouseOffset.IsEmpty && !Q && !W && !E && !A && !S && !D)
                return;

            RhinoViewport vp = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;
            Point3d loc = vp.CameraLocation;
            Vector3d dir = new Vector3d(vp.CameraDirection);
            Vector3d up = new Vector3d(vp.CameraUp);
            dir.Unitize();
            up.Unitize();

            // Change direction if mouse moved
            if (!MouseOffset.IsEmpty)
            {
                double angleH = MouseOffset.X / (double)MOUSE_SENSITIVITY * -360.0;
                double angleV = MouseOffset.Y / (double)MOUSE_SENSITIVITY * -360.0;
                MouseOffset = System.Drawing.Point.Empty;

                dir.Rotate(Math.PI * angleH / 180.0, up);
                dir.Rotate(Math.PI * angleV / 180.0, Vector3d.CrossProduct(vp.CameraDirection, up));
                vp.SetCameraDirection(dir, false);
            }

            // Move camera if some key is pressed

            double finalSpeed = speed;

            if (Shift)
                finalSpeed *= 4;

            if (Q)
                loc -= Vector3d.ZAxis * finalSpeed;
            if (W)
                loc += dir * finalSpeed;
            if (E)
                loc += Vector3d.ZAxis * finalSpeed;
            if (A)
                loc -= Vector3d.CrossProduct(dir, up) * finalSpeed;
            if (S)
                loc -= dir * finalSpeed;
            if (D)
                loc += Vector3d.CrossProduct(dir, up) * finalSpeed;

            vp.SetCameraLocation(loc, false);
            if (!vp.IsVisible(vp.CameraTarget))
            {
                Point3d newTarget = vp.CameraLocation + vp.CameraDirection * 10000;
                vp.SetCameraTarget(newTarget, false);
            }

            RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
        }

        private static IntPtr SetHook(LowLevelProc proc, bool Keyboard)
        {
            using (System.Diagnostics.Process curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (System.Diagnostics.ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(Keyboard ? WH_KEYBOARD_LL : WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0)
                return CallNextHookEx((IntPtr)0, nCode, wParam, lParam);

            if (wParam == (IntPtr)WM_LBUTTONDOWN)
                StopWASD(true);
            else if (wParam == (IntPtr)WM_MBUTTONDOWN || wParam == (IntPtr)WM_RBUTTONDOWN)
                StopWASD(false);

            if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_KEYUP)
            {
                bool IsKeyDown = wParam == (IntPtr)WM_KEYDOWN;
                Keys key = (Keys)Marshal.ReadInt32(lParam);

                if (key == Keys.Enter)
                    StopWASD(true);
                else if (key == Keys.Escape)
                    StopWASD(false);
                else if (key == Keys.Q)
                    Q = IsKeyDown;
                else if (key == Keys.W)
                    W = IsKeyDown;
                else if (key == Keys.E)
                    E = IsKeyDown;
                else if (key == Keys.A)
                    A = IsKeyDown;
                else if (key == Keys.S)
                    S = IsKeyDown;
                else if (key == Keys.D)
                    D = IsKeyDown;
                else if (key == Keys.H)
                {
                    if (IsKeyDown)
                        Overlay.ShowImage(Properties.Resources.Info);
                    else
                        Overlay.ShowImage(null);
                }
                else if (key == Keys.LShiftKey || key == Keys.RShiftKey || key == Keys.Shift || key == Keys.ShiftKey)
                    Shift = IsKeyDown;
                else if (key == Keys.Left && IsKeyDown)
                    RhinoHelpers.PreviousNamedView();
                else if (key == Keys.Right && IsKeyDown)
                    RhinoHelpers.NextNamedView();
                else if (key == Keys.Oemplus && IsKeyDown)
                    RhinoHelpers.SaveNamedView();
                else if (key == Keys.PrintScreen)
                    return CallNextHookEx((IntPtr)0, nCode, wParam, lParam); // forward PrintScreen Key

                //RhinoApp.WriteLine("KEY:" + key);
            }
            else if (wParam == (IntPtr)WM_MOUSEMOVE)
            {
                NativeMethods.MSLLHOOKSTRUCT data = NativeMethods.GetData((IntPtr)lParam);
                Rhino.Display.RhinoViewport vp = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;
                MouseOffset = new System.Drawing.Point(data.pt.x - MidPoint.X, data.pt.y - MidPoint.Y);
            }
            else if (wParam == (IntPtr)WM_MOUSEWHEEL)
            {
                int delta = NativeMethods.GetDelta(lParam) / 120;
                if (speed < 0.01)
                    speed = 0.01;
                else if (speed > 1000000)
                    speed = 1000000;
                else if (delta < 0 && speed * 0.8 > 0.01)
                    speed *= 0.8;
                else if (delta > 0 && speed * 1.25 < 1000000)
                    speed *= 1.25;
                Properties.Settings.Default.Speed = speed;
                Properties.Settings.Default.Save();
                ShowSpeedMessage();
            }
            else if ((int)wParam >= WM_LBUTTONDOWN && (int)wParam <= WM_RBUTTONUP)
            {
                //RhinoApp.WriteLine("CLICK " + DateTime.Now.Ticks);
            }
            else
                return CallNextHookEx((IntPtr)0, nCode, wParam, lParam);


            return new IntPtr(-1);
        }

        [DllImport("user32.dll")]
        static extern int ShowCursor(bool bShow);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
