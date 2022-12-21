using Display;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace RhinoWASD
{
    public static class RhinoHelpers
    {
        public static string CurrentNamedView = string.Empty;

        private static void RestoreNamedView(int index)
        {
            if (index < 0 || index >= RhinoDoc.ActiveDoc.NamedViews.Count)
                return;

            RhinoDoc.ActiveDoc.NamedViews.Restore(index, RhinoDoc.ActiveDoc.Views.ActiveView.MainViewport);
            CurrentNamedView = RhinoDoc.ActiveDoc.NamedViews[index].Name;
            Overlay.ShowMessage(CurrentNamedView);
        }

        public static void PreviousNamedView()
        {
            int count = RhinoDoc.ActiveDoc.NamedViews.Count;
            if (count < 1)
                return;

            if (!string.IsNullOrEmpty(CurrentNamedView))
            {
                int cur = RhinoDoc.ActiveDoc.NamedViews.FindByName(CurrentNamedView);
                int newCur = cur - 1;
                if (newCur >= 0 && newCur < count)
                {
                    RestoreNamedView(newCur);
                }
            }
        }

        public static void NextNamedView()
        {
            int count = RhinoDoc.ActiveDoc.NamedViews.Count;
            if (count < 1)
                return;

            if (!string.IsNullOrEmpty(CurrentNamedView))
            {
                int cur = RhinoDoc.ActiveDoc.NamedViews.FindByName(CurrentNamedView);
                int newCur = cur + 1;
                if (newCur >= 0 && newCur < count)
                {
                    RestoreNamedView(newCur);
                }
            }
        }

        public static void SaveNamedView()
        {
            string name = Environment.UserName + " " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (RhinoDoc.ActiveDoc.NamedViews.Add(name, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewportID) >= 0)
            {
                Overlay.ShowMessage("\"" + name + "\" saved as named view");
                CurrentNamedView = name;
            }
            else
                Overlay.ShowMessage("Couln't save view \"" + name + "\"");
        }

        public static void SetAimpointZoomDepth(double widthRatio, double heightRatio)
        {
            RhinoViewport vp = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;
            int viewWidth = vp.Size.Width;
            int viewHeight = vp.Size.Height;

            using (ZBufferCapture depthBuffer = new ZBufferCapture(vp))
            {
                Point3d currentCursorWorldPosition = depthBuffer.WorldPointAt(
                    (int)Math.Round(viewWidth * widthRatio),
                    (int)Math.Round(viewHeight * heightRatio)
                );
                vp.SetCameraTarget(currentCursorWorldPosition, false);
                RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
            }

            Overlay.ShowMessage("Zoom depth set on the aimpoint");
        }

        public static (double, double) GetPhysicalDistance()
        {
            double startTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            RhinoViewport vp = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;

            int viewWidth = vp.Size.Width;
            int viewHeight = vp.Size.Height;

            Point3d cameraLocation = vp.CameraLocation;
            System.Drawing.Point midPoint = new System.Drawing.Point(viewWidth / 2, viewHeight / 2);
            Line viewLineReversed = vp.ClientToWorld(midPoint);
            Line viewLine = new Line(viewLineReversed.To, viewLineReversed.From);
            viewLine.Extend(0, 1E6);
            Vector3d viewDirection = viewLine.Direction;

            Vector3d frontDirection = new Vector3d(viewDirection.X, viewDirection.Y, 0);
            frontDirection *= 1E8 / frontDirection.Length;
            Line lineToFront = new Line(viewLine.From, viewLine.From + frontDirection);
            Vector3d bottomDirection = new Vector3d(0, 0, -1);
            bottomDirection *= 1E8 / bottomDirection.Length;
            Line lineToBottom = new Line(viewLine.From, viewLine.From + bottomDirection);

            List<Line> intersectionLines = new List<Line> { lineToFront, lineToBottom };

            List<double> intersectionDistance = new List<double>();
            foreach (Line intersectionLine in intersectionLines)
            {
                bool didFind = false;

                Point3d nextIntersectionPoint = intersectionLine.To;
                foreach (RhinoObject eachObject in RhinoDoc.ActiveDoc.Objects)
                {
                    MeshType blankMesh = new MeshType();
                    Mesh[] meshes = eachObject.GetMeshes(blankMesh);
                    foreach (Mesh mesh in meshes)
                    {
                        int[] faceIds;
                        Point3d[] pinPoints = Rhino.Geometry.Intersect.Intersection.MeshLine(mesh, intersectionLine, out faceIds);
                        foreach (Point3d pinPoint in pinPoints)
                        {
                            double beforeDistance = nextIntersectionPoint.DistanceTo(cameraLocation);
                            double newDistance = pinPoint.DistanceTo(cameraLocation);
                            if (newDistance < beforeDistance)
                            {
                                nextIntersectionPoint = pinPoint;
                                didFind = true;
                            }
                        }
                    }
                }
                if (didFind)
                {
                    double distance = nextIntersectionPoint.DistanceTo(cameraLocation);
                    intersectionDistance.Add(distance);
                }
                else
                {
                    intersectionDistance.Add(0);
                }
            }

            double endTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            double duration = endTime - startTime;

            if (duration > 1000)
            {
                intersectionDistance[0] = 0;
                intersectionDistance[1] = 0;
            }

            return (intersectionDistance[0], intersectionDistance[1]);
        }
    }
}
