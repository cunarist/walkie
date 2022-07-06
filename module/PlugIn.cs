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

        private static Timer timer;
        private static System.Drawing.Point cursorPosition = Cursor.Position;

        public PlugIn() { Instance = this; }

        public static PlugIn Instance { get; private set; }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            timer = new Timer();
            timer.Interval = 1;
            timer.Tick += DetectMouseMove;
            timer.Start();

            Rhino.ApplicationSettings.GeneralSettings.MiddleMouseMode = (Rhino.ApplicationSettings.MiddleMouseMode)2;
            Rhino.ApplicationSettings.GeneralSettings.MiddleMouseMacro = "WASD";

            return LoadReturnCode.Success;
        }

        protected override void OnShutdown()
        {
            timer.Stop();
            timer.Tick -= DetectMouseMove;
            timer.Dispose();
            timer = null;
        }

        private static void DetectMouseMove(object sender, EventArgs args)
        {
            System.Drawing.Point newPosition = Cursor.Position;
            if (newPosition.X != cursorPosition.X || newPosition.Y != cursorPosition.Y)
            {
                RhinoView view = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView;
                int viewWidth = view.ActiveViewport.Size.Width;
                int viewHeight = view.ActiveViewport.Size.Height;
                System.Drawing.Point cursorInView = view.ActiveViewport.ScreenToClient(newPosition);
                int cursorX = cursorInView.X;
                int cursorY = cursorInView.Y;
                if (0 < cursorX && cursorX < viewWidth && 0 < cursorY && cursorY < viewHeight)
                    HandleMouseMove();
            }
            cursorPosition = newPosition;
        }

        private static void HandleMouseMove()
        {
            RhinoView view = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView;
            if (view == null)
                return;
            System.Drawing.Point cursorInView = view.ActiveViewport.ScreenToClient(Cursor.Position);
            Point3d cameraLocation = view.ActiveViewport.CameraLocation;
            Line viewLine = view.ActiveViewport.ClientToWorld(cursorInView);
            viewLine.Extend(1E8, 0);
            Point3d zoomTarget = viewLine.From;
            bool didSetTarget = false;
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
                        double beforeDistance = zoomTarget.DistanceTo(cameraLocation);
                        double newDistance = pinPoint.DistanceTo(cameraLocation);
                        if (newDistance < beforeDistance)
                        {
                            zoomTarget = pinPoint;
                            didSetTarget = true;
                        }
                    }
                }
            }
            if (didSetTarget)
            {
                view.ActiveViewport.SetCameraTarget(zoomTarget, false);
            }
        }
    }
}