using Display;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using System;

namespace RhinoWASD
{
    public static class RhinoHelpers
    {
        public static string CurrentNamedView = string.Empty;

        private static bool IsNamedView(RhinoViewport vp, ViewportInfo info)
        {
            if (
                Math.Abs(vp.Camera35mmLensLength - info.Camera35mmLensLength) < 1 &&
                vp.CameraDirection == info.CameraDirection &&
                vp.CameraLocation == info.CameraLocation &&
                vp.CameraUp == info.CameraUp &&
                vp.CameraTarget == info.TargetPoint
                )
                return true;

            return false;
        }

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
                RhinoViewport vp = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;
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
                RhinoViewport vp = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;
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
    }
}
