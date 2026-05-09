using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace WatermarkTool
{
    public static class WatermarkBitmapGenerator
    {
        public static Bitmap Generate(WatermarkSettings s)
        {
            int w = (int)(s.PageWidthPt / 72f * s.Dpi);
            int h = (int)(s.PageHeightPt / 72f * s.Dpi);
            if (w < 1) w = 1;
            if (h < 1) h = 1;

            var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.Clear(Color.Transparent);

                var color = Color.FromArgb(s.Transparency, s.FontColor.R, s.FontColor.G, s.FontColor.B);
                using (var brush = new SolidBrush(color))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    using (var font = new Font(s.FontName, s.FontSize, GraphicsUnit.Point))
                    {
                        if (s.IsTiled)
                        {
                            float gapXPx = s.GapX / 72f * s.Dpi;
                            float gapYPx = s.GapY / 72f * s.Dpi;
                            float startXPx = s.StartX / 72f * s.Dpi;
                            float startYPx = s.StartY / 72f * s.Dpi;
                            float boxW = Math.Max(gapXPx * 0.9f, 200);
                            float boxH = Math.Max(gapYPx * 0.9f, 100);

                            for (int i = 0; i < s.Rows; i++)
                            {
                                for (int j = 0; j < s.Cols; j++)
                                {
                                    float cx = startXPx + j * gapXPx + boxW / 2;
                                    float cy = startYPx + i * gapYPx + boxH / 2;
                                    if (cx > w + boxW / 2 || cy > h + boxH / 2) continue;
                                    DrawWatermark(g, s.Text, font, brush, sf, cx, cy, boxW, boxH, s.Rotation);
                                }
                            }
                        }
                        else
                        {
                            float cx = w / 2f;
                            float cy = h / 2f;
                            float boxW = w * 0.8f;
                            float boxH = h * 0.4f;
                            DrawWatermark(g, s.Text, font, brush, sf, cx, cy, boxW, boxH, s.Rotation);
                        }
                    }
                }
            }
            return bmp;
        }

        private static void DrawWatermark(Graphics g, string text, Font font, Brush brush, StringFormat sf, float cx, float cy, float boxW, float boxH, float rotation)
        {
            var state = g.Save();
            g.TranslateTransform(cx, cy);
            g.RotateTransform(rotation);
            var rect = new RectangleF(-boxW / 2, -boxH / 2, boxW, boxH);
            g.DrawString(text, font, brush, rect, sf);
            g.Restore(state);
        }
    }
}