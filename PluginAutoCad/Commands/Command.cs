using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using PluginAutoCad.Core;
using PluginAutoCad.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PluginAutoCad
{
    public class GISCommands
    {
        [CommandMethod("GISLOGIN")]
        public async void GisLogin()
        {
            var ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                if (!Session.IsLoggedIn)
                {
                    using (var loginForm = new LoginForm())
                    {
                        if (loginForm.ShowDialog() != DialogResult.OK || !Session.IsLoggedIn)
                            return;
                    }
                }               
                ed.WriteMessage($"\n✓ Đăng nhập thành công: {Session.Username}");
                Session.Clear();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nLỗi: {ex.Message}");
            }
        }
    }
}
