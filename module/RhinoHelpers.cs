using Display;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using System;
using System.Drawing;
using System.IO;

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

            int cur = RhinoDoc.ActiveDoc.NamedViews.Count - 1;
            if (!string.IsNullOrEmpty(CurrentNamedView))
            {
                cur = RhinoDoc.ActiveDoc.NamedViews.FindByName(CurrentNamedView);
                if (cur >= 0 && cur < count && IsNamedView(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport, RhinoDoc.ActiveDoc.NamedViews[cur].Viewport))
                    cur--;

            }
            RestoreNamedView(cur);
        }

        public static void NextNamedView()
        {
            int count = RhinoDoc.ActiveDoc.NamedViews.Count;
            if (count < 1)
                return;

            int cur = 0;
            if (!string.IsNullOrEmpty(CurrentNamedView))
            {
                cur = RhinoDoc.ActiveDoc.NamedViews.FindByName(CurrentNamedView);
                if (cur >= 0 && cur < count && IsNamedView(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport, RhinoDoc.ActiveDoc.NamedViews[cur].Viewport))
                    cur++;

            }
            RestoreNamedView(cur);
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

        public static Size CalculateSize(int resolution, int width, int height, int dpi, bool portrait)
        {
            switch (resolution)
            {
                case 0: //FullHD
                    width = 1920;
                    height = 1080;
                    break;
                case 1: //4K
                    width = 3840;
                    height = 2160;
                    break;
                case 2: //A4
                case 3: //A3
                    if (portrait)
                    {
                        width = resolution == 2 ? 210 : 297;
                        height = resolution == 2 ? 297 : 420;
                    }
                    else
                    {
                        width = resolution == 2 ? 297 : 420;
                        height = resolution == 2 ? 210 : 297;
                    }
                    width = (int)Math.Round(((double)dpi * (double)width) / 25.4);
                    height = (int)Math.Round(((double)dpi * (double)height) / 25.4);
                    break;
                case 4: //View
                    width = 0;
                    height = 0;
                    break;
            }
            return new Size(width, height);
        }

        public static void CustomScreenshot()
        {
            Size size = CalculateSize(
                Properties.Settings.Default.Resolution,
                Properties.Settings.Default.Width,
                Properties.Settings.Default.Height,
                Properties.Settings.Default.DPI,
                Properties.Settings.Default.Portrait);

            Screenshot(size, Properties.Settings.Default.GridAndAxes);
        }

        private static void Screenshot(Size size, bool ShowGridAndAxes)
        {
            string rhinoFile = RhinoDoc.ActiveDoc.Path;
            string filename = string.IsNullOrEmpty(rhinoFile) ? "" : Path.GetFileNameWithoutExtension(rhinoFile) + "_";
            string dir = string.IsNullOrEmpty(rhinoFile) ?
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop) :
                Path.GetDirectoryName(rhinoFile);

            string name = filename + Environment.UserName + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string path = Path.Combine(dir, "Screenshots", name + ".png");
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));

            Bitmap img = size.IsEmpty ?
                RhinoDoc.ActiveDoc.Views.ActiveView.CaptureToBitmap(ShowGridAndAxes, ShowGridAndAxes, ShowGridAndAxes) :
                RhinoDoc.ActiveDoc.Views.ActiveView.CaptureToBitmap(size, ShowGridAndAxes, ShowGridAndAxes, ShowGridAndAxes);

            if (File.Exists(path))
                File.Delete(path);

            img.Save(path);
            Overlay.ShowMessage("Screenshot \"" + name + ".png\" saved");
        }
    }
}
