using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.PlugIns;
using System;
using System.Windows.Forms;

namespace RhinoWASD
{
    public class PlugIn : Rhino.PlugIns.PlugIn
    {
        public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;

        private static System.Threading.Timer timer;

        private static System.Drawing.Point lastCursorPosition = Cursor.Position;
        private static double waitUntil = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

        public PlugIn() { Instance = this; }

        public static PlugIn Instance { get; private set; }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            timer = new System.Threading.Timer(DetectUnofficialEvents, null, 0, 10);

            Rhino.ApplicationSettings.GeneralSettings.MiddleMouseMode = Rhino.ApplicationSettings.MiddleMouseMode.RunMacro;
            Rhino.ApplicationSettings.GeneralSettings.MiddleMouseMacro = "WASD";

            return LoadReturnCode.Success;
        }

        protected override void OnShutdown()
        {
            timer.Dispose();
            timer = null;
        }

        private static void DetectUnofficialEvents(object state)
        {
            if (RhinoDoc.ActiveDoc == null) { return; }
            if (RhinoDoc.ActiveDoc.Objects == null) { return; }
            if (RhinoDoc.ActiveDoc.Views == null) { return; }
            if (RhinoDoc.ActiveDoc.Views.ActiveView == null) { return; }

            double startTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            if (startTime < waitUntil) { return; }
            waitUntil = startTime + 60 * 1000;

            RhinoViewport vp = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;

            System.Drawing.Point currentCursorPosition = Cursor.Position;
            if (!(currentCursorPosition.Equals(lastCursorPosition)))
            {
                int viewWidth = vp.Size.Width;
                int viewHeight = vp.Size.Height;
                System.Drawing.Point cursorInView = vp.ScreenToClient(currentCursorPosition);
                int cursorX = cursorInView.X;
                int cursorY = cursorInView.Y;
                if (0 < cursorX && cursorX < viewWidth && 0 < cursorY && cursorY < viewHeight)
                {
                    bool didSetTarget = false;
                    System.Drawing.Point cursorPositionInView = vp.ScreenToClient(Cursor.Position);
                    Point3d cameraLocation = vp.CameraLocation;
                    Line viewLine = vp.ClientToWorld(cursorPositionInView);
                    viewLine.Extend(1E8, 0);
                    Point3d nextCameraTaraget = viewLine.From;
                    foreach (RhinoObject eachObject in RhinoDoc.ActiveDoc.Objects)
                    {
                        MeshType blankMesh = new MeshType();
                        Mesh[] meshes = eachObject.GetMeshes(blankMesh);
                        foreach (Mesh mesh in meshes)
                        {
                            int[] faceIds;
                            Point3d[] pinPoints = Rhino.Geometry.Intersect.Intersection.MeshLine(mesh, viewLine, out faceIds);
                            foreach (Point3d pinPoint in pinPoints)
                            {
                                double beforeDistance = nextCameraTaraget.DistanceTo(cameraLocation);
                                double newDistance = pinPoint.DistanceTo(cameraLocation);
                                if (newDistance < beforeDistance)
                                {
                                    nextCameraTaraget = pinPoint;
                                    didSetTarget = true;
                                }
                            }
                        }
                    }
                    if (didSetTarget)
                    {
                        vp.SetCameraTarget(nextCameraTaraget, false);
                        RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
                    }
                }
            }
            lastCursorPosition = currentCursorPosition;

            double endTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            double duration = endTime - startTime;
            if (duration < 100)
            {
                waitUntil = endTime;
            }
        }
    }
}