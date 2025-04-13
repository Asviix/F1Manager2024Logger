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

        // Add Drivers Properties
        string[] carNames = new string[]
        {
                "Ferrari1", "Ferrari2",
                "McLaren1", "McLaren2",
                "RedBull1", "RedBull2",
                "Mercedes1", "Mercedes2",
                "Alpine1", "Alpine2",
                "Williams1", "Williams2",
                "Haas1", "Haas2",
                "RacingBulls1", "RacingBulls2",
                "KickSauber1", "KickSauber2",
                "AstonMartin1", "AstonMartin2",
                "MyTeam1", "MyTeam2"
        };

        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("Starting Plugin");

            // Register properties for SimHub
            pluginManager.AddProperty("F1Manager.Status.IsMMF_Connected", this.GetType(), typeof(bool));
            pluginManager.AddProperty("F1Manager.Status.MMF_Status", this.GetType(), typeof(string));

            PluginManager = pluginManager;

            Settings = this.ReadCommonSettings<F1Manager2024PluginSettings>("GeneralSettings", () => new F1Manager2024PluginSettings()
            {
                Path = null
            });

            _mmfReader = new mmfReader();

            if (!string.IsNullOrWhiteSpace(Settings.Path))
            {
                if (FileExistsRecursive(Settings.Path))
                {
                    _mmfReader.StartReading(Settings.Path);
                    _mmfReader.DataReceived += DataReceived;
                }
                else
                {
                    UpdateStatus(false, "File Not found");
                    SimHub.Logging.Current.Info($"File not found: {Settings.Path}");
                }
            }
            else
            {
                UpdateStatus(false, "Path is not set");
                SimHub.Logging.Current.Info("Path is not set");
            }

            // Add Session Property
            pluginManager.AddProperty("F1Manager.session.timeElapsed", GetType(), typeof(float), "Time Elapsed in the session.");
            pluginManager.AddProperty("F1Manager.session.trackName", GetType(), typeof(int), "Track Name.");
            pluginManager.AddProperty("F1Manager.session.bestSessionTime", GetType(), typeof(float), "Best Time in the session.");
            pluginManager.AddProperty("F1Manager.session.rubberState", GetType(), typeof(int), "Rubber on Track.");
            pluginManager.AddProperty("F1Manager.session.sessionType", GetType(), typeof(string), "Type of the session.");
            pluginManager.AddProperty("F1Manager.session.sessionTypeShort", GetType(), typeof(string), "Short Type of the session.");
            pluginManager.AddProperty("F1Manager.session.airTemp", GetType(), typeof(float), "Air Temperature in the session.");
            pluginManager.AddProperty("F1Manager.session.trackTemp", GetType(), typeof(float), "Track Temperature in the session.");
            pluginManager.AddProperty("F1Manager.session.weather", GetType(), typeof(string), "Weather in the session.");

            foreach (var name in carNames)
            {
                // Position and basic info
                pluginManager.AddProperty($"{name}_Position", GetType(), typeof(int), "Position");
                pluginManager.AddProperty($"{name}_DriverNumber", GetType(), typeof(int), "Driver Number");
                pluginManager.AddProperty($"{name}_PitStopStatus", GetType(), typeof(string), "Pit Stop Status");

                // Status
                pluginManager.AddProperty($"{name}_TurnNumber", GetType(), typeof(int), "Turn Number");
                pluginManager.AddProperty($"{name}_CurrentLap", GetType(), typeof(int), "Current Lap");

                // Timings
                pluginManager.AddProperty($"{name}_CurrentLapTime", GetType(), typeof(float), "Current Lap Time");
                pluginManager.AddProperty($"{name}_DriverBestLap", GetType(), typeof(float), "Driver Best Lap");
                pluginManager.AddProperty($"{name}_LastLapTime", GetType(), typeof(float), "Last Lap Time");
                pluginManager.AddProperty($"{name}_LastS1Time", GetType(), typeof(float), "Last Sector 1 Time");
                pluginManager.AddProperty($"{name}_LastS2Time", GetType(), typeof(float), "Last Sector 2 Time");
                pluginManager.AddProperty($"{name}_LastS3Time", GetType(), typeof(float), "Last Sector 3 Time");

                // Car telemetry
                pluginManager.AddProperty($"{name}_Speed", GetType(), typeof(int), "Speed (km/h)");
                pluginManager.AddProperty($"{name}_Rpm", GetType(), typeof(int), "RPM");
                pluginManager.AddProperty($"{name}_Gear", GetType(), typeof(int), "Gear");
                pluginManager.AddProperty($"{name}_Charge", GetType(), typeof(float), "ERS Charge");
                pluginManager.AddProperty($"{name}_Fuel", GetType(), typeof(float), "Fuel");

                // Tyres
                pluginManager.AddProperty($"{name}_TyreCompound", GetType(), typeof(string), "Tyre Compound");
                pluginManager.AddProperty($"{name}_flTemp", GetType(), typeof(float), "Front Left Temp");
                pluginManager.AddProperty($"{name}_frTemp", GetType(), typeof(float), "Front Right Temp");
                pluginManager.AddProperty($"{name}_rlTemp", GetType(), typeof(float), "Rear Left Temp");
                pluginManager.AddProperty($"{name}_rrTemp", GetType(), typeof(float), "Rear Right Temp");
                pluginManager.AddProperty($"{name}_flDeg", GetType(), typeof(float), "Front Left Wear");
                pluginManager.AddProperty($"{name}_frDeg", GetType(), typeof(float), "Front Right Wear");
                pluginManager.AddProperty($"{name}_rlDeg", GetType(), typeof(float), "Rear Left Wear");
                pluginManager.AddProperty($"{name}_rrDeg", GetType(), typeof(float), "Rear Right Wear");

                // Modes
                pluginManager.AddProperty($"{name}_PaceMode", GetType(), typeof(string), "Pace Mode");
                pluginManager.AddProperty($"{name}_FuelMode", GetType(), typeof(string), "Fuel Mode");
                pluginManager.AddProperty($"{name}_ERSMode", GetType(), typeof(string), "ERS Mode");
                pluginManager.AddProperty($"{name}_DRSMode", GetType(), typeof(string), "DRS Mode");

                // Components
                pluginManager.AddProperty($"{name}_EngineTemp", GetType(), typeof(float), "Engine Temp");
                pluginManager.AddProperty($"{name}_EngineDeg", GetType(), typeof(float), "Engine Wear");
                pluginManager.AddProperty($"{name}_GearboxDeg", GetType(), typeof(float), "Gearbox Wear");
                pluginManager.AddProperty($"{name}_ERSDeg", GetType(), typeof(float), "ERS Wear");
            }

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

                    // Update Session Properties
                    UpdateValue("F1Manager.session.TrackName", _lastData["MyTeam1"]["telemetry"]["session"]["trackName"]);
                    UpdateValue("F1Manager.session.TimeElapsed", _lastData["MyTeam1"]["telemetry"]["session"]["timeElapsed"]);
                    UpdateValue("F1Manager.session.BestSessionTime", _lastData["MyTeam1"]["telemetry"]["session"]["bestSessionTime"]);
                    UpdateValue("F1Manager.session.RubberState", _lastData["MyTeam1"]["telemetry"]["session"]["rubber"]);
                    UpdateValue("F1Manager.session.SessionType", _lastData["MyTeam1"]["telemetry"]["session"]["sessionType"]);
                    UpdateValue("F1Manager.session.SessionTypeShort", _lastData["MyTeam1"]["telemetry"]["session"]["sessionTypeShort"]);
                    UpdateValue("F1Manager.session.AirTemp", _lastData["MyTeam1"]["telemetry"]["session"]["weather"]["airTemp"]);
                    UpdateValue("F1Manager.session.TrackTemp", _lastData["MyTeam1"]["telemetry"]["session"]["weather"]["trackTemp"]);
                    UpdateValue("F1Manager.session.Weather", _lastData["MyTeam1"]["telemetry"]["session"]["weather"]["weather"]);

                    // Update Drivers Properties
                    foreach (var car in carNames)
                    {
                        UpdateValue($"{car}_Position", _lastData[car]["telemetry"]["driver"]["position"]);
                        UpdateValue($"{car}_DriverNumber", _lastData[car]["telemetry"]["driver"]["driverNumber"]);
                        UpdateValue($"{car}_PitStopStatus", _lastData[car]["telemetry"]["driver"]["pitstopStatus"]);
                        // Status
                        UpdateValue($"{car}_TurnNumber", _lastData[car]["telemetry"]["driver"]["status"]["turnNumber"]);
                        UpdateValue($"{car}_CurrentLap", _lastData[car]["telemetry"]["driver"]["status"]["currentLap"]);
                        // Timings
                        UpdateValue($"{car}_CurrentLapTime", _lastData[car]["telemetry"]["driver"]["timings"]["currentLapTime"]);
                        UpdateValue($"{car}_DriverBestLap", _lastData[car]["telemetry"]["driver"]["timings"]["driverBestLap"]);
                        UpdateValue($"{car}_LastLapTime", _lastData[car]["telemetry"]["driver"]["timings"]["lastLapTime"]);
                        UpdateValue($"{car}_LastS1Time", _lastData[car]["telemetry"]["driver"]["timings"]["sectors"]["lastS1Time"]);
                        UpdateValue($"{car}_LastS2Time", _lastData[car]["telemetry"]["driver"]["timings"]["sectors"]["lastS2Time"]);
                        UpdateValue($"{car}_LastS3Time", _lastData[car]["telemetry"]["driver"]["timings"]["sectors"]["lastS3Time"]);
                        // Car telemetry
                        UpdateValue($"{car}_Speed", _lastData[car]["telemetry"]["driver"]["car"]["speed"]);
                        UpdateValue($"{car}_Rpm", _lastData[car]["telemetry"]["driver"]["car"]["rpm"]);
                        UpdateValue($"{car}_Gear", _lastData[car]["telemetry"]["driver"]["car"]["gear"]);
                        UpdateValue($"{car}_Charge", _lastData[car]["telemetry"]["driver"]["car"]["charge"]);
                        UpdateValue($"{car}_Fuel", _lastData[car]["telemetry"]["driver"]["car"]["fuel"]);
                        // Tyres
                        UpdateValue($"{car}_TyreCompound", _lastData[car]["telemetry"]["driver"]["car"]["tyres"]["compound"]);
                        UpdateValue($"{car}_flTemp", _lastData[car]["telemetry"]["driver"]["car"]["tyres"]["temperature"]["flTemp"]);
                        UpdateValue($"{car}_frTemp", _lastData[car]["telemetry"]["driver"]["car"]["tyres"]["temperature"]["frTemp"]);
                        UpdateValue($"{car}_rlTemp", _lastData[car]["telemetry"]["driver"]["car"]["tyres"]["temperature"]["rlTemp"]);
                        UpdateValue($"{car}_rrTemp", _lastData[car]["telemetry"]["driver"]["car"]["tyres"]["temperature"]["rrTemp"]);
                        UpdateValue($"{car}_flDeg", _lastData[car]["telemetry"]["driver"]["car"]["tyres"]["wear"]["flDeg"]);
                        UpdateValue($"{car}_frDeg", _lastData[car]["telemetry"]["driver"]["car"]["tyres"]["wear"]["frDeg"]);
                        UpdateValue($"{car}_rlDeg", _lastData[car]["telemetry"]["driver"]["car"]["tyres"]["wear"]["rlDeg"]);
                        UpdateValue($"{car}_rrDeg", _lastData[car]["telemetry"]["driver"]["car"]["tyres"]["wear"]["rrDeg"]);
                        // Modes
                        UpdateValue($"{car}_PaceMode", _lastData[car]["telemetry"]["driver"]["car"]["modes"]["paceMode"]);
                        UpdateValue($"{car}_FuelMode", _lastData[car]["telemetry"]["driver"]["car"]["modes"]["fuelMode"]);
                        UpdateValue($"{car}_ERSMode", _lastData[car]["telemetry"]["driver"]["car"]["modes"]["ersMode"]);
                        UpdateValue($"{car}_DRSMode", _lastData[car]["telemetry"]["driver"]["car"]["modes"]["drsMode"]);
                        // Components
                        UpdateValue($"{car}_EngineTemp", _lastData[car]["telemetry"]["driver"]["car"]["components"]["engine"]["engineTemp"]);
                        UpdateValue($"{car}_EngineDeg", _lastData[car]["telemetry"]["driver"]["car"]["components"]["engine"]["engineDeg"]);
                        UpdateValue($"{car}_GearboxDeg", _lastData[car]["telemetry"]["driver"]["car"]["components"]["gearbox"]["gearboxDeg"]);
                        UpdateValue($"{car}_ERSDeg", _lastData[car]["telemetry"]["driver"]["car"]["components"]["ers"]["ersDeg"]);
                    }
                }

                UpdateStatus(true, "Connected");
            }
            catch
            {
                return;
            }
        }

        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            lock (_dataLock)
            {
                if (_lastData == null || (DateTime.UtcNow - _lastDataTime).TotalSeconds > 1)
                {
                    return;
                }
            }
        }

        private void UpdateStatus(bool connected, string message)
        {
            _ismmfConnected = connected;
            _mmfStatus = message;
            UpdateValue("F1Manager.Status.IsMMF_Connected", connected);
            UpdateValue("F1Manager.Status.MMF_Status", message);
        }

        private void UpdateValue(string data, object message)
        {
            PluginManager.SetPropertyValue<F1ManagerPlotter>(data, message);
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
                SimHub.Logging.Current.Info("Failed to create settings control", ex);
                return new SettingsControl(); // Fallback to empty control
            }
        }

        private bool FileExistsRecursive(string filePath)
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error($"Error checking file existence: {ex.Message}");
                return false;
            }
        }
    }
}