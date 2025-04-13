using GameReaderCommon;
using SimHub.Plugins;
using Newtonsoft.Json;
using System;
using System.Windows.Media;

namespace F1Manager2024Plugin
{
    [PluginDescription("F1 Manager 2024 Telemetry Plotter")]
    [PluginAuthor("Plots telemetry from F1 Manager 2024 via memory-mapped file")]
    [PluginName("Thomas DEFRANCE")]
    public class F1ManagerPlotter : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        public PluginManager PluginManager { get; set; }

        public F1Manager2024PluginSettings Settings;
        public mmfReader _mmfReader;
        private string _mmfStatus = "Not Connected";
        private bool _ismmfConnected;

        private bool IsmmfConnected => _ismmfConnected;
        private string mmfStatus => _mmfStatus;
        private DateTime _lastDataTime = DateTime.Now;
        private readonly object _dataLock = new object();
        private dynamic _lastData;

        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => "F1 Manager Plugin Settings";

        private void UpdateValue(string data, object message)
        {
            PluginManager.SetPropertyValue<F1ManagerPlotter>(data, message);
        }

        public void Init(PluginManager pluginManager)
        {
            PluginManager = pluginManager;

            // Load settings FIRST
            Settings = this.ReadCommonSettings<F1Manager2024PluginSettings>("GeneralSettings", () => new F1Manager2024PluginSettings()
            {
                Path = null
            });


            // THEN create mmfReader with settings
            _mmfReader = new mmfReader();
            if (_mmfReader != null)
            { 
                _mmfReader.DataReceived += DataReceived; 
            }

            if (!string.IsNullOrWhiteSpace(Settings.Path))
            {
                _mmfReader.StartReading(Settings.Path);
            }
            SimHub.Logging.Current.Info("Starting plugin");

            // Register properties for SimHub
            pluginManager.AddProperty("F1Manager.Status.IsMMF_Connected", this.GetType(), typeof(bool));
            pluginManager.AddProperty("F1Manager.Status.MMF_Status", this.GetType(), typeof(string));

            // Test Property
            pluginManager.AddProperty("F1Manager.Session.TrackName", GetType(), typeof(string));

            // Declare an action which can be called
            this.AddAction(
                actionName: "IncrementSpeedWarning",
                actionStart: (a, b) =>
                {
                    SimHub.Logging.Current.Info("Speed warning changed");
                });

            // Declare an action which can be called
            this.AddAction(
                actionName: "DecrementSpeedWarning",
                actionStart: (a, b) =>
                {
                });

            // Declare an input which can be mapped
            this.AddInputMapping(
                inputName: "InputPressed",
                inputPressed: (a, b) => {/* One of the mapped input has been pressed   */},
                inputReleased: (a, b) => {/* One of the mapped input has been released */}
            );
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
            UpdateValue("F1Manager.Status.IsMMF_Connected", connected);
            UpdateValue("F1Manager.Status.MMF_Status", message);
        }

        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            lock (_dataLock)
            {
                if (_lastData == null || (DateTime.UtcNow - _lastDataTime).TotalSeconds > 1)
                {
                    UpdateStatus(false, "No recent data");
                    return;
                }

                UpdateValue("F1Manager.Session.TrackName", _lastData.MyTeam1.telemetry.session.trackName);
            }
        }

        public void End(PluginManager pluginManager)
        {
            _mmfReader.DataReceived -= DataReceived;
            _mmfReader.StopReading();

            // Save settings
            this.SaveCommonSettings("GeneralSettings", Settings);
        }

        public void UpdateMmfPath(string newPath)
        {
            Settings.Path = newPath;
            this.SaveCommonSettings("GeneralSettings", Settings);
        }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            try
            {
                return new SettingsControl(this);
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error("Failed to create settings control", ex);
                return new SettingsControl(); // Fallback to empty control
            }
        }
    }
}