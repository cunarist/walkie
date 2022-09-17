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

        private static Timer timer;

        private static System.Drawing.Point lastCursorPosition = Cursor.Position;
        private static bool isWorkingOnCameraTarget = false;
        private static bool ignoreCameraTargetChange = false;
        public static Point3d desiredCameraTarget = new Point3d(0, 0, 0);

        public PlugIn() { Instance = this; }

        public static PlugIn Instance { get; private set; }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            timer = new Timer();
            timer.Interval = 1;
            timer.Tick += DetectUnofficialEvents;
            timer.Start();

            Rhino.ApplicationSettings.GeneralSettings.MiddleMouseMode = Rhino.ApplicationSettings.MiddleMouseMode.RunMacro;
            Rhino.ApplicationSettings.GeneralSettings.MiddleMouseMacro = "WASD";

            return LoadReturnCode.Success;
        }

        protected override void OnShutdown()
        {
            timer.Stop();
            timer.Tick -= DetectUnofficialEvents;
            timer.Dispose();
            timer = null;
        }

        private static void DetectUnofficialEvents(object sender, EventArgs args)
        {
            if (RhinoDoc.ActiveDoc == null) { return; }
            if (RhinoDoc.ActiveDoc.Views == null) { return; }
            if (RhinoDoc.ActiveDoc.Views.ActiveView == null) { return; }

            RhinoViewport vp = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;

            Point3d currentCameraTarget = vp.CameraTarget;
            if (!(currentCameraTarget.Equals(desiredCameraTarget)))
            {
                vp.SetCameraTarget(desiredCameraTarget, false);
                if (!ignoreCameraTargetChange)
                {
                    // When Rhino itself changed the camera target in a weird sense
                    RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
                }
                else
                {
                    ignoreCameraTargetChange = false;
                }
            }

            System.Drawing.Point currentCursorPosition = Cursor.Position;
            if (!(currentCursorPosition.Equals(lastCursorPosition)))
            {
                int viewWidth = vp.Size.Width;
                int viewHeight = vp.Size.Height;
                System.Drawing.Point cursorInView = vp.ScreenToClient(currentCursorPosition);
                int cursorX = cursorInView.X;
                int cursorY = cursorInView.Y;
                if (0 < cursorX && cursorX < viewWidth && 0 < cursorY && cursorY < viewHeight && !isWorkingOnCameraTarget)
                {
                    isWorkingOnCameraTarget = true;
                    System.Threading.Timer releaseTimer = null;
                    releaseTimer = new System.Threading.Timer((obj) =>
                    {
                        isWorkingOnCameraTarget = false;
                        releaseTimer.Dispose();
                    }, null, 1000, System.Threading.Timeout.Infinite);
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
                        desiredCameraTarget = nextCameraTaraget;
                        ignoreCameraTargetChange = true;
                    }
                    isWorkingOnCameraTarget = false;
                    releaseTimer.Dispose();
                }
            }
            lastCursorPosition = currentCursorPosition;
        }
    }
}