using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using System.Windows.Input;

[assembly: ExtensionApplication(typeof(PluginAutoCad.Commands.RibbonSetup))]

namespace PluginAutoCad.Commands
{
    public class RibbonSetup : IExtensionApplication
    {
        public void Initialize()
        {
            Application.Idle += OnIdle;
        }

        public void Terminate() { }

        private void OnIdle(object sender, System.EventArgs e)
        {
            Application.Idle -= OnIdle;
            CreateRibbon();
        }

        private void CreateRibbon()
        {
            RibbonControl ribbon = ComponentManager.Ribbon;

            if (ribbon == null)
                return;

            foreach (RibbonTab tab in ribbon.Tabs)
            {
                if (tab.Id == "GIS_TOOLS_TAB")
                    return;
            }

            RibbonTab newTab = new RibbonTab();
            newTab.Title = "GIS Tools";
            newTab.Id = "GIS_TOOLS_TAB";

            ribbon.Tabs.Add(newTab);

            RibbonPanelSource panelSource = new RibbonPanelSource();
            panelSource.Title = "GIS Plugin";

            RibbonPanel panel = new RibbonPanel();
            panel.Source = panelSource;

            newTab.Panels.Add(panel);

            RibbonButton button = new RibbonButton();

            button.Text = "🌍Login GIS";
            button.ShowText = true;  

            button.CommandParameter = "GISLOGIN ";
            button.CommandHandler = new RibbonCommandHandler();

            panelSource.Items.Add(button);
        }
    }

    public class RibbonCommandHandler : ICommand
    {
        public bool CanExecute(object parameter) => true;

        public event System.EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;

            RibbonButton btn = parameter as RibbonButton;

            if (btn != null)
            {
                doc.SendStringToExecute(btn.CommandParameter.ToString(), true, false, false);
            }
        }
    }
}