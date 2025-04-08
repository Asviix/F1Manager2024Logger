using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography.X509Certificates;

namespace F1Manager2024Logger
{
    [PluginName("F1 Manager 2024 Plguin")]
    [PluginDescription("Makes F1 Manager 2024 Data available in SimHub Dash.")]
    [PluginAuthor("Thomas \"Asviix\" DEFRANCE")]
    public class TelemetryPlugin : IPlugin
    {

        public database database;

        
        public PluginManager PluginManager { get; set; }

        public void Init(PluginManager pluginManager)
        {
            this.PluginManager = pluginManager;

            pluginManager.AddProperty("F1M Connected", GetType(), typeof(bool), "Is F1 Manager connected");
            
        }

        public void End(PluginManager pluginManager)
        {
            this.PluginManager = null;
        }

        
    }
}
