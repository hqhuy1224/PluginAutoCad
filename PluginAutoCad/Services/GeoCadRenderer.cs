using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Windows.Forms;

namespace PluginAutoCad.Services
{
    public static class GeoCadRenderer
    {
        // hàm đính kèm ảnh WMS vào AutoCAD
        public static ObjectId AttachWmsImage(string imagePath, Point3d insertPoint, double scaleFactor = 1.0)
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
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
                var btr = (BlockTableRecord)tr.GetObject(
                 SymbolUtilityServices.GetBlockModelSpaceId(db),
                 OpenMode.ForWrite);
                ObjectId rasterId = btr.AppendEntity(raster);
                tr.AddNewlyCreatedDBObject(raster, true);

                tr.Commit();
                doc.Editor.Regen();
                doc.Editor.Command("_.ZOOM", "_E");
                return rasterId;
            }
        }
        // hàm tạo layer nếu chưa tồn tại 
        private static void EnsureLayer(
             Database db,
             Transaction tr,
             string layerName)
        {
            var lt = (LayerTable)tr.GetObject(
                db.LayerTableId,
                OpenMode.ForRead);

            if (!lt.Has(layerName))
            {
                lt.UpgradeOpen();

                var ltr = new LayerTableRecord
                {
                    Name = layerName
                };

                lt.Add(ltr);
                tr.AddNewlyCreatedDBObject(ltr, true);
            }
        }

        // hàm vẽ GeoJSON lên AutoCAD
        public static void DrawGeoJsonToCad(string json, string sourceLayerName)
        {
            // lấy doccument và database hiện tại của AutoCAD
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            // parse GeoJSON (thuộc thư viện Newtonsoft.Json) vd: parse thành object geojson["name"]
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

            // bắt đầu transaction để vẽ các đối tượng vào AutoCAD
            using (var tr = db.TransactionManager.StartTransaction())
            {
                //var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                //var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                // lấy model space để vẽ các đối tượng vào đó
                var btr = (BlockTableRecord)tr.GetObject(
                SymbolUtilityServices.GetBlockModelSpaceId(db),
                OpenMode.ForWrite);
                foreach (var feature in features)
                {
                    var geom = feature["geometry"];
                    var props = feature["properties"];
                    string type = geom["type"].ToString();
                    string layerName = GetLayerName(sourceLayerName);

                    switch (type)
                    {
                        case "Point":
                            DrawPoint(feature, geom, props,layerName, db, btr, tr);
                            break;

                        case "LineString":
                            DrawLineString(feature, geom, props,layerName, db, btr, tr);
                            break;

                        case "Polygon":
                            DrawPolygon(feature, geom, props, layerName, db, btr, tr);
                            break;

                        case "MultiLineString":
                            DrawMultiLineString(feature, geom, props,layerName, db, btr, tr);
                            break;

                        case "MultiPolygon":
                            DrawMultiPolygon(feature, geom, props, layerName, db, btr, tr);
                            break;
                    }
                }

                tr.Commit();
                doc.Editor.Regen();
                doc.Editor.Command("_.ZOOM", "_E");
            }
        }

        // hàm chuẩn hóa tên layer (chỉ giữ lại chữ, số và dấu gạch dưới, chuyển thành chữ hoa)
        private static string NormalizeLayer(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "DEFAULT";

            return new string(name
                .ToUpper()
                .Where(c => char.IsLetterOrDigit(c) || c == '_')
                .ToArray());
        }

        // hàm tạo tên layer dựa trên tên layer gốc (thêm tiền tố "GIS_" và chuẩn hóa tên)
        private static string GetLayerName(string sourceLayerName)
        {
            return NormalizeLayer(sourceLayerName);
        }

        // hàm thêm entity vào AutoCAD, gán layer và đính kèm thuộc tính (xdata)
        private static void AddEntity(
             Entity ent,
             string layerName,
             JToken properties,
             Database db,
             BlockTableRecord btr,
             Transaction tr)
        {
            EnsureLayer(db, tr, layerName);

            ent.Layer = layerName;

            btr.AppendEntity(ent);
            tr.AddNewlyCreatedDBObject(ent, true);

            AttachAttributes(db, tr, ent, properties);
        }

        // hàm vẽ điểm từ GeoJSON lên AutoCAD
        private static void DrawPoint(
            JToken feature,
            JToken geom,
            JToken properties,
            string layerName,
            Database db,
            BlockTableRecord btr,
            Transaction tr)
        {
            var c = geom["coordinates"];

            var pt = new DBPoint(new Point3d((double)c[0], (double)c[1], 0));

            string layer = GetLayerName(layerName);

            AddEntity(pt, layer, properties, db, btr, tr);
        }

        // hàm vẽ đường thẳng từ GeoJSON lên AutoCAD
        private static void DrawLineString(
             JToken feature,
             JToken geom,
             JToken properties,
             string layerName,
             Database db,
             BlockTableRecord btr,
             Transaction tr)
        {
            var pl = new Polyline();

            int i = 0;
            foreach (var p in geom["coordinates"])
            {
                pl.AddVertexAt(i++, new Point2d((double)p[0], (double)p[1]), 0, 0, 0);
            }

            string layer = GetLayerName(layerName);

            AddEntity(pl, layer, properties, db, btr, tr);
        }

        // hàm vẽ đa giác từ GeoJSON lên AutoCAD
        private static void DrawPolygon(
             JToken feature,
             JToken geom,
             JToken properties,
             string layerName,
             Database db,
             BlockTableRecord btr,
             Transaction tr)
        {
            foreach (var ring in geom["coordinates"])
            {
                var pl = new Polyline();

                int i = 0;
                foreach (var p in ring)
                {
                    pl.AddVertexAt(i++, new Point2d((double)p[0], (double)p[1]), 0, 0, 0);
                }

                pl.Closed = true;

                string layer = GetLayerName(layerName);

                AddEntity(pl, layer, properties, db, btr, tr);
            }
        }

        // hàm vẽ nhiều đường thẳng từ GeoJSON lên AutoCAD
        private static void DrawMultiLineString(
            JToken feature,
            JToken geom,
            JToken properties,
            string layerName,
            Database db,
            BlockTableRecord btr,
            Transaction tr)
        {
            foreach (var line in geom["coordinates"])
            {
                var pl = new Polyline();

                int i = 0;
                foreach (var p in line)
                {
                    pl.AddVertexAt(i++, new Point2d((double)p[0], (double)p[1]), 0, 0, 0);
                }

                string layer = GetLayerName(layerName);

                AddEntity(pl, layer, properties, db, btr, tr);
            }
        }

        // hàm vẽ nhiều đa giác từ GeoJSON lên AutoCAD
        private static void DrawMultiPolygon(
            JToken feature,
            JToken geom,
            JToken properties,
            string layerName,
            Database db,
            BlockTableRecord btr,
            Transaction tr)
        {
            foreach (var poly in geom["coordinates"])
            {
                foreach (var ring in poly)
                {
                    var pl = new Polyline();

                    int i = 0;
                    foreach (var p in ring)
                    {
                        pl.AddVertexAt(i++, new Point2d((double)p[0], (double)p[1]), 0, 0, 0);
                    }

                    pl.Closed = true;

                    string layer = GetLayerName(layerName);

                    AddEntity(pl, layer, properties, db, btr, tr);
                }
            }
        }

        //
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