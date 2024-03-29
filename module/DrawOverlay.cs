﻿using Rhino;
using Rhino.Display;
using Rhino.UI;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace Display
{
    public static class Overlay
    {
        private static Size screenSize = Size.Empty;
        public static bool isMessageShown = false;
        public static bool isImageShown = false;

        #region MessageOverlay

        private static string message;
        private static Size messageSize = Size.Empty;
        private static DateTime showMessageUntil;

        private static float scaleFactor = 1;
        private const int paddingLeftRight = 14;
        private const int paddingTopBottom = 6;

        async public static void RefreshViews(int milliseconds)
        {
            await Task.Delay(milliseconds);
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        public static void ShowMessage(string text) { ShowMessage(text, 2000); }

        public static void ShowMessage(string text, int durationMS)
        {
            if (string.IsNullOrEmpty(text) || durationMS <= 0)
                return;

            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                scaleFactor = Math.Max(graphics.DpiX, graphics.DpiY) / 96;
            }

            message = text;
            showMessageUntil = DateTime.Now.AddMilliseconds(durationMS);
            RefreshViews(durationMS);
            screenSize = Size.Empty;
            messageSize = Size.Empty;

            DisplayPipeline.DrawOverlay += OnDrawMessageOverlay;
            isMessageShown = true;

            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        private static StringFormat messageFormat = new StringFormat()
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        private static void OnDrawMessageOverlay(object sender, DrawEventArgs args)
        {
            if (args.Display.Viewport.Id != RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewportID)
                return;

            if (isImageShown)
                return;

            if (screenSize != args.Viewport.Bounds.Size)
            {
                screenSize = args.Viewport.Bounds.Size;

                Font normalFont = Fonts.NormalFont;
                Font font = new Font(normalFont.FontFamily, 4, GraphicsUnit.Millimeter);
                CalculateString(message, ref font);
                DrawMessageBitmap(font);
            }

            if (DateTime.Now >= showMessageUntil && isMessageShown)
            {
                isMessageShown = false;
                DisplayPipeline.DrawOverlay -= OnDrawMessageOverlay;
                screenSize = Size.Empty;
                return;
            }

            args.Display.DrawBitmap(
              bitmapImage,
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
            int rad = (int)(Math.Min(paddingLeftRight, paddingTopBottom) * scaleFactor);
            int diam = rad * 2;

            Brush brush = new SolidBrush(Color.FromArgb(223, 0, 0, 0));
            Rectangle box = new Rectangle(rad, rad, messageSize.Width - 2 * rad, messageSize.Height - 2 * rad);

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
            bitmapImage = new DisplayBitmap(bmp);
        }

        public static void CalculateString(string text, ref Font font)
        {
            using (var image = new Bitmap(1, 1))
            {
                using (var g = Graphics.FromImage(image))
                {
                    Size s = Size.Round(g.MeasureString(text, font));

                    int messageWidth = s.Width + (int)(paddingLeftRight * scaleFactor) * 2;
                    int messageHeight = s.Height + (int)(paddingTopBottom * scaleFactor) * 2;

                    while (messageWidth > screenSize.Width * 0.8 || messageHeight > screenSize.Height * 0.5)
                    {
                        s = Size.Round(g.MeasureString(text, font));
                    }

                    messageSize = new Size(messageWidth, messageHeight);
                }
            }
        }

        #endregion

        #region ImageOverlay

        private static Image displayImage = null;
        private static DisplayBitmap bitmapImage = null;

        public static void ShowImage(Image img)
        {
            displayImage = img;

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
            g.Clear(Color.FromArgb(223, 0, 0, 0));

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
