using System.Collections.Generic;

namespace PluginAutoCad.Core
{
    public static class Session
    {
        // Auth
        public static string AccessToken;

        public static string RefeshToken;

        // GIS
        public static string GeoBaseUrl;

        // User info
        public static string Username;


        public static List<string> Layers { get; set; } = new List<string>();
        //public static string CurrentCRS { get; set; } = "EPSG:4326"; 

        public static string ServiceType { get; set; }

        public static bool IsLoggedIn => !string.IsNullOrEmpty(AccessToken);
    }
}

