using PluginAutoCad.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginAutoCad.Services
{
    public static class ServiceDetector
    {
        public static (string baseUrl, string serviceType) ParseGeoUrl(string url)
        {
            url = url.Trim().ToLower();

            if (url.EndsWith("/wms"))
            {
                return (url, "wms");
            }

            if (url.EndsWith("/wfs"))
            {
                return (url, "wfs");
            }

            throw new Exception("Geo URL must end with /wms or /wfs");
        }
    }
}