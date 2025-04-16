using GameReaderCommon;
using SimHub.Plugins;
using Newtonsoft.Json;
using System;
using System.Windows.Media;
using System.Drawing.Text;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Security.Policy;
using System.Windows.Markup;
using SimHub.Plugins.DataPlugins.RGBDriver.LedsContainers.Groups;

namespace F1Manager2024Plugin
{
    [PluginDescription("F1 Manager 2024 Telemetry Plotter")]
    [PluginName("Plots telemetry from F1 Manager 2024 via memory-mapped file")]
    [PluginAuthor("Thomas DEFRANCE")]
    public class F1ManagerPlotter : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        public PluginManager PluginManager { get; set; }

        public F1Manager2024PluginSettings Settings;
        public MmfReader _mmfReader;
        private Exporter _exporter;
        private string _mmfStatus = "Not Connected";
        private bool _ismmfConnected;

        private bool IsmmfConnected => _ismmfConnected;
        private string MmfStatus => _mmfStatus;
        private DateTime _lastDataTime = DateTime.Now;
        private readonly object _dataLock = new object();
        private dynamic _lastData;

        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => "F1 Manager Plugin";

        // Add Drivers Properties
        readonly string[] carNames = new string[]
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
            pluginManager.AddProperty("Status_IsMMF_Connected", this.GetType(), typeof(bool));
            pluginManager.AddProperty("Status_MMF_Status", this.GetType(), typeof(string));

            PluginManager = pluginManager;

            Settings = this.ReadCommonSettings<F1Manager2024PluginSettings>("GeneralSettings", () => new F1Manager2024PluginSettings()
            {
                Path = null
            });
            
            _exporter = new Exporter(Settings);

            StartReading(Settings.Path);

            #region Init Properties

            // Add Session Properties
            pluginManager.AddProperty("TimeElapsed", GetType(), typeof(float), "Time Elapsed in the session.");
            pluginManager.AddProperty("TrackName", GetType(), typeof(int), "Track Name.");
            pluginManager.AddProperty("BestSessionTime", GetType(), typeof(float), "Best Time in the session.");
            pluginManager.AddProperty("RubberState", GetType(), typeof(int), "Rubber on Track.");
            pluginManager.AddProperty("SessionType", GetType(), typeof(string), "Type of the session.");
            pluginManager.AddProperty("SessionTypeShort", GetType(), typeof(string), "Short Type of the session.");
            pluginManager.AddProperty("AirTemp", GetType(), typeof(float), "Air Temperature in the session.");
            pluginManager.AddProperty("TrackTemp", GetType(), typeof(float), "Track Temperature in the session.");
            pluginManager.AddProperty("Weather", GetType(), typeof(string), "Weather in the session.");

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
            #endregion

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
                    if (_lastData == null)
                    {
                        UpdateStatus(false, "No data received");
                        return;
                    }
                    #region Update Properties
                    // Update Session Properties
                    UpdateValue("TrackName", _lastData?["Ferrari1"]?["telemetry"]?["session"]?["trackName"] ?? "Unknown");
                    UpdateValue("TimeElapsed", _lastData?["Ferrari1"]?["telemetry"]?["session"]?["timeElapsed"] ?? 0f);
                    UpdateValue("BestSessionTime", _lastData?["Ferrari1"]?["telemetry"]?["session"]?["bestSessionTime"] ?? 0f);
                    UpdateValue("RubberState", _lastData?["Ferrari1"]?["telemetry"]?["session"]?["rubber"] ?? 0);
                    UpdateValue("SessionType", _lastData?["Ferrari1"]?["telemetry"]?["session"]?["sessionType"] ?? "Unknown");
                    UpdateValue("SessionTypeShort", _lastData?["Ferrari1"]?["telemetry"]?["session"]?["sessionTypeShort"] ?? "Unknown");
                    UpdateValue("AirTemp", _lastData?["Ferrari1"]?["telemetry"]?["session"]?["weather"]?["airTemp"] ?? 0f);
                    UpdateValue("TrackTemp", _lastData?["Ferrari1"]?["telemetry"]?["session"]?["weather"]?["trackTemp"] ?? 0f);
                    UpdateValue("Weather", _lastData?["Ferrari1"]?["telemetry"]?["session"]?["weather"]?["weather"] ?? "Unknown");

                    // Update Drivers Properties
                    foreach (var car in carNames)
                    {
                        // Update historical data
                        if (LapOrTurnChanged(car))
                        {
                            UpdateHistoricalData(car, data[car]);
                        }

                        UpdateValue($"{car}_Position", (_lastData?[car]?["telemetry"]?["driver"]?["position"] ?? 0) + 1); // Adjust for 0-based index
                        UpdateValue($"{car}_DriverNumber", _lastData?[car]?["telemetry"]?["driver"]?["driverNumber"] ?? 0);
                        UpdateValue($"{car}_PitStopStatus", _lastData?[car]?["telemetry"]?["driver"]?["pitstopStatus"] ?? "Unknown");
                        // Status
                        UpdateValue($"{car}_TurnNumber", _lastData?[car]?["telemetry"]?["driver"]?["status"]?["turnNumber"] ?? 0);
                        UpdateValue($"{car}_CurrentLap", (_lastData?[car]?["telemetry"]?["driver"]?["status"]?["currentLap"] ?? 0) + 1); // Adjust for Index
                        // Timings
                        UpdateValue($"{car}_CurrentLapTime", _lastData?[car]?["telemetry"]?["driver"]?["timings"]?["currentLapTime"] ?? 0f);
                        UpdateValue($"{car}_DriverBestLap", _lastData?[car]?["telemetry"]?["driver"]?["timings"]?["driverBestLap"] ?? 0f);
                        UpdateValue($"{car}_LastLapTime", _lastData?[car]?["telemetry"]?["driver"]?["timings"]?["lastLapTime"] ?? 0f);
                        UpdateValue($"{car}_LastS1Time", _lastData?[car]?["telemetry"]?["driver"]?["timings"]?["sectors"]?["lastS1Time"] ?? 0f);
                        UpdateValue($"{car}_LastS2Time", _lastData?[car]?["telemetry"]?["driver"]?["timings"]?["sectors"]?["lastS2Time"] ?? 0f);
                        UpdateValue($"{car}_LastS3Time", _lastData?[car]?["telemetry"]?["driver"]?["timings"]?["sectors"]?["lastS3Time"] ?? 0f);
                        // Car telemetry
                        UpdateValue($"{car}_Speed", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["speed"] ?? 0);
                        UpdateValue($"{car}_Rpm", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["rpm"] ?? 0);
                        UpdateValue($"{car}_Gear", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["gear"] ?? 0);
                        UpdateValue($"{car}_Charge", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["charge"] ?? 0f);
                        UpdateValue($"{car}_Fuel", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["fuel"] ?? 0f);
                        // Tyres
                        UpdateValue($"{car}_TyreCompound", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["tyres"]?["compound"] ?? "Unknown");
                        UpdateValue($"{car}_flTemp", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["tyres"]?["temperature"]?["flTemp"] ?? 0f);
                        UpdateValue($"{car}_frTemp", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["tyres"]?["temperature"]?["frTemp"] ?? 0f);
                        UpdateValue($"{car}_rlTemp", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["tyres"]?["temperature"]?["rlTemp"] ?? 0f);
                        UpdateValue($"{car}_rrTemp", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["tyres"]?["temperature"]?["rrTemp"] ?? 0f);
                        UpdateValue($"{car}_flDeg", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["tyres"]?["wear"]?["flDeg"] ?? 0f);
                        UpdateValue($"{car}_frDeg", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["tyres"]?["wear"]?["frDeg"] ?? 0f);
                        UpdateValue($"{car}_rlDeg", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["tyres"]?["wear"]?["rlDeg"] ?? 0f);
                        UpdateValue($"{car}_rrDeg", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["tyres"]?["wear"]?["rrDeg"] ?? 0f);
                        // Modes
                        UpdateValue($"{car}_PaceMode", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["modes"]?["paceMode"] ?? "Unknown");
                        UpdateValue($"{car}_FuelMode", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["modes"]?["fuelMode"] ?? "Unknown");
                        UpdateValue($"{car}_ERSMode", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["modes"]?["ersMode"] ?? "Unknown");
                        UpdateValue($"{car}_DRSMode", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["modes"]?["drsMode"] ?? "Unknown");
                        // Components
                        UpdateValue($"{car}_EngineTemp", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["components"]?["engine"]?["engineTemp"] ?? 0f);
                        UpdateValue($"{car}_EngineDeg", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["components"]?["engine"]?["engineDeg"] ?? 0f);
                        UpdateValue($"{car}_GearboxDeg", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["components"]?["gearbox"]?["gearboxDeg"] ?? 0f);
                        UpdateValue($"{car}_ERSDeg", _lastData?[car]?["telemetry"]?["driver"]?["car"]?["components"]?["ers"]?["ersDeg"] ?? 0f);

                        // Write to CSV if needed
                        if (Settings.ExporterEnabled || Settings.TrackedDrivers.Contains(car))
                        {
                            if (LapOrTurnChanged(car))
                                _exporter.ExportData(_lastData, car);
                                
                        }
                    }
                    #endregion
                }

                UpdateStatus(true, "Connected");
            }
            catch
            {
                return;
            }
        }

        public void StartReading(string filePath)
        {
            _mmfReader = new MmfReader();

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                if (FileExistsRecursive(filePath))
                {
                    _mmfReader.StartReading(filePath);
                    _mmfReader.DataReceived += DataReceived;
                }
                else
                {
                    UpdateStatus(false, "File Not found");
                }
            }
            else
            {
                UpdateStatus(false, "Path is not set");
            }
        }

        public void StopReading()
        {
            _mmfReader.DataReceived -= DataReceived;
            UpdateStatus(false, "Path is not set");
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
            UpdateValue("Status_IsMMF_Connected", connected);
            UpdateValue("Status_MMF_Status", message);
        }

        private void UpdateValue(string data, object message)
        {
            PluginManager.SetPropertyValue<F1ManagerPlotter>(data, message);
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

        private class LastRecordedData
        {
            public int LastTurnNumber { get; set; } = -1;
            public int LastLapNumber { get; set; } = -1;
        }

        private readonly Dictionary<string, LastRecordedData> _lastRecordedData = new Dictionary<string, LastRecordedData>();

        private bool LapOrTurnChanged(string carName)
        {
            try
            {
                if (!_lastRecordedData.ContainsKey(carName))
                {
                    _lastRecordedData[carName] = new LastRecordedData();
                    return true;
                }

                int currentTurn = (int)(_lastData[carName]?["telemetry"]?["driver"]?["status"]?["turnNumber"] ?? -1);
                int currentLap = (int)(_lastData[carName]?["telemetry"]?["driver"]?["status"]?["currentLap"] ?? -1);

                bool shouldWrite = currentTurn != _lastRecordedData[carName].LastTurnNumber ||
                                   currentLap != _lastRecordedData[carName].LastLapNumber;

                _lastRecordedData[carName].LastTurnNumber = currentTurn;
                _lastRecordedData[carName].LastLapNumber = currentLap;

                return shouldWrite;
            }
            catch
            {
                return false;
            }
        }

        private readonly ConcurrentDictionary<string, Dictionary<int, Dictionary<int, dynamic>>> _carHistory = new ConcurrentDictionary<string, Dictionary<int, Dictionary<int, dynamic>>>();

        private readonly object _historyLock = new object();
        private const int MaxLapsToStore = 70; // Adjust as needed

        private void UpdateHistoricalData(string carName, dynamic currentData)
        {
            if (currentData == null) return;

            // Check for session reset
            float currentTime = (float)(currentData?["telemetry"]?["session"]?["timeElapsed"] ?? 0f);
            if (currentTime < 3f)
            {
                ClearAllHistory();
            }

            int currentLap = (int)(currentData?["telemetry"]?["driver"]?["status"]?["currentLap"] ?? 1);
            int currentTurn = (int)(currentData?["telemetry"]?["driver"]?["status"]?["turnNumber"] ?? 1);

            if (currentLap < 1 || currentTurn < 1) return; // Skip invalid data

            lock (_historyLock)
            {
                // Initialize data structure if needed
                if (!_carHistory.ContainsKey(carName))
                {
                    _carHistory[carName] = new Dictionary<int, Dictionary<int, dynamic>>();
                }

                if (!_carHistory[carName].ContainsKey(currentLap))
                {
                    _carHistory[carName][currentLap] = new Dictionary<int, dynamic>();

                    // Clean up old laps if we've reached max
                    if (_carHistory[carName].Count > MaxLapsToStore)
                    {
                        int oldestLap = _carHistory[carName].Keys.Min();
                        _carHistory[carName].Remove(oldestLap);
                    }
                }

                // Store turn data
                _carHistory[carName][currentLap][currentTurn] = currentData;

                // Update JSON properties
                UpdateLapProperty(carName, currentLap);
            }
        }

        private void UpdateLapProperty(string carName, int lapNumber)
        {
            if (!_carHistory.ContainsKey(carName) || !_carHistory[carName].ContainsKey(lapNumber))
                return;

            // Create property if it doesn't exist
            if (!PluginManager.GetAllPropertiesNames().Contains($"{carName}.History.Lap{lapNumber}"))
            {
                PluginManager.AddProperty(
                    $"{carName}.History.Lap{lapNumber}",
                    this.GetType(),
                    typeof(string),
                    null,
                    hidden: true
                );
            }

            // Serialize the complete lap data
            var lapData = new
            {
                LapNumber = lapNumber,
                Turns = _carHistory[carName][lapNumber]
                    .OrderBy(t => t.Key)
                    .ToDictionary(t => t.Key, t => t.Value)
            };

            PluginManager.SetPropertyValue(
                $"F1Manager.{carName}.History.Lap{lapNumber}",
                this.GetType(),
                JsonConvert.SerializeObject(lapData, Formatting.None)
            );
        }

        public void ClearAllHistory()
        {
            lock (_historyLock)
            {
                foreach (var car in _carHistory.Keys)
                {
                    // Reset current lap
                    PluginManager.SetPropertyValue(
                        $"F1Manager.{car}.History.CurrentLap",
                        this.GetType(),
                        null
                    );

                    // Reset all properties
                    for (int i = 1; i <= MaxLapsToStore; i++)
                    {
                        PluginManager.SetPropertyValue(
                            $"F1Manager.{car}.History.Lap{i}",
                            this.GetType(),
                            null
                        );
                    }
                }

                _carHistory.Clear();
            }
            SimHub.Logging.Current.Info("Cleared all historical data due to session reset");
        }

        public void End(PluginManager pluginManager)
        {
            _mmfReader.DataReceived -= DataReceived;
            _mmfReader.StopReading();

            // Save settings
            this.SaveCommonSettings("GeneralSettings", Settings);
        }
    }
}