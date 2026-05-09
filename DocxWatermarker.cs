using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace WatermarkTool
{
    public static class DocxWatermarker
    {
        const string W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        const string R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        const string WP = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
        const string A = "http://schemas.openxmlformats.org/drawingml/2006/main";
        const string PIC = "http://schemas.openxmlformats.org/drawingml/2006/picture";
        const string HDR_REL = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/header";
        const string IMG_REL = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image";
        const string DOC_REL = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

        private class RelInfo
        {
            public string Id;
            public string Type;
            public string TargetUri;
            public TargetMode Mode;
        }

        public static void Apply(string sourcePath, string outputPath, WatermarkSettings settings)
        {
            var partData = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            var partCt = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var partRels = new Dictionary<string, List<RelInfo>>(StringComparer.OrdinalIgnoreCase);
            var pkgRels = new List<RelInfo>();
            string docKey = null;

            using (var src = Package.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                foreach (var rel in src.GetRelationships())
                    pkgRels.Add(new RelInfo { Id = rel.Id, Type = rel.RelationshipType,
                        TargetUri = Normalize(rel.TargetUri), Mode = rel.TargetMode });

                foreach (var part in src.GetParts())
                {
                    string key = Normalize(part.Uri);
                    if (key == "_rels/.rels") continue;
                    partCt[key] = part.ContentType;
                    using (var s = part.GetStream(FileMode.Open, FileAccess.Read))
                    {
                        partData[key] = new byte[s.Length];
                        int off = 0; while (off < partData[key].Length) off += s.Read(partData[key], off, partData[key].Length - off);
                    }
                    if (!key.EndsWith(".rels", StringComparison.OrdinalIgnoreCase))
                    {
                        var rels = new List<RelInfo>();
                        foreach (var rel in part.GetRelationships())
                            rels.Add(new RelInfo { Id = rel.Id, Type = rel.RelationshipType,
                                TargetUri = Normalize(rel.TargetUri), Mode = rel.TargetMode });
                        partRels[key] = rels;
                    }
                    if (key.EndsWith("/document.xml", StringComparison.OrdinalIgnoreCase) && docKey == null)
                        docKey = key;
                }
            }
            if (docKey == null) throw new Exception("找不到 document.xml。");
            int slash = docKey.LastIndexOf('/');
            string dir = docKey.Substring(0, slash + 1);

            // parse page size
            string docStr = Encoding.UTF8.GetString(partData[docKey]);
            var docXml = XDocument.Parse(docStr);
            var nsW = XNamespace.Get(W);
            float pageW = 595, pageH = 842;
            var pgSzMatch = Regex.Match(docStr, @"<w:pgSz[^>]*?w:w\s*=\s*""(\d+)""[^>]*?w:h\s*=\s*""(\d+)""",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (pgSzMatch.Success)
            {
                pageW = float.Parse(pgSzMatch.Groups[1].Value) / 20f;
                pageH = float.Parse(pgSzMatch.Groups[2].Value) / 20f;
            }

            // generate watermark image
            settings.PageWidthPt = pageW; settings.PageHeightPt = pageH; settings.Dpi = 150;
            string imgKey = dir + "media/watermark.png";
            using (var bmp = WatermarkBitmapGenerator.Generate(settings))
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                partData[imgKey] = ms.ToArray();
            }
            partCt[imgKey] = "image/png";

            // IDs and part names must not collide with existing document content.
            string imgRid = NextRelId(partRels.ContainsKey(docKey) ? partRels[docKey] : null, "rIdWmImage");
            string hdrImgRid = NextRelId(null, "rIdWmImage");
            string hdrRid = NextRelId(partRels.ContainsKey(docKey) ? partRels[docKey] : null, "rIdWmHeader");
            string hdrKey = NextHeaderKey(partData, dir);

            // header XML
            long cxEmu = (long)(pageW * 12700), cyEmu = (long)(pageH * 12700);
            string hdrXml = string.Format(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<w:hdr xmlns:w=""{0}"" xmlns:r=""{1}"" xmlns:wp=""{2}"" xmlns:a=""{3}"" xmlns:pic=""{4}"">
  <w:p><w:pPr><w:pStyle w:val=""Header""/></w:pPr><w:r><w:rPr><w:noProof/></w:rPr><w:drawing>
   <wp:anchor distT=""0"" distB=""0"" distL=""0"" distR=""0"" simplePos=""0"" relativeHeight=""251658752"" behindDoc=""0"" locked=""0"" layoutInCell=""1"" allowOverlap=""1"">
    <wp:simplePos x=""0"" y=""0""/>
    <wp:positionH relativeFrom=""page""><wp:posOffset>0</wp:posOffset></wp:positionH>
    <wp:positionV relativeFrom=""page""><wp:posOffset>0</wp:posOffset></wp:positionV>
    <wp:extent cx=""{5}"" cy=""{6}""/>
    <wp:effectExtent l=""0"" t=""0"" r=""0"" b=""0""/>
    <wp:wrapNone/>
    <wp:docPr id=""1"" name=""Watermark""/>
    <wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect=""1""/></wp:cNvGraphicFramePr>
    <a:graphic><a:graphicData uri=""http://schemas.openxmlformats.org/drawingml/2006/picture"">
     <pic:pic>
      <pic:nvPicPr><pic:cNvPr id=""0"" name=""watermark.png""/><pic:cNvPicPr/></pic:nvPicPr>
      <pic:blipFill><a:blip r:embed=""{7}""/><a:stretch><a:fillRect/></a:stretch></pic:blipFill>
      <pic:spPr>
       <a:xfrm><a:off x=""0"" y=""0""/><a:ext cx=""{5}"" cy=""{6}""/></a:xfrm>
       <a:prstGeom prst=""rect""><a:avLst/></a:prstGeom>
      </pic:spPr>
     </pic:pic>
    </a:graphicData></a:graphic>
   </wp:anchor>
  </w:drawing></w:r></w:p>
</w:hdr>", W, R, WP, A, PIC, cxEmu, cyEmu, hdrImgRid);
            partData[hdrKey] = Encoding.UTF8.GetBytes(hdrXml);
            partCt[hdrKey] = "application/vnd.openxmlformats-officedocument.wordprocessingml.header+xml";
            partRels[hdrKey] = new List<RelInfo> {
                new RelInfo { Id = hdrImgRid, Type = IMG_REL, TargetUri = "media/watermark.png", Mode = TargetMode.Internal }
            };

            // patch document.xml
            string patchedDoc = PatchDocumentXml(docStr, hdrRid, imgRid, pageW, pageH);
            partData[docKey] = Encoding.UTF8.GetBytes(patchedDoc);

            // update document relationships
            if (!partRels.ContainsKey(docKey))
                partRels[docKey] = new List<RelInfo>();
            var docRels = partRels[docKey];
            docRels.Add(new RelInfo { Id = hdrRid, Type = HDR_REL, TargetUri = PartFileName(hdrKey), Mode = TargetMode.Internal });
            docRels.Add(new RelInfo { Id = imgRid, Type = IMG_REL, TargetUri = "media/watermark.png", Mode = TargetMode.Internal });

            // write output
            if (File.Exists(outputPath)) File.Delete(outputPath);
            using (var dst = Package.Open(outputPath, FileMode.Create))
            {
                foreach (var kv in partData)
                {
                    if (kv.Key.EndsWith(".rels", StringComparison.OrdinalIgnoreCase) || kv.Key == "[Content_Types].xml")
                        continue;
                    var uri = new Uri("/" + kv.Key.TrimStart('/'), UriKind.Relative);
                    var ct = partCt.ContainsKey(kv.Key) ? partCt[kv.Key] : "application/octet-stream";
                    var part = dst.CreatePart(uri, ct);
                    using (var s = part.GetStream())
                        s.Write(kv.Value, 0, kv.Value.Length);
                    if (partRels.ContainsKey(kv.Key))
                        foreach (var r in partRels[kv.Key])
                            part.CreateRelationship(
                                new Uri(r.TargetUri, r.Mode == TargetMode.External ? UriKind.Absolute : UriKind.Relative),
                                r.Mode, r.Type, r.Id);
                }
                foreach (var r in pkgRels)
                    dst.CreateRelationship(
                        new Uri(r.TargetUri, r.Mode == TargetMode.External ? UriKind.Absolute : UriKind.Relative),
                        r.Mode, r.Type, r.Id);
            }
        }

        private static string Normalize(Uri uri)
        {
            string s = uri.ToString();
            return s.StartsWith("/") ? s.Substring(1) : s;
        }

        private static string PatchDocumentXml(string xml, string hdrRid, string imgRid, float pageW, float pageH)
        {
            var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
            var nsW = XNamespace.Get(W);
            var nsR = XNamespace.Get(R);
            var body = doc.Root.Element(nsW + "body");
            if (body == null) return xml;

            var sectPrs = body.Descendants(nsW + "sectPr").ToList();
            if (sectPrs.Count == 0)
            {
                var sectPr = new XElement(nsW + "sectPr",
                    new XElement(nsW + "pgSz",
                        new XAttribute(nsW + "w", ((int)(pageW * 20)).ToString()),
                        new XAttribute(nsW + "h", ((int)(pageH * 20)).ToString())),
                    new XElement(nsW + "pgMar",
                        new XAttribute(nsW + "top", "1440"),
                        new XAttribute(nsW + "right", "1800"),
                        new XAttribute(nsW + "bottom", "1440"),
                        new XAttribute(nsW + "left", "1800"),
                        new XAttribute(nsW + "header", "720"),
                        new XAttribute(nsW + "footer", "720"),
                        new XAttribute(nsW + "gutter", "0")),
                    new XElement(nsW + "cols", new XAttribute(nsW + "space", "720")),
                    new XElement(nsW + "docGrid", new XAttribute(nsW + "linePitch", "312")));
                body.Add(sectPr);
                sectPrs.Add(sectPr);
            }

            foreach (var sectPr in sectPrs)
            {
                sectPr.Elements(nsW + "headerReference")
                    .Where(e => (string)e.Attribute(nsW + "type") == "default")
                    .Remove();
                sectPr.AddFirst(new XElement(nsW + "headerReference",
                    new XAttribute(nsW + "type", "default"),
                    new XAttribute(nsR + "id", hdrRid)));
            }

            AddForegroundWatermarksOverPictures(doc, imgRid, pageW, pageH);
            return doc.Declaration == null ? doc.ToString(SaveOptions.DisableFormatting) : doc.Declaration + doc.ToString(SaveOptions.DisableFormatting);
        }

        private static void AddForegroundWatermarksOverPictures(XDocument doc, string imgRid, float pageW, float pageH)
        {
            var nsW = XNamespace.Get(W);
            var nsWp = XNamespace.Get(WP);
            var nsA = XNamespace.Get(A);
            var nsPic = XNamespace.Get(PIC);
            var nsR = XNamespace.Get(R);
            long cxEmu = (long)(pageW * 12700), cyEmu = (long)(pageH * 12700);
            int docPrId = doc.Descendants(nsWp + "docPr")
                .Select(e => (int?)e.Attribute("id") ?? 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            foreach (var para in doc.Descendants(nsW + "p").ToList())
            {
                bool hasPicture = para.Descendants(nsA + "blip").Any()
                    || para.Descendants(nsPic + "pic").Any()
                    || para.Descendants().Any(e => e.Name.LocalName == "pict");
                bool hasWatermark = para.Descendants(nsWp + "docPr")
                    .Any(e => ((string)e.Attribute("name") ?? "").StartsWith("Watermark", StringComparison.OrdinalIgnoreCase));
                if (!hasPicture || hasWatermark) continue;

                para.Add(new XElement(nsW + "r",
                    new XElement(nsW + "rPr", new XElement(nsW + "noProof")),
                    new XElement(nsW + "drawing", CreateAnchor(imgRid, docPrId++, cxEmu, cyEmu))));
            }
        }

        private static XElement CreateAnchor(string imgRid, int docPrId, long cxEmu, long cyEmu)
        {
            var nsWp = XNamespace.Get(WP);
            var nsA = XNamespace.Get(A);
            var nsPic = XNamespace.Get(PIC);
            var nsR = XNamespace.Get(R);

            return new XElement(nsWp + "anchor",
                new XAttribute("distT", "0"),
                new XAttribute("distB", "0"),
                new XAttribute("distL", "0"),
                new XAttribute("distR", "0"),
                new XAttribute("simplePos", "0"),
                new XAttribute("relativeHeight", "251658752"),
                new XAttribute("behindDoc", "0"),
                new XAttribute("locked", "0"),
                new XAttribute("layoutInCell", "1"),
                new XAttribute("allowOverlap", "1"),
                new XElement(nsWp + "simplePos", new XAttribute("x", "0"), new XAttribute("y", "0")),
                new XElement(nsWp + "positionH", new XAttribute("relativeFrom", "page"), new XElement(nsWp + "posOffset", "0")),
                new XElement(nsWp + "positionV", new XAttribute("relativeFrom", "page"), new XElement(nsWp + "posOffset", "0")),
                new XElement(nsWp + "extent", new XAttribute("cx", cxEmu.ToString()), new XAttribute("cy", cyEmu.ToString())),
                new XElement(nsWp + "effectExtent",
                    new XAttribute("l", "0"), new XAttribute("t", "0"), new XAttribute("r", "0"), new XAttribute("b", "0")),
                new XElement(nsWp + "wrapNone"),
                new XElement(nsWp + "docPr", new XAttribute("id", docPrId.ToString()), new XAttribute("name", "Watermark")),
                new XElement(nsWp + "cNvGraphicFramePr",
                    new XElement(nsA + "graphicFrameLocks", new XAttribute("noChangeAspect", "1"))),
                new XElement(nsA + "graphic",
                    new XElement(nsA + "graphicData",
                        new XAttribute("uri", "http://schemas.openxmlformats.org/drawingml/2006/picture"),
                        new XElement(nsPic + "pic",
                            new XElement(nsPic + "nvPicPr",
                                new XElement(nsPic + "cNvPr", new XAttribute("id", "0"), new XAttribute("name", "watermark.png")),
                                new XElement(nsPic + "cNvPicPr")),
                            new XElement(nsPic + "blipFill",
                                new XElement(nsA + "blip", new XAttribute(nsR + "embed", imgRid)),
                                new XElement(nsA + "stretch", new XElement(nsA + "fillRect"))),
                            new XElement(nsPic + "spPr",
                                new XElement(nsA + "xfrm",
                                    new XElement(nsA + "off", new XAttribute("x", "0"), new XAttribute("y", "0")),
                                    new XElement(nsA + "ext", new XAttribute("cx", cxEmu.ToString()), new XAttribute("cy", cyEmu.ToString()))),
                                new XElement(nsA + "prstGeom", new XAttribute("prst", "rect"), new XElement(nsA + "avLst")))))));
        }

        private static string NextRelId(List<RelInfo> rels, string prefix)
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (rels != null)
                foreach (var rel in rels)
                    used.Add(rel.Id);

            string id;
            do id = prefix + Guid.NewGuid().ToString("N").Substring(0, 8);
            while (used.Contains(id));
            return id;
        }

        private static string NextHeaderKey(Dictionary<string, byte[]> partData, string dir)
        {
            for (int i = 1; i < 1000; i++)
            {
                string key = dir + "header" + i + ".xml";
                if (!partData.ContainsKey(key)) return key;
            }
            return dir + "header" + Guid.NewGuid().ToString("N") + ".xml";
        }

        private static string PartFileName(string key)
        {
            int slash = key.LastIndexOf('/');
            return slash >= 0 ? key.Substring(slash + 1) : key;
        }
    }
}
