using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.PlugIns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace RhinoWASD
{
    public class PlugIn : Rhino.PlugIns.PlugIn
    {
        public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;
        private static System.Threading.Timer timer;
        private static bool didNotifyCursorDepthPerformance = false;

        public PlugIn() { Instance = this; }

        public static PlugIn Instance { get; private set; }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            timer = new System.Threading.Timer((s) => { SetZoomDepth(); }, null, 0, 250);

            Rhino.ApplicationSettings.GeneralSettings.MiddleMouseMode = Rhino.ApplicationSettings.MiddleMouseMode.RunMacro;
            Rhino.ApplicationSettings.GeneralSettings.MiddleMouseMacro = "Walk";

            RhinoDoc.EndOpenDocumentInitialViewUpdate += (o, e) => { didNotifyCursorDepthPerformance = false; };

            return LoadReturnCode.Success;
        }

        protected override void OnShutdown()
        {
            timer.Dispose();
        }

        private static void SetZoomDepth()
        {
            if (RhinoDoc.ActiveDoc == null) { return; }
            if (RhinoDoc.ActiveDoc.Objects == null) { return; }
            if (RhinoDoc.ActiveDoc.Views == null) { return; }
            if (RhinoDoc.ActiveDoc.Views.ActiveView == null) { return; }

            double startTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            RhinoViewport vp = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;
            System.Drawing.Point currentCursorPosition = Cursor.Position;

            int viewWidth = vp.Size.Width;
            int viewHeight = vp.Size.Height;
            System.Drawing.Point cursorInView = vp.ScreenToClient(currentCursorPosition);
            int cursorX = cursorInView.X;
            int cursorY = cursorInView.Y;

            if (!(0 < cursorX && cursorX < viewWidth) || !(0 < cursorY && cursorY < viewHeight)) { return; }

            List<RhinoObject> selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(true, true).ToList();

            if (selectedObjects.Count() == 0)
            {
                bool didFindCursorDepth = false;
                bool didLookAllObjects = true;
                System.Drawing.Point cursorPositionInView = vp.ScreenToClient(Cursor.Position);
                Point3d cameraLocation = vp.CameraLocation;

                Line viewLine = vp.ClientToWorld(cursorPositionInView);
                viewLine.Extend(1E8, 0);
                Point3d nextCameraTaraget = viewLine.From;

                foreach (RhinoObject eachObject in RhinoDoc.ActiveDoc.Objects)
                {
                    if (eachObject == null) { continue; }
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
                                didFindCursorDepth = true;
                            }
                        }
                    }

                    double nowTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                    double middleDuration = nowTime - startTime;
                    if (25 < middleDuration)
                    {
                        didLookAllObjects = false;
                        if (!didNotifyCursorDepthPerformance)
                        {
                            RhinoApp.WriteLine("Walkie's cursor zoom depth feature might not work if the document is too heavy.");
                            didNotifyCursorDepthPerformance = true;
                        }
                        break;
                    }
                }

                if (didFindCursorDepth && didLookAllObjects)
                {
                    vp.SetCameraTarget(nextCameraTaraget, false);
                    RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
                }
            }
            else
            {
                List<Point3d> centers = new List<Point3d>();
                foreach (RhinoObject selectedObject in selectedObjects)
                {
                    BoundingBox boundingBox;
                    List<RhinoObject> singleList = new List<RhinoObject>();
                    singleList.Add(selectedObject);
                    RhinoObject.GetTightBoundingBox(singleList, out boundingBox);
                    centers.Add(boundingBox.Center);
                    double nowTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                    double middleDuration = nowTime - startTime;
                    if (25 < middleDuration)
                    {
                        break;
                    }
                }

                Point3d averageCenter = new Point3d(
                    centers.Average(p => p.X),
                    centers.Average(p => p.Y),
                    centers.Average(p => p.Z)
                );

                vp.SetCameraTarget(averageCenter, false);
                RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();

            }
        }
    }
}