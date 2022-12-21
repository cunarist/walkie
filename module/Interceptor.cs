using Display;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Runtime.InteropServices;
using System.Threading;
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
        private const double MIN_SPEED = 1E-2;
        private const double MAX_SPEED = 1E6;


        private static LowLevelProc _proc = HookCallback;
        private static IntPtr _kHook = IntPtr.Zero, _mHook = IntPtr.Zero;
        private static System.Windows.Forms.Timer timer;
        private static Point3d BeforeLocation;
        private static Vector3d BeforeDirection;
        private static bool shouldUseAimpoint = false;
        private static double speed = 1;
        private static System.Drawing.Point CursorPositionBuffer = System.Drawing.Point.Empty;
        private static System.Drawing.Rectangle ScreenRect;

        private static Thread distanceFinder = null;
        private static double frontDistance = 0;
        private static double bottomDistance = 0;

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

        public static bool gravity = false;

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
            _kHook = SetHook(_proc, true);
            _mHook = SetHook(_proc, false);
            ShowCursor(false);

            RhinoViewport vp = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;

            CursorPositionBuffer = new System.Drawing.Point(Cursor.Position.X, Cursor.Position.Y);

            speed = vp.CameraLocation.DistanceTo(vp.CameraTarget) / 100;
            speed = Math.Max(MIN_SPEED, speed);
            speed = Math.Min(MAX_SPEED, speed);

            ScreenRect = Screen.PrimaryScreen.Bounds;
            Cursor.Position = MidPoint;

            BeforeLocation = vp.CameraLocation;
            BeforeDirection = new Vector3d(vp.CameraDirection);

            Q = W = E = S = A = D = Shift = Esc = Enter = false;
            if (timer == null)
            {
                timer = new System.Windows.Forms.Timer();
                timer.Interval = TIMER_INVERVAL;
                timer.Tick += OnTick;
                timer.Start();
            }

            Aimpoint.ShowImage(true);
            shouldUseAimpoint = true;

            void keepFindingDistance()
            {
                while (true)
                {
                    (frontDistance, bottomDistance) = RhinoHelpers.GetPhysicalDistance();
                    Thread.Sleep(100);
                }
            }

            distanceFinder = new Thread(keepFindingDistance);
            distanceFinder.Start();

            ShowSpeedMessage();
        }

        public static void StopWASD(bool ShouldKeepView)
        {
            RhinoViewport vp = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;

            distanceFinder.Abort();
            distanceFinder = null;

            if (ShouldKeepView)
            {
                if (shouldUseAimpoint)
                {
                    RhinoHelpers.SetAimpointZoomDepth(0.5, 0.5);
                    int viewWidth = vp.Size.Width;
                    int viewHeight = vp.Size.Height;
                    Cursor.Position = vp.ClientToScreen(new Point2d(viewWidth / 2, viewHeight / 2));
                }
            }
            else
            {
                vp.SetCameraDirection(BeforeDirection, false);
                vp.SetCameraLocation(BeforeLocation, false);
                Point3d newTarget = vp.CameraLocation + vp.CameraDirection * (speed * 100);
                vp.SetCameraTarget(newTarget, false);
                RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
                Cursor.Position = CursorPositionBuffer;
            }

            Aimpoint.ShowImage(false);
            shouldUseAimpoint = false;
            gravity = false;

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
            ShowCursor(true);

            W = A = S = D = Q = E = Shift = Esc = Enter = false;
        }

        private static void OnTick(object sender, EventArgs args)
        {
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

            if (Shift) { finalSpeed *= 4; }

            double bottomSpeedAddition = 0;
            if (bottomDistance != 0 && gravity)
            {
                double unitSize = 1;
                UnitSystem unitSystem = RhinoDoc.ActiveDoc.ModelUnitSystem;
                if (unitSystem == UnitSystem.Meters) { unitSize = 1; }
                else if (unitSystem == UnitSystem.Nanometers) { unitSize = 1E-9; }
                else if (unitSystem == UnitSystem.Microns) { unitSize = 1E-6; }
                else if (unitSystem == UnitSystem.Millimeters) { unitSize = 1E-3; }
                else if (unitSystem == UnitSystem.Centimeters) { unitSize = 1E-2; }
                else if (unitSystem == UnitSystem.Decimeters) { unitSize = 1E-1; }
                else if (unitSystem == UnitSystem.Dekameters) { unitSize = 1E1; }
                else if (unitSystem == UnitSystem.Hectometers) { unitSize = 1E2; }
                else if (unitSystem == UnitSystem.Kilometers) { unitSize = 1E3; }
                else if (unitSystem == UnitSystem.Megameters) { unitSize = 1E6; }
                else if (unitSystem == UnitSystem.Gigameters) { unitSize = 1E9; }
                else if (unitSystem == UnitSystem.Inches) { unitSize = 25.4 / 1000; }
                else if (unitSystem == UnitSystem.Feet) { unitSize = 304.8 / 1000; }
                else if (unitSystem == UnitSystem.Yards) { unitSize = 304.8 / 1000; }
                else if (unitSystem == UnitSystem.Miles) { unitSize = 1609.34; }
                double bottomDistanceInMeters = bottomDistance * unitSize;

                double bottomThresholdInMeters = 1.8;
                bottomSpeedAddition = Math.Pow(Math.Min(bottomDistanceInMeters - bottomThresholdInMeters, 3) * 0.2, 3) / unitSize * 3;
            }

            double sideSpeedDrag = 0;
            if (frontDistance != 0 && gravity)
            {
                double frontThreshold = speed * 0.1;
                if (frontThreshold < frontDistance)
                { sideSpeedDrag = Math.Min(0.99, speed / (frontDistance - frontThreshold) * 10); }
            }

            Vector3d movement = new Vector3d(0, 0, 0);
            if (W || A || S || D || Q || E)
            {
                if (shouldUseAimpoint)
                {
                    Aimpoint.ShowImage(false);
                    shouldUseAimpoint = false;
                }

                if (W) { movement += dir * finalSpeed; }
                if (A) { movement -= Vector3d.CrossProduct(dir, up) * finalSpeed; }
                if (S) { movement -= dir * finalSpeed; }
                if (D) { movement += Vector3d.CrossProduct(dir, up) * finalSpeed; }

                if (gravity) { movement.Z = 0; }

                if (Q) { movement -= Vector3d.ZAxis * finalSpeed; }
                if (E) { movement += Vector3d.ZAxis * finalSpeed; }

            }
            movement.X *= (1 - sideSpeedDrag);
            movement.Y *= (1 - sideSpeedDrag);
            movement.Z -= bottomSpeedAddition;

            loc += movement;
            vp.SetCameraLocation(loc, false);

            Point3d newTarget = vp.CameraLocation + vp.CameraDirection * (speed * 100);
            vp.SetCameraTarget(newTarget, false);
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

            if (wParam == (IntPtr)WM_LBUTTONUP)
                StopWASD(true);
            else if (wParam == (IntPtr)WM_RBUTTONUP)
                StopWASD(false);
            else if (wParam == (IntPtr)WM_MBUTTONDOWN)
                StopWASD(false);

            if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_KEYUP)
            {
                bool IsKeyDown = wParam == (IntPtr)WM_KEYDOWN;
                Keys key = (Keys)Marshal.ReadInt32(lParam);

                if (key == Keys.W)
                    W = IsKeyDown;
                else if (key == Keys.A)
                    A = IsKeyDown;
                else if (key == Keys.S)
                    S = IsKeyDown;
                else if (key == Keys.D)
                    D = IsKeyDown;
                else if (key == Keys.Q)
                    Q = IsKeyDown;
                else if (key == Keys.E)
                    E = IsKeyDown;
                else if (key == Keys.LShiftKey || key == Keys.RShiftKey || key == Keys.Shift || key == Keys.ShiftKey)
                    Shift = IsKeyDown;
                else if (key == Keys.H)
                {
                    if (IsKeyDown)
                        Overlay.ShowImage(Properties.Resources.help);
                    else
                        Overlay.ShowImage(null);
                }
                else if (key == Keys.Enter && !IsKeyDown)
                    StopWASD(true);
                else if (key == Keys.Escape && !IsKeyDown)
                    StopWASD(false);
                else if (key == Keys.Z && IsKeyDown)
                {
                    if (gravity)
                    {
                        gravity = false;
                        Overlay.ShowMessage("Gravity disabled");
                    }
                    else
                    {
                        gravity = true;
                        Overlay.ShowMessage("Gravity enabled");
                    }
                }
                else if (key == Keys.Left && IsKeyDown)
                    RhinoHelpers.PreviousNamedView();
                else if (key == Keys.Right && IsKeyDown)
                    RhinoHelpers.NextNamedView();
                else if (key == Keys.Oemplus && IsKeyDown)
                    RhinoHelpers.SaveNamedView();
                else if (key == Keys.PrintScreen)
                    return CallNextHookEx((IntPtr)0, nCode, wParam, lParam);
            }
            else if (wParam == (IntPtr)WM_MOUSEMOVE)
            {
                NativeMethods.MSLLHOOKSTRUCT data = NativeMethods.GetData((IntPtr)lParam);
                MouseOffset = new System.Drawing.Point(data.pt.x - MidPoint.X, data.pt.y - MidPoint.Y);
            }
            else if (wParam == (IntPtr)WM_MOUSEWHEEL)
            {
                RhinoViewport vp = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;

                int delta = NativeMethods.GetDelta(lParam) / 120;
                if (speed < MIN_SPEED)
                    speed = MIN_SPEED;
                else if (speed > MAX_SPEED)
                    speed = MAX_SPEED;
                else if (delta < 0 && speed * 0.8 > MIN_SPEED)
                    speed *= 0.8;
                else if (delta > 0 && speed * 1.25 < MAX_SPEED)
                    speed *= 1.25;
                ShowSpeedMessage();

                Point3d newTarget = vp.CameraLocation + vp.CameraDirection * (speed * 100);
                vp.SetCameraTarget(newTarget, false);
                RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
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
