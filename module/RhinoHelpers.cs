using Display;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using System;

namespace RhinoWASD
{
    public static class RhinoHelpers
    {
        private static void RestoreNamedView(int index, bool showMessage = true)
        {
            if (index < 0 || index >= RhinoDoc.ActiveDoc.NamedViews.Count)
                return;

            RhinoDoc.ActiveDoc.NamedViews.Restore(index, RhinoDoc.ActiveDoc.Views.ActiveView.MainViewport);
            string currentNamedView = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.Name;

            if (showMessage) { Overlay.ShowMessage("Named view \"" + currentNamedView + "\" restored"); }
        }

        private static void DisableNamedView(bool showMessage = true)
        {
            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.Name = "Perspective";
            if (showMessage) { Overlay.ShowMessage("Named view disabled"); }
        }

        public static void PreviousNamedView()
        {
            int count = RhinoDoc.ActiveDoc.NamedViews.Count;
            if (count < 1)
                return;

            string currentNamedView = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.Name;
            int cur = RhinoDoc.ActiveDoc.NamedViews.FindByName(currentNamedView);
            int newCur = cur - 1;
            if (cur == -1) { newCur = count - 1; }

            if (newCur >= 0 && newCur < count)
            {
                RestoreNamedView(newCur);
            }
            else
            {
                DisableNamedView();
            }
        }

        public static void NextNamedView()
        {
            int count = RhinoDoc.ActiveDoc.NamedViews.Count;
            if (count < 1)
                return;

            string currentNamedView = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.Name;
            int cur = RhinoDoc.ActiveDoc.NamedViews.FindByName(currentNamedView);
            int newCur = cur + 1;
            if (cur == -1) { newCur = 0; }

            if (newCur >= 0 && newCur < count)
            {
                RestoreNamedView(newCur);
            }
            else
            {
                DisableNamedView();
            }
        }

        public static void SaveNamedView()
        {
            string name = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            int result = RhinoDoc.ActiveDoc.NamedViews.Add(name, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewportID);
            if (result >= 0)
            {
                Overlay.ShowMessage("Named view \"" + name + "\" saved");
                RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.Name = name;
            }
            else
            {
                Overlay.ShowMessage("Named view cannot be saved");
            }
        }

        public static void DeleteNamedView()
        {
            int count = RhinoDoc.ActiveDoc.NamedViews.Count;
            if (count < 1)
                return;

            string currentNamedView = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.Name;
            int cur = RhinoDoc.ActiveDoc.NamedViews.FindByName(currentNamedView);
            int newCur = cur - 1;

            bool shouldAlternate = false;
            if (newCur >= 0 && newCur < count)
            {
                shouldAlternate = true;
            }

            bool result = RhinoDoc.ActiveDoc.NamedViews.Delete(currentNamedView);
            if (result)
            {
                Overlay.ShowMessage("Named view \"" + currentNamedView + "\" deleted");
                if (shouldAlternate)
                {
                    RestoreNamedView(newCur, false);
                }
                else
                {
                    DisableNamedView(false);
                }
            }
            else
            {
                Overlay.ShowMessage("Named view cannot be deleted");
            }
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
    }
}
