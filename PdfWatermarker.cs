using System;
using System.Drawing;
using System.IO;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;

namespace WatermarkTool
{
    public static class PdfWatermarker
    {
        public static void Apply(string sourcePath, string outputPath, WatermarkSettings settings)
        {
            using (var inputDoc = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Import))
            using (var outputDoc = new PdfDocument())
            {
                for (int i = 0; i < inputDoc.PageCount; i++)
                {
                    var srcPage = inputDoc.Pages[i];
                    var page = outputDoc.AddPage(srcPage);

                    settings.PageWidthPt = (float)page.Width;
                    settings.PageHeightPt = (float)page.Height;
                    settings.Dpi = 150;

                    string tempPng = Path.Combine(Path.GetTempPath(), string.Format("wmtmp_{0}.png", Guid.NewGuid()));
                    try
                    {
                        using (var bmp = WatermarkBitmapGenerator.Generate(settings))
                        {
                            bmp.Save(tempPng, System.Drawing.Imaging.ImageFormat.Png);
                        }
                        var ximg = XImage.FromFile(tempPng);
                        var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
                        gfx.DrawImage(ximg, 0, 0, page.Width, page.Height);
                        gfx.Dispose();
                        ximg.Dispose();
                    }
                    finally
                    {
                        try { if (File.Exists(tempPng)) File.Delete(tempPng); } catch { }
                    }
                }
                outputDoc.Save(outputPath);
            }
        }
    }
}