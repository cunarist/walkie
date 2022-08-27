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
        public static Point3d desiredCameraTarget = new Point3d(0, 0, 0);

        public PlugIn() { Instance = this; }

        public static PlugIn Instance { get; private set; }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            timer = new Timer();
            timer.Interval = 1;
            timer.Tick += DetectUnofficialEvents;
            timer.Start();

            Rhino.ApplicationSettings.GeneralSettings.MiddleMouseMode = (Rhino.ApplicationSettings.MiddleMouseMode)2;
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
            if (Rhino.RhinoDoc.ActiveDoc == null)
                return;
            if (Rhino.RhinoDoc.ActiveDoc.Views == null)
                return;
            if (Rhino.RhinoDoc.ActiveDoc.Views.ActiveView == null)
                return;

            RhinoViewport vp = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;

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
                    System.Drawing.Point cursorPositionInView = vp.ScreenToClient(Cursor.Position);
                    Point3d cameraLocation = vp.CameraLocation;
                    Line viewLine = vp.ClientToWorld(cursorPositionInView);
                    viewLine.Extend(1E8, 0);
                    Point3d nextCameraTaraget = viewLine.From;
                    foreach (RhinoObject eachObject in Rhino.RhinoDoc.ActiveDoc.Objects)
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
                                }
                            }
                        }
                    }
                    desiredCameraTarget = nextCameraTaraget;
                }
            }
            lastCursorPosition = currentCursorPosition;

            Point3d currentCameraTarget = vp.CameraTarget;
            if (!(currentCameraTarget.Equals(desiredCameraTarget)))
            {
                vp.SetCameraTarget(desiredCameraTarget, false);
            }
        }
    }
}