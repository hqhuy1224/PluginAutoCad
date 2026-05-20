[Setup]
AppName=EKMAP GIS Plugin
AppVersion=1.0
DefaultDirName={commonappdata}\Autodesk\ApplicationPlugins
DisableDirPage=yes
OutputDir=Output
OutputBaseFilename=EKMAP_GIS_Plugin_Setup
Compression=lzma
SolidCompression=yes

[Files]
Source: "C:\Users\ekmapdata\source\repos\PluginAutoCad\Installer\bundle\PluginAutoCad.bundle\*"; \
DestDir: "{commonappdata}\Autodesk\ApplicationPlugins\PluginAutoCad.bundle"; \
Flags: recursesubdirs ignoreversion 