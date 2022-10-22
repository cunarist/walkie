using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.PlugIns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace RhinoWASD
{
    public class PlugIn : Rhino.PlugIns.PlugIn
    {
        public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;

        private static System.Threading.Timer timer;

        private static System.Drawing.Point lastCursorPosition = Cursor.Position;

        private static bool didNotifyPerformance = false;
        private static double setDepthWaitUntil = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

        public PlugIn() { Instance = this; }

        public static PlugIn Instance { get; private set; }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            timer = new System.Threading.Timer(DetectUnofficialEvents, null, 0, 1);

            Rhino.ApplicationSettings.GeneralSettings.MiddleMouseMode = Rhino.ApplicationSettings.MiddleMouseMode.RunMacro;
            Rhino.ApplicationSettings.GeneralSettings.MiddleMouseMacro = "Walk";

            RhinoDoc.SelectObjects += (o, e) => new Thread(SetSelectionZoomDepth).Start();
            RhinoDoc.ActiveDocumentChanged += (o, e) => new Thread(() => didNotifyPerformance = false).Start();

            return LoadReturnCode.Success;
        }

        protected override void OnShutdown()
        {
            timer.Dispose();
        }

        private static void DetectUnofficialEvents(object state)
        {
            System.Drawing.Point currentCursorPosition = Cursor.Position;
            if (!(currentCursorPosition.Equals(lastCursorPosition))) { SetCursorZoomDepth(); }
            lastCursorPosition = currentCursorPosition;
        }

        private static void SetCursorZoomDepth()
        {
            if (RhinoDoc.ActiveDoc == null) { return; }
            if (RhinoDoc.ActiveDoc.Objects == null) { return; }
            if (RhinoDoc.ActiveDoc.Views == null) { return; }
            if (RhinoDoc.ActiveDoc.Views.ActiveView == null) { return; }

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
                bool didAll = true;
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
                    double nowTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                    double middleDuration = nowTime - startTime;
                    if (50 < middleDuration)
                    {
                        didAll = false;
                        if (!didNotifyPerformance)
                        {
                            RhinoApp.WriteLine("Walkie's cursor zoom depth feature might not work if the document is too heavy.");
                            didNotifyPerformance = true;
                        }
                        break;
                    }
                }
                if (didSetTarget && didAll)
                {
                    vp.SetCameraTarget(nextCameraTaraget, false);
                    RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
                }
            }

            double endTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            setDepthWaitUntil = endTime;
        }

        private static void SetSelectionZoomDepth()
        {
            if (RhinoDoc.ActiveDoc == null) { return; }
            if (RhinoDoc.ActiveDoc.Objects == null) { return; }
            if (RhinoDoc.ActiveDoc.Views == null) { return; }
            if (RhinoDoc.ActiveDoc.Views.ActiveView == null) { return; }

            double startTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            List<RhinoObject> selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(true, true).ToList();
            if (selectedObjects.Count() == 0) { return; }

            List<Point3d> centers = new List<Point3d>();

            foreach (RhinoObject selectedObject in selectedObjects)
            {
                BoundingBox boundingBox;
                int getCount = Math.Min(selectedObjects.Count(), 10);
                RhinoObject.GetTightBoundingBox(selectedObjects.GetRange(0, getCount), out boundingBox);
                centers.Add(boundingBox.Center);
                double nowTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                double middleDuration = nowTime - startTime;
                if (50 < middleDuration)
                {
                    break;
                }
            }

            Point3d averageCenter = new Point3d(
                centers.Average(p => p.X),
                centers.Average(p => p.Y),
                centers.Average(p => p.Z)
            );

            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraTarget(averageCenter, false);
            RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
        }
    }
}