using PluginAutoCad.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

public static class LayerParser
{
    public static List<string> ParseWmsLayers(string xml)
    {
        var doc = XDocument.Parse(xml);

        return doc.Descendants()
            .Where(x => x.Name.LocalName == "Layer")
            .Elements()
            .Where(x => x.Name.LocalName == "Name")
            .Select(x => x.Value)
            .Distinct()
            .ToList();
    }

    public static List<string> ParseWfsLayers(string xml)
    {
        var doc = XDocument.Parse(xml);

        return doc.Descendants()
            .Where(x => x.Name.LocalName == "FeatureType")
            .Elements()
            .Where(x => x.Name.LocalName == "Name")
            .Select(x => x.Value)
            .Distinct()
            .ToList();
    }

    public static WmsBoundingBox GetLayerBoundingBox(string xml, string layerName)
    {
        var doc = XDocument.Parse(xml);

        var layer = doc.Descendants()
            .FirstOrDefault(x => x.Name.LocalName == "Layer" &&
                x.Descendants().Any(n =>
                    n.Name.LocalName == "Name" &&
                    n.Value == layerName));

        if (layer == null)
            throw new Exception("Không tìm thấy layer");

        var geo = layer.Descendants()
            .FirstOrDefault(x => x.Name.LocalName == "EX_GeographicBoundingBox");

        return new WmsBoundingBox
        {
            MinX = double.Parse(geo.Element(geo.Name.Namespace + "westBoundLongitude").Value),
            MaxX = double.Parse(geo.Element(geo.Name.Namespace + "eastBoundLongitude").Value),
            MinY = double.Parse(geo.Element(geo.Name.Namespace + "southBoundLatitude").Value),
            MaxY = double.Parse(geo.Element(geo.Name.Namespace + "northBoundLatitude").Value)
        };
    }
}