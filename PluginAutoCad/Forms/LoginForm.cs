using Autodesk.AutoCAD.Geometry;
using PluginAutoCad.Core;
using PluginAutoCad.Forms;
using PluginAutoCad.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace PluginAutoCad
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
        }
        public virtual IntPtr Token { get; }

        //label server url
        private void label1_Click(object sender, EventArgs e)
        {
                
        }

        //label username
        private void label2_Click(object sender, EventArgs e)
        {

        }

        //label password
        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        // hàm load layer WMS và vẽ lên AutoCAD
        private async Task LoadAndDrawWmsLayers()
        {
            var ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                var geo = new GeoService(Session.AccessToken);
                var xml = await geo.GetWmsCapabilities(Session.GeoBaseUrl);
                var layers = LayerParser.ParseWmsLayers(xml);

                if (layers.Count == 0)
                {
                    MessageBox.Show("Không tìm thấy layer WMS nào!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // test layer[0]
                //string layerName = layers[0];
                using (var view = ed.GetCurrentView())
                {
                    foreach (var item in layers)
                    {
                        var bbox = LayerParser.GetLayerBoundingBox(xml, item);

                        double minX = bbox.MinX;
                        double minY = bbox.MinY;
                        double maxX = bbox.MaxX;
                        double maxY = bbox.MaxY;

                        string imagePath = await geo.GetWmsMapImageAsync(
                            Session.GeoBaseUrl,
                            item.ToString(),
                            minX, minY, maxX, maxY,
                            1200, 800,
                            "EPSG:4326");

                        var insertPoint = new Point3d(minX, minY, 0);

                        GeoCadRenderer.AttachWmsImage(imagePath, insertPoint, 1.0);

                        ed.WriteMessage($"\n✓ Đã vẽ layer [{item}] thành công!");
                    }
                }

            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nLỗi: {ex.Message}");
                MessageBox.Show($"Lỗi khi vẽ layer:\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //hàm load layer WFS và vẽ lên AutoCAD
        private async Task LoadAndDrawWfsLayers()
        {
            var ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                var geo = new GeoService(Session.AccessToken);

                var xml = await geo.GetWfsCapabilities(Session.GeoBaseUrl);
                var layers = LayerParser.ParseWfsLayers(xml);

                if (layers.Count == 0)
                {
                    MessageBox.Show("Không tìm thấy layer WFS nào!");
                    return;
                }

                //string layerName = layers[0];
                foreach (var item in layers)
                {
                    ed.WriteMessage($"\nĐang load WFS layer: {item}");

                    string geojson = await geo.GetWfsFeaturesAsync(
                        Session.GeoBaseUrl,
                        item
                    );

                    GeoCadRenderer.DrawGeoJsonToCad(geojson);

                    ed.WriteMessage($"\n✓ Đã vẽ layer WFS: {item}");
                }

               
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nLỗi WFS: {ex.Message}");
                MessageBox.Show(ex.Message);
            }
        }

        //button login
        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            string authUrl = "http://localhost/auth"; 
            string geoUrl = txturl.Text.Trim();       
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            try
            {
                BtnLogin.Enabled = false;

                var auth = new AuthService(authUrl);
                var token = await auth.LoginAsync(username, password);

                var parsed = ServiceDetector.ParseGeoUrl(geoUrl);

                Session.AccessToken = token;
                Session.GeoBaseUrl = geoUrl;
                Session.ServiceType = parsed.serviceType;

                MessageBox.Show("Login OK" + "");

                //await Task.WhenAll(
                //     LoadAndDrawWmsLayers(),
                //     LoadAndDrawWfsLayers()
                //);

                if (Session.ServiceType == "wms")
                {
                    await LoadAndDrawWmsLayers();
                }
                else if (Session.ServiceType == "wfs")
                {
                    await LoadAndDrawWfsLayers();
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch {
                MessageBox.Show($"Sai username hoặc password", "Login fail", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                BtnLogin.Enabled = true;
            }
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {

        }
    }
}
