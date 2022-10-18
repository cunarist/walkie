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

        private static string lastFilePath = "";
        private static System.Drawing.Point lastCursorPosition = Cursor.Position;

        public static bool setDepthEnabled = false;
        private static double setDepthDurationRecord = 0;
        private static double setDepthWaitUntil = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

        public PlugIn() { Instance = this; }

        public static PlugIn Instance { get; private set; }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            timer = new System.Threading.Timer(DetectUnofficialEvents, null, 0, 10);

            Rhino.ApplicationSettings.GeneralSettings.MiddleMouseMode = Rhino.ApplicationSettings.MiddleMouseMode.RunMacro;
            Rhino.ApplicationSettings.GeneralSettings.MiddleMouseMacro = "Walk";

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
            if (RhinoDoc.ActiveDoc.Path == null) { return; }

            System.Drawing.Point currentCursorPosition = Cursor.Position;
            if (!(currentCursorPosition.Equals(lastCursorPosition))) { SetCursorZoomDepth(); }
            lastCursorPosition = currentCursorPosition;

            string currentFilePath = RhinoDoc.ActiveDoc.Path;
            if (lastFilePath != currentFilePath) { setDepthEnabled = true; }
            lastFilePath = currentFilePath;
        }

        private static void SetCursorZoomDepth()
        {
            if (RhinoDoc.ActiveDoc == null) { return; }
            if (RhinoDoc.ActiveDoc.Objects == null) { return; }
            if (RhinoDoc.ActiveDoc.Views == null) { return; }
            if (RhinoDoc.ActiveDoc.Views.ActiveView == null) { return; }

            if (!setDepthEnabled) { return; }
            double startTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            if (startTime < setDepthWaitUntil) { return; }
            setDepthWaitUntil = startTime + 1 * 1000;

            RhinoViewport vp = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;
            System.Drawing.Point currentCursorPosition = Cursor.Position;

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

            double endTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            double duration = endTime - startTime;
            setDepthDurationRecord = setDepthDurationRecord * 0.9 + duration * 0.1;

            if (setDepthDurationRecord < 100) { setDepthWaitUntil = endTime; }
            else
            {
                setDepthEnabled = false;
                setDepthDurationRecord = 0;
                RhinoApp.WriteLine("This scene is too heavy. Walkie's cursor zoom depth feature is disabled.");
            }
        }
    }
}