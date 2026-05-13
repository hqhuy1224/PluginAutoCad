using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Newtonsoft.Json.Linq;
using System;

namespace PluginAutoCad.Services
{
    public static class GeoCadRenderer
    {
        // hàm đính kèm ảnh WMS vào AutoCAD
        public static ObjectId AttachWmsImage(string imagePath, Point3d insertPoint, double scaleFactor = 1.0)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                // Tạo hoặc lấy Image Dictionary
                ObjectId imageDictId = RasterImageDef.GetImageDictionary(db);
                if (imageDictId.IsNull)
                    imageDictId = RasterImageDef.CreateImageDictionary(db);

                var imageDict = (DBDictionary)tr.GetObject(imageDictId, OpenMode.ForWrite);

                // Tạo RasterImageDef
                var imageDef = new RasterImageDef();
                imageDef.SourceFileName = imagePath;
                imageDef.Load();   

                string defName = "WMS_" + DateTime.Now.Ticks;
                ObjectId defId = imageDict.SetAt(defName, imageDef);
                tr.AddNewlyCreatedDBObject(imageDef, true);

                // Tạo RasterImage
                var raster = new RasterImage();
                raster.ImageDefId = defId;
                raster.ShowImage = true;
                raster.ImageTransparency = true;

                raster.Orientation = new CoordinateSystem3d(
                    insertPoint,
                    new Vector3d(scaleFactor, 0, 0),   // trục X
                    new Vector3d(0, scaleFactor, 0)    // trục Y
                );

                // Thêm vào Model Space
                var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                ObjectId rasterId = btr.AppendEntity(raster);
                tr.AddNewlyCreatedDBObject(raster, true);

                tr.Commit();
                return rasterId;
            }
        }
        // hàm vẽ GeoJSON lên AutoCAD
        public static void DrawGeoJsonToCad(string json)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            var obj = JObject.Parse(json);
            var features = obj["features"];

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                foreach (var feature in features)
                {
                    var geom = feature["geometry"];
                    string type = geom["type"].ToString();

                    // POLYGON
                    if (type == "Polygon")
                    {
                        var ring = geom["coordinates"][0];

                        Polyline pl = new Polyline();

                        int i = 0;
                        foreach (var p in ring)
                        {
                            double x = (double)p[0];
                            double y = (double)p[1];

                            pl.AddVertexAt(i, new Point2d(x, y), 0, 0, 0);
                            i++;
                        }

                        pl.Closed = true;

                        btr.AppendEntity(pl);
                        tr.AddNewlyCreatedDBObject(pl, true);
                    }

                    // LINESTRING
                    else if (type == "LineString")
                    {
                        var line = geom["coordinates"];

                        Polyline pl = new Polyline();

                        int i = 0;
                        foreach (var p in line)
                        {
                            double x = (double)p[0];
                            double y = (double)p[1];

                            pl.AddVertexAt(i, new Point2d(x, y), 0, 0, 0);
                            i++;
                        }

                        btr.AppendEntity(pl);
                        tr.AddNewlyCreatedDBObject(pl, true);
                    }

                    // POINT
                    else if (type == "Point")
                    {
                        var p = geom["coordinates"];

                        double x = (double)p[0];
                        double y = (double)p[1];

                        DBPoint pt = new DBPoint(new Point3d(x, y, 0));

                        btr.AppendEntity(pt);
                        tr.AddNewlyCreatedDBObject(pt, true);
                    }
                }

                tr.Commit();
            }
        }

    }
}