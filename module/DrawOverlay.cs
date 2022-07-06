using Rhino;
using Rhino.Display;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Display
{
    public static class Overlay
    {
        private static Size screenSize = Size.Empty;

        #region MessageOverlay
        public static bool MessageVisible = false;
        private static bool IsActive = false;
        private const int paddingLeftRight = 14;
        private const int paddingTopBottom = 6;
        private const int fontSize = 24;

        private static Size messageSize = Size.Empty;
        private static string message;
        private static DateTime start;

        public static void ShowMessage(string text) { ShowMessage(text, 2000); }
        public static void ShowMessage(string text, int durationMS)
        {
            if (string.IsNullOrEmpty(text) || durationMS <= 0)
                return;

            message = text;
            start = DateTime.Now;
            duration = durationMS;
            screenSize = Size.Empty;
            messageSize = Size.Empty;

            // add eventhandler only if necessary
            if (!IsActive)
            {
                IsActive = true;
                DisplayPipeline.DrawOverlay += OnDrawMessageOverlay;
            }

            // clear viewports (REDRAW) after duration expired
            Timer t = new Timer();
            t.Interval = (int)Math.Ceiling(duration) + 5;
            t.Tick += (object o, EventArgs e) => { t.Stop(); RhinoDoc.ActiveDoc.Views.Redraw(); };
            t.Start();

            // show message immediately (REDRAW)
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        private static StringFormat messageFormat = new StringFormat()
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        private static double duration = 1000;

        private static void OnDrawMessageOverlay(object sender, DrawEventArgs args)
        {
            if (args.Display.Viewport.Id != RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewportID)
                return;

            if (screenSize != args.Viewport.Bounds.Size)
            {
                screenSize = args.Viewport.Bounds.Size;

                Font font = new Font(SystemFonts.DefaultFont.FontFamily, fontSize, GraphicsUnit.Pixel);
                CalculateString(message, ref font);
                DrawMessageBitmap(font);
            }

            int ms = (int)Math.Floor(DateTime.Now.Subtract(start).TotalMilliseconds);
            if (ms >= duration)
            {
                DisplayPipeline.DrawOverlay -= OnDrawMessageOverlay;
                IsActive = false;
                screenSize = Size.Empty;
                return;
            }

            args.Display.DrawBitmap(
              bitmap,
              (int)Math.Round((screenSize.Width - messageSize.Width) * 0.5),
              (int)Math.Round((screenSize.Height - messageSize.Height) * 0.9)
              );
        }

        private static void DrawMessageBitmap(Font font)
        {
            Rectangle rect = new Rectangle(0, 0, messageSize.Width, messageSize.Height);
            Bitmap bmp = new Bitmap(messageSize.Width, messageSize.Height);

            // Init GFX
            Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PageUnit = GraphicsUnit.Pixel;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            g.Clear(Color.Transparent);

            // Draw BOX
            int rad = Math.Min(paddingLeftRight, paddingTopBottom);
            int diam = rad * 2;

            Brush brush = new SolidBrush(Color.FromArgb(191, 0, 0, 0));
            Rectangle box = new Rectangle(
              paddingLeftRight,
              paddingTopBottom,
              messageSize.Width - 2 * paddingLeftRight,
              messageSize.Height - 2 * paddingTopBottom);

            g.FillRectangle(brush, box);

            box.Inflate(rad, rad);
            g.FillPie(brush, box.Left, box.Top, diam, diam, 180, 90);
            g.FillPie(brush, box.Left, box.Bottom - diam, diam, diam, 90, 90);
            g.FillPie(brush, box.Right - diam, box.Top, diam, diam, 270, 90);
            g.FillPie(brush, box.Right - diam, box.Bottom - diam, diam, diam, 0, 90);

            g.FillRectangle(brush, box.Left, box.Top + rad, rad, box.Height - diam);
            g.FillRectangle(brush, box.Right - rad, box.Top + rad, rad, box.Height - diam);
            g.FillRectangle(brush, box.Left + rad, box.Top, box.Width - diam, rad);
            g.FillRectangle(brush, box.Left + rad, box.Bottom - rad, box.Width - diam, rad);

            // Draw TEXT
            brush = new SolidBrush(Color.White);
            g.DrawString(message, font, brush, rect, messageFormat);


            // SAVE
            g.Save();
            bitmap = new DisplayBitmap(bmp);
        }

        public static void CalculateString(string text, ref Font font)
        {
            using (var image = new Bitmap(1, 1))
            {
                using (var g = Graphics.FromImage(image))
                {
                    Size s = Size.Round(g.MeasureString(text, font));
                    while (s.Width + paddingLeftRight * 2 > screenSize.Width * 0.8 || s.Height + paddingTopBottom * 2 > screenSize.Height * 0.5)
                    {
                        font = new Font(font.FontFamily, font.Size - 0.5f, font.Style, font.Unit, font.GdiCharSet, font.GdiVerticalFont);
                        s = Size.Round(g.MeasureString(text, font));
                    }

                    messageSize = new Size(s.Width + paddingLeftRight * 2, s.Height + paddingTopBottom * 2);
                }
            }
        }

        #endregion

        #region ImageOverlay
        public static bool ImageVisible { get { return displayImage != null; } }
        private static Image displayImage = null;
        private static DisplayBitmap bitmap = null;

        public static void ShowImage(Image img)
        {
            displayImage = img;

            if (displayImage != null)
            {
                screenSize = Size.Empty;
                DisplayPipeline.DrawOverlay += OnDrawImageOverlay;
            }
            else
            {
                DisplayPipeline.DrawOverlay -= OnDrawImageOverlay;
                screenSize = Size.Empty;
            }
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        private static Size img = new Size(1000, 400);
        private static void OnDrawImageOverlay(object sender, DrawEventArgs args)
        {
            if (args.Display.Viewport.Id != RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewportID || displayImage == null)
                return;

            if (screenSize != args.Viewport.Bounds.Size)
            {
                screenSize = args.Viewport.Bounds.Size;
                bitmap = DrawInfoImage(displayImage);
            }

            args.Display.DrawBitmap(bitmap, 0, 0);
        }

        private static DisplayBitmap DrawInfoImage(Image img)
        {
            if (img == null)
                return null;

            Bitmap bmp = new Bitmap(screenSize.Width, screenSize.Height);
            Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.FromArgb(191, 0, 0, 0));

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
