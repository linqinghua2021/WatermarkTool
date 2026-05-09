using System.Drawing;

namespace WatermarkTool
{
    public class WatermarkSettings
    {
        public string Text { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public Color FontColor { get; set; }
        public int Transparency { get; set; }
        public float Rotation { get; set; }
        public bool IsTiled { get; set; }
        public int Rows { get; set; }
        public int Cols { get; set; }
        public float GapX { get; set; }
        public float GapY { get; set; }
        public float StartX { get; set; }
        public float StartY { get; set; }
        public float PageWidthPt { get; set; }
        public float PageHeightPt { get; set; }
        public int Dpi { get; set; }
    }
}