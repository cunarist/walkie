using Rhino;
using Rhino.Display;
using RhinoWASD.Properties;
using System;
using System.Drawing;

namespace Display
{
    public static class Aimpoint
    {
        private static Size screenSize = Size.Empty;
        public static bool isMessageShown = false;
        public static bool isImageShown = false;

        #region ImageOverlay

        private static Image displayImage = null;
        private static DisplayBitmap bitmapImage = null;

        public static void ShowImage(bool visible)
        {
            if (visible) { displayImage = Resources.aimpoint; }
            else { displayImage = null; }

            if (displayImage != null)
            {
                screenSize = Size.Empty;
                DisplayPipeline.DrawOverlay += OnDrawImageOverlay;
                isImageShown = true;
            }
            else
            {
                DisplayPipeline.DrawOverlay -= OnDrawImageOverlay;
                screenSize = Size.Empty;
                isImageShown = false;
            }

            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        private static void OnDrawImageOverlay(object sender, DrawEventArgs args)
        {
            if (args.Display.Viewport.Id != RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewportID || displayImage == null)
                return;

            if (screenSize != args.Viewport.Bounds.Size)
            {
                screenSize = args.Viewport.Bounds.Size;
                bitmapImage = DrawInfoImage(displayImage);
            }

            args.Display.DrawBitmap(bitmapImage, 0, 0);
        }

        private static DisplayBitmap DrawInfoImage(Image img)
        {
            if (img == null)
                return null;

            Bitmap bmp = new Bitmap(screenSize.Width, screenSize.Height);
            Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.FromArgb(0, 0, 0, 0));

            // Figure out the ratio
            double ratioX = (double)screenSize.Width / (double)img.Width;
            double ratioY = (double)screenSize.Height / (double)img.Height;
            double ratio = ratioX < ratioY ? ratioX : ratioY;
            ratio = ratio > 1 ? 1 : ratio;

            int width = Convert.ToInt32(img.Width * ratio);
            int height = Convert.ToInt32(img.Height * ratio);

            // Now calculate the X,Y position of the upper-left corner
            // (one of these will always be zero)
            int x = Convert.ToInt32((screenSize.Width - (img.Width * ratio)) / 2);
            int y = Convert.ToInt32((screenSize.Height - (img.Height * ratio)) / 2);

            Rectangle srcRect = new Rectangle(0, 0, img.Width, img.Height);
            Rectangle destRect = new Rectangle(x, y, width, height);
            g.DrawImage(img, destRect, srcRect, GraphicsUnit.Pixel);
            g.Save();

            return new DisplayBitmap(bmp);
        }

        #endregion
    }
}
