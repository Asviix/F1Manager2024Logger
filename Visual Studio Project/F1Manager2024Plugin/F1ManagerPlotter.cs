using SimHub.Plugins;
using GameReaderCommon;
using Newtonsoft.Json;
using System;
using System.Data;

namespace F1Manager2024Plugin
{
    [PluginName("F1 Manager 2024 Telemetry Plotter")]
    [PluginDescription("Plots telemetry from F1 Manager 2024 via memory-mapped file")]
    [PluginAuthor("Thomas DEFRANCE")]
    public class F1ManagerPlotter : IPlugin, IDataPlugin
    {
        public PluginManager PluginManager { get; set; }
        private mmfReader _mmfReader;
        private bool _ismmfConnected;
        private string _mmfStatus = "Not connected";

        private DateTime _lastDataTime = DateTime.MinValue;
        private readonly object _dataLock = new object();
        private dynamic _lastData;

        public bool IsmmfConnected => _ismmfConnected;
        public string mmfStatus => _mmfStatus;

        public void Init(PluginManager pluginManager)
        {
            PluginManager = pluginManager;
            _mmfReader = new mmfReader();
            _mmfReader.DataReceived += DataReceived; // Subscribe to the DataReceived event
            _mmfReader.StartReading(); // Start reading from the memory-mapped file

            // Register properties for SimHub
            pluginManager.AddProperty("F1Manager.Status.IsMMFConnected", this.GetType(), typeof(bool));
            pluginManager.AddProperty("F1Manager.Status.MMFStatus", this.GetType(), typeof(string));

            // Test Property
            pluginManager.AddProperty("F1Manager.Session.TrackName", GetType(), typeof(string));
        }

        private void DataReceived(string json)
        {
            try
            {
                if (json.StartsWith("ERROR:"))
                {
                    UpdateStatus(false, json.Substring(6));
                    return;
                }

                var data = JsonConvert.DeserializeObject<dynamic>(json);
                lock (_dataLock)
                {
                    _lastData = data;
                    _lastDataTime = DateTime.UtcNow;
                }

                UpdateStatus(true, "Connected");
            }
            catch (Exception ex)
            {
                UpdateStatus(false, ex.Message);
            }
        }

        private void UpdateStatus(bool connected, string message)
        {
            _ismmfConnected = connected;
            _mmfStatus = message;
            PluginManager.SetPropertyValue<F1ManagerPlotter>("F1Manager.Status.IsMMFConnected", connected);
            PluginManager.SetPropertyValue<F1ManagerPlotter>("F1Manager.Status.MMFStatus", message);
        }
        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            // Now actually using DataUpdate to push values to SimHub
            lock (_dataLock)
            {
                if (_lastData == null || (DateTime.UtcNow - _lastDataTime).TotalSeconds > 1)
                {
                    UpdateStatus(false, "No recent data");
                    return;
                }

                PluginManager.SetPropertyValue<F1ManagerPlotter>("F1Manager.Session.TrackName", _lastData.MyTeam1.telemetry.session.trackName);
            }
        }

        public void End(PluginManager pluginManager)
        {
            _mmfReader.DataReceived -= DataReceived; // Unsubscribe from the DataReceived event
            _mmfReader.StopReading(); // Stop reading from the memory-mapped file
        }
    }
}