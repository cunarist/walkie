using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.PlugIns;
using Rhino.UI;
using System.Threading.Tasks;

namespace RhinoWASD
{
    public class PlugIn : Rhino.PlugIns.PlugIn
    {
        public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;
        private static Point3d lastCursorWorldPosition = new Point3d(0, 0, 0);

        public PlugIn() { Instance = this; }

        public static PlugIn Instance { get; private set; }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            Rhino.ApplicationSettings.GeneralSettings.MiddleMouseMode = Rhino.ApplicationSettings.MiddleMouseMode.RunMacro;
            Rhino.ApplicationSettings.GeneralSettings.MiddleMouseMacro = "Walk";
            SetCursorZoomDepth();

            return LoadReturnCode.Success;
        }

        protected override void OnShutdown()
        {
        }

        async private static void SetCursorZoomDepth()
        {
            while (true)
            {
                if (RhinoDoc.ActiveDoc == null) { await Task.Delay(10); continue; }
                if (RhinoDoc.ActiveDoc.Objects == null) { await Task.Delay(10); continue; }
                if (RhinoDoc.ActiveDoc.Views == null) { await Task.Delay(10); continue; }
                if (RhinoDoc.ActiveDoc.Views.ActiveView == null) { await Task.Delay(10); continue; }
                if (RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport == null) { await Task.Delay(10); continue; }

                RhinoViewport vp = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;

                Point2d cursorInScreen = MouseCursor.Location;
                int cursorInScreenX = (int)cursorInScreen.X;
                int cursorInScreenY = (int)cursorInScreen.Y;
                System.Drawing.Point cursorInSystemScreen = new System.Drawing.Point(cursorInScreenX, cursorInScreenY);
                System.Drawing.Point cursorInView = vp.ScreenToClient(cursorInSystemScreen);
                int cursorX = cursorInView.X;
                int cursorY = cursorInView.Y;
                int viewWidth = vp.Size.Width;
                int viewHeight = vp.Size.Height;

                if (!(0 < cursorX && cursorX < viewWidth) || !(0 < cursorY && cursorY < viewHeight)) { await Task.Delay(10); continue; }

                using (ZBufferCapture depthBuffer = new ZBufferCapture(vp))
                {
                    Point3d currentCursorWorldPosition = depthBuffer.WorldPointAt(cursorX, cursorY);

                    if (currentCursorWorldPosition != lastCursorWorldPosition)
                    {
                        lastCursorWorldPosition = currentCursorWorldPosition;
                        vp.SetCameraTarget(currentCursorWorldPosition, false);
                        RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
                    }
                }

                await Task.Delay(10);
            }
        }
    }
}