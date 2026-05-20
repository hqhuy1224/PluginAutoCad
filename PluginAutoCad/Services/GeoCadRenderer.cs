using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

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
        // hàm tạo layer nếu chưa tồn tại 
        private static void EnsureLayer(Database db, Transaction tr, string layerName, short colorIndex)
        {
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            if (!lt.Has(layerName))
            {
                lt.UpgradeOpen();

                LayerTableRecord ltr = new LayerTableRecord
                {
                    Name = layerName,
                    Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(
                        Autodesk.AutoCAD.Colors.ColorMethod.ByAci,
                        colorIndex)
                };

                lt.Add(ltr);
                tr.AddNewlyCreatedDBObject(ltr, true);
            }
        }

        // hàm vẽ GeoJSON lên AutoCAD
        public static void DrawGeoJsonToCad(string json)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            JObject geojson;

            try
            {
                geojson = JObject.Parse(json);
            }
            catch
            {
                throw new Exception("Invalid GeoJSON format");
            }

            var features = geojson["features"];

            if (features == null)
                throw new Exception("GeoJSON missing 'features'");

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                foreach (var feature in features)
                {
                    var geom = feature["geometry"];
                    var props = feature["properties"];
                    string type = geom["type"].ToString();

                    switch (type)
                    {
                        case "Point":
                            DrawPoint(geom, props, db, btr, tr);
                            break;

                        case "LineString":
                            DrawLineString(geom, props, db, btr, tr);
                            break;

                        case "Polygon":
                            DrawPolygon(geom, props, db, btr, tr);
                            break;

                        case "MultiLineString":
                            DrawMultiLineString(geom, props, db, btr, tr);
                            break;

                        case "MultiPolygon":
                            DrawMultiPolygon(geom, props, db, btr, tr);
                            break;
                    }
                }

                tr.Commit();
            }
        }

        // hàm áp dụng style cho đối tượng CAD
        private static void ApplyStyle(Entity ent, string layerName, short colorIndex, LineWeight lw = LineWeight.LineWeight025)
        {
            ent.Layer = layerName;
            ent.ColorIndex = colorIndex;
            ent.LineWeight = lw;
        }

        private static void DrawPoint(JToken geom, JToken properties, Database db, BlockTableRecord btr, Transaction tr)
        {
            var coord = geom["coordinates"];

            double x = (double)coord[0];
            double y = (double)coord[1];

            DBPoint pt = new DBPoint(new Point3d(x, y, 0));

            EnsureLayer(db, tr, "GIS_POINT", 1);
            ApplyStyle(pt, "GIS_POINT", 1);

            btr.AppendEntity(pt);
            tr.AddNewlyCreatedDBObject(pt, true);

            // attach GIS attributes
            AttachAttributes(db, tr, pt, properties);
        }

        private static void DrawLineString(JToken geom, JToken properties, Database db, BlockTableRecord btr, Transaction tr)
        {
            var coords = geom["coordinates"];

            Polyline pl = new Polyline();

            int i = 0;

            foreach (var p in coords)
            {
                double x = (double)p[0];
                double y = (double)p[1];

                pl.AddVertexAt(i++, new Point2d(x, y), 0, 0, 0);
            }

            pl.Closed = false;

            EnsureLayer(db, tr, "GIS_LINE", 3);
            ApplyStyle(pl, "GIS_LINE", 3, LineWeight.LineWeight030);

            btr.AppendEntity(pl);
            tr.AddNewlyCreatedDBObject(pl, true);

            // attach GIS attribute
            AttachAttributes(db, tr, pl, properties);
        }

        private static void DrawPolygon(JToken geom, JToken properties, Database db, BlockTableRecord btr, Transaction tr)
        {
            var rings = geom["coordinates"];

            foreach (var ring in rings)
            {
                Polyline pl = new Polyline();

                int i = 0;

                foreach (var p in ring)
                {
                    double x = (double)p[0];
                    double y = (double)p[1];

                    pl.AddVertexAt(i++, new Point2d(x, y), 0, 0, 0);
                }

                pl.Closed = true;

                EnsureLayer(db, tr, "GIS_POLYGON", 5);
                ApplyStyle(pl, "GIS_POLYGON", 5, LineWeight.LineWeight005);

                btr.AppendEntity(pl);
                tr.AddNewlyCreatedDBObject(pl, true);

                AttachAttributes(db, tr, pl, properties);
            }
        }

        private static void DrawMultiLineString(JToken geom, JToken properties, Database db, BlockTableRecord btr, Transaction tr)
        {
            var lines = geom["coordinates"];

            foreach (var line in lines)
            {
                Polyline pl = new Polyline();

                int i = 0;

                foreach (var p in line)
                {
                    double x = (double)p[0];
                    double y = (double)p[1];

                    pl.AddVertexAt(i++, new Point2d(x, y), 0, 0, 0);
                }

                pl.Closed = false;

                EnsureLayer(db, tr, "GIS_LINE", 3);
                ApplyStyle(pl, "GIS_LINE", 3);

                btr.AppendEntity(pl);
                tr.AddNewlyCreatedDBObject(pl, true);

                AttachAttributes(db, tr, pl, properties);
            }
        }

        private static void DrawMultiPolygon(JToken geom, JToken properties, Database db, BlockTableRecord btr, Transaction tr)
        {
            var polygons = geom["coordinates"];

            foreach (var poly in polygons)
            {
                foreach (var ring in poly)
                {
                    Polyline pl = new Polyline();

                    int i = 0;

                    foreach (var p in ring)
                    {
                        double x = (double)p[0];
                        double y = (double)p[1];

                        pl.AddVertexAt(i++, new Point2d(x, y), 0, 0, 0);
                    }

                    pl.Closed = true;

                    EnsureLayer(db, tr, "GIS_POLYGON", 5);
                    ApplyStyle(pl, "GIS_POLYGON", 5, LineWeight.LineWeight050);

                    btr.AppendEntity(pl);
                    tr.AddNewlyCreatedDBObject(pl, true);

                    AttachAttributes(db, tr, pl, properties);
                }
            }
        }


        public static void AttachAttributes(Database db, Transaction tr, Entity ent, JToken properties)
        {
            if (properties == null)
                return;

            const string appName = "GIS_ATTR";

            // đăng ký RegApp
            var regTable = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);

            if (!regTable.Has(appName))
            {
                regTable.UpgradeOpen();

                RegAppTableRecord reg = new RegAppTableRecord();
                reg.Name = appName;

                regTable.Add(reg);
                tr.AddNewlyCreatedDBObject(reg, true);
            }

            ResultBuffer rb = new ResultBuffer();

            rb.Add(new TypedValue(1001, appName));

            foreach (var prop in properties)
            {
                string key = prop.Path.Split('.').Last();
                string value = prop.First.ToString();

                rb.Add(new TypedValue(1000, key + "=" + value));
            }

            ent.XData = rb;
        }
    }
}