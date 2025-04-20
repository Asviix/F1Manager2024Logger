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
using System.IO.Packaging;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using WoteverCommon;

namespace F1Manager2024Plugin
{
    [PluginDescription("Plots telemetry from F1 Manager 2024 via memory-mapped file")]
    [PluginName("F1 Manager 2024 Telemetry Plotter")]
    [PluginAuthor("Thomas DEFRANCE")]
    public class F1ManagerPlotter : IPlugin, IWPFSettingsV2
    {
        public PluginManager PluginManager { get; set; }

        public F1Manager2024PluginSettings Settings;
        public MmfReader _mmfReader;
        public Exporter _exporter;
        private DateTime _lastDataTime;
        private float _lastTimeElapsed;
        private readonly object _dataLock = new object();
        private Telemetry _lastData;

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
            pluginManager.AddProperty("Status_IsMemoryMap_Connected", this.GetType(), false);
            pluginManager.AddProperty("Status_MemoryMap_Status", this.GetType(), "Waiting for Mapped Memory");

            // Load Settings
            Settings = this.ReadCommonSettings<F1Manager2024PluginSettings>("GeneralSettings", () => new F1Manager2024PluginSettings());
            
            // Create new Exporter
            _exporter = new Exporter();

            // Create new Reader
            _mmfReader = new MmfReader();
            _mmfReader.StartReading("F1ManagerTelemetry");
            _mmfReader.DataReceived += DataReceived;

            #region Init Properties
            // Add Session Properties
            pluginManager.AddProperty("TimeSpeed", GetType(), typeof(float), "Time Fast-Forward Multiplicator.");
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
                pluginManager.AddProperty($"{name}_TireCompound", GetType(), typeof(string), "Tire Compound");
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

        private void DataReceived(Telemetry telemetry)
        {
            lock (_dataLock)
            {
                try
                {
                    _lastData = telemetry;

                    UpdateProperties(_lastData, _lastDataTime, _lastTimeElapsed);
                    UpdateStatus(true, "Connected");
                }
                catch (Exception)
                {
                    UpdateStatus(false, "Error processing data");
                }
            }
        }

        // Helper Functions
        class LastRecordedData
        {
            public int LastTurnNumber { get; set; }
            public int LastLapNumber { get; set; }
        }

        private readonly Dictionary<string, LastRecordedData> _lastRecordedData = new Dictionary<string, LastRecordedData>();

        private readonly ConcurrentDictionary<string, Dictionary<int, Dictionary<int, CarTelemetry>>> _carHistory = new ConcurrentDictionary<string, Dictionary<int, Dictionary<int, CarTelemetry>>>();

        private readonly object _historyLock = new object();
        private const int MaxLapsToStore = 70; // Adjust as needed
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

        private void UpdateStatus(bool connected, string message)
        {
            UpdateValue("Status_IsMemoryMap_Connected", connected);
            UpdateValue("Status_MemoryMap_Status", message);
        }

        private void UpdateProperties(Telemetry telemetry, DateTime lastDataTime, float lastTimeElapsed)
        {
            if (telemetry.Car == null || telemetry.Car.Length < 22) return;

            // Compute Time Fast-Forward Property
            var session = telemetry.Car[0].Driver.Session;
            if (DateTime.UtcNow - lastDataTime > TimeSpan.FromSeconds(1))
            {
                UpdateValue("TimeSpeed", (session.timeElapsed - lastTimeElapsed));
                _lastDataTime = DateTime.UtcNow;
                _lastTimeElapsed = session.timeElapsed;
            }

            // Update Session Properties
            UpdateValue("TrackName", GetTrackName(session.trackId));
            UpdateValue("TimeElapsed", session.timeElapsed);
            UpdateValue("BestSessionTime", session.bestSessionTime);
            UpdateValue("RubberState", session.rubber);
            UpdateValue("SessionType", GetSessionType(session.sessionType));
            UpdateValue("SessionTypeShort", GetShortSessionType(session.sessionType));
            UpdateValue("AirTemp", session.Weather.airTemp);
            UpdateValue("TrackTemp", session.Weather.trackTemp);
            UpdateValue("Weather", GetWeather(session.Weather.weather));

            // Update Drivers Properties
            for (int i = 0; i < telemetry.Car.Length && i < telemetry.Car.Length; i++)
            {
                var car = telemetry.Car[i];
                var name = carNames[i];

                // Update historical data
                if (LapOrTurnChanged(name, car))
                {
                    UpdateHistoricalData(name, car);

                    _exporter.ExportData(name, car, Settings);
                }

                UpdateValue($"{name}_Position", (car.Driver.position) + 1); // Adjust for 0-based index
                UpdateValue($"{name}_DriverNumber", car.Driver.driverNumber);
                UpdateValue($"{name}_PitStopStatus", GetPitStopStatus(car.pitStopStatus));
                // Status
                UpdateValue($"{name}_TurnNumber", car.Driver.turnNumber);
                UpdateValue($"{name}_CurrentLap", (car.currentLap) + 1); // Adjust for Index
                // Timings
                UpdateValue($"{name}_CurrentLapTime", car.Driver.currentLapTime);
                UpdateValue($"{name}_DriverBestLap", car.Driver.driverBestLap);
                UpdateValue($"{name}_LastLapTime", car.Driver.lastLapTime);
                UpdateValue($"{name}_LastS1Time", car.Driver.lastS1Time);
                UpdateValue($"{name}_LastS2Time", car.Driver.lastS2Time);
                UpdateValue($"{name}_LastS3Time", car.Driver.lastS3Time);
                // Car telemetry
                UpdateValue($"{name}_Speed", car.Driver.speed);
                UpdateValue($"{name}_Rpm", car.Driver.rpm);
                UpdateValue($"{name}_Gear", car.Driver.gear);
                UpdateValue($"{name}_Charge", car.charge);
                UpdateValue($"{name}_Fuel", car.fuel);
                // Tyres
                UpdateValue($"{name}_TireCompound", GetTireCompound(car.tireCompound));
                UpdateValue($"{name}_flTemp", car.flTemp);
                UpdateValue($"{name}_frTemp", car.frTemp);
                UpdateValue($"{name}_rlTemp", car.rlTemp);
                UpdateValue($"{name}_rrTemp", car.rrTemp);
                UpdateValue($"{name}_flDeg", car.flWear);
                UpdateValue($"{name}_frDeg", car.frWear);
                UpdateValue($"{name}_rlDeg", car.rlWear);
                UpdateValue($"{name}_rrDeg", car.rrWear);
                // Modes
                UpdateValue($"{name}_PaceMode", GetPaceMode(car.paceMode));
                UpdateValue($"{name}_FuelMode", GetFuelMode(car.fuelMode));
                UpdateValue($"{name}_ERSMode", GetERSMode(car.ersMode));
                UpdateValue($"{name}_DRSMode", GetDRSMode(car.Driver.drsMode));
                // Components
                UpdateValue($"{name}_EngineTemp", car.engineTemp);
                UpdateValue($"{name}_EngineDeg", car.engineWear);
                UpdateValue($"{name}_GearboxDeg", car.gearboxWear);
                UpdateValue($"{name}_ERSDeg", car.ersWear);
            }
        }

        public static string GetTrackName(int trackId)
        {
            return trackId switch
            {
                0 => "Invalid",
                1 => "Albert Park",
                2 => "Bahrain",
                3 => "Shanghai",
                4 => "Baku",
                5 => "Barcelona",
                6 => "Monaco",
                7 => "Montreal",
                8 => "Paul Ricard",
                9 => "Red Bull Ring",
                10 => "Silverstone",
                11 => "Jeddah",
                12 => "Hungaroring",
                13 => "Spa-Francorchamps",
                14 => "Monza",
                15 => "Marina Bay",
                16 => "Sochi",
                17 => "Suzuka",
                18 => "Hermanos Rodriguez",
                19 => "Circuit of the Americas",
                20 => "Interlagos",
                21 => "Yas Marina",
                22 => "Miami",
                23 => "Zandvoort",
                24 => "Imola",
                25 => "Las Vegas",
                26 => "Qatar",
                _ => "Unknown"
            };
        }

        public static string GetSessionType(int sessionId)
        {
            return sessionId switch
            {
                0 => "Practice 1",
                1 => "Practice 2",
                2 => "Practice 3",
                3 => "Qualifying 1",
                4 => "Qualifying 2",
                5 => "Qualifying 3",
                6 => "Race",
                7 => "Sprint",
                8 => "Sprint Qualifying 1",
                9 => "Sprint Qualifying 2",
                10 => "Sprint Qualifying 3",
                _ => "Unknown"
            };
        }

        public static string GetShortSessionType(int sessionId)
        {
            return sessionId switch
            {
                0 => "P1",
                1 => "P2",
                2 => "P3",
                3 => "Q1",
                4 => "Q2",
                5 => "Q3",
                6 => "R",
                7 => "S",
                8 => "SQ1",
                9 => "SQ2",
                10 => "SQ3",
                _ => "Unknown"
            };
        }

        public static string GetWeather(int weather)
        {
            return weather switch
            {
                0 => "None",
                1 => "Sunny",
                2 => "Partly Sunny",
                3 => "Cloudy",
                4 => "Light Rain",
                5 => "Moderate Rain",
                6 => "Heavy Rain",
                _ => "Unknown"
            };
        }

        public static string GetPitStopStatus(int pitStop)
        {
            return pitStop switch
            {
                0 => "None",
                1 => "Requested",
                2 => "Entering",
                3 => "Queuing",
                4 => "Stopped",
                5 => "Exiting",
                6 => "In Garage",
                7 => "Jack Up",
                8 => "Releasing",
                9 => "Car Setup",
                10 => "Pit Stop Approach",
                11 => "Pit Stop Penalty",
                12 => "Waiting for Release",
                _ => "Unknown"
            };
        }

        public static string GetTireCompound(int compound)
        {
            return compound switch
            {
                0 or 1 or 2 or 3 or 4 or 5 or 6 or 7 => "Soft",
                8 or 9 or 10 => "Medium",
                11 or 12 => "Hard",
                13 or 14 or 15 or 16 or 17 => "Intermediated",
                18 or 19 => "Wet",
                _ => "Unknown"
            };
        }

        public static string GetPaceMode(int paceMode)
        {
            return paceMode switch
            {
                0 => "Attack",
                1 => "Aggressive",
                2 => "Standard",
                3 => "Light",
                4 => "Conserve",
                _ => "Unknown"
            };
        }

        public static string GetFuelMode(int fuelMode)
        {
            return fuelMode switch
            {
                0 => "Push",
                1 => "Balanced",
                2 => "Conserve",
                _ => "Unknown"
            };
        }

        public static string GetERSMode(int ersMode)
        {
            return ersMode switch
            {
                0 => "Neutral",
                1 => "Harvest",
                2 => "Standard",
                3 => "Top Up",
                _ => "Unknown"
            };
        }

        public static string GetDRSMode(int drsMode)
        {
            return drsMode switch
            {
                0 => "Disabled",
                1 => "Detected",
                2 => "Enabled",
                3 => "Active",
                _ => "Unknown"
            };
        }

        private void UpdateValue(string data, object message)
        {
            PluginManager.SetPropertyValue<F1ManagerPlotter>(data, message);
        }

        private bool LapOrTurnChanged(string carName, CarTelemetry car)
        {
            try
            {
                if (!_lastRecordedData.ContainsKey(carName))
                {
                    _lastRecordedData[carName] = new LastRecordedData
                    {
                        LastLapNumber = car.currentLap + 1,
                        LastTurnNumber = car.Driver.turnNumber,
                    };
                    return true;
                }

                int currentTurn = car.Driver.turnNumber;
                int currentLap = car.currentLap + 1;

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

        private void UpdateHistoricalData(string carName, CarTelemetry car)
        {
            // Check for session reset
            float currentTime = (float)(car.Driver.Session.timeElapsed);
            if (currentTime < 3f)
            {
                ClearAllHistory();
            }

            int currentLap = car.currentLap + 1; // Don't forget to index
            int currentTurn = car.Driver.turnNumber;

            if (currentLap < 1 || currentTurn < 1) return; // Skip invalid data

            lock (_historyLock)
            {
                // Initialize data structure if needed
                if (!_carHistory.ContainsKey(carName))
                {
                    _carHistory[carName] = new Dictionary<int, Dictionary<int, CarTelemetry>>();
                }

                if (!_carHistory[carName].ContainsKey(currentLap))
                {
                    _carHistory[carName][currentLap] = new Dictionary<int, CarTelemetry>();

                    // Clean up old laps if we've reached max
                    if (_carHistory[carName].Count > MaxLapsToStore)
                    {
                        int oldestLap = _carHistory[carName].Keys.Min();
                        _carHistory[carName].Remove(oldestLap);
                    }
                }

                // Store turn data
                _carHistory[carName][currentLap][currentTurn] = car;

                // Update JSON properties
                UpdateLapProperty(carName, currentLap);
            }
        }

        private void UpdateLapProperty(string carName, int lapNumber)
        {
            if (!_carHistory.ContainsKey(carName) || !_carHistory[carName].ContainsKey(lapNumber))
                return;

            // Create property if it doesn't exist
            string propertyName = $"{carName}.History.Lap{lapNumber}";
            if (!PluginManager.GetAllPropertiesNames().Contains(propertyName))
            {
                PluginManager.AddProperty(
                    propertyName,
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
                    .ToDictionary(
                        t => t.Key,
                        t => new
                        {
                            TrackName = GetTrackName(t.Value.Driver.Session.trackId),
                            TimeElapsed = t.Value.Driver.Session.timeElapsed,
                            BestSessionTime = t.Value.Driver.Session.bestSessionTime,
                            RubberState = t.Value.Driver.Session.rubber,
                            SessionType = GetSessionType(t.Value.Driver.Session.sessionType),
                            SessionTypeShort = GetShortSessionType(t.Value.Driver.Session.sessionType),
                            AirTemp = t.Value.Driver.Session.Weather.airTemp,
                            TrackTemp = t.Value.Driver.Session.Weather.trackTemp,
                            Weather = GetWeather(t.Value.Driver.Session.Weather.weather),

                            Position = t.Value.Driver.position,
                            DriverNumber = t.Value.Driver.driverNumber,
                            PitStopStatus = GetPitStopStatus(t.Value.pitStopStatus),
                            TurnNumber = t.Value.Driver.turnNumber,
                            CurrentLap = t.Value.currentLap,
                            CurrentLapTime = t.Value.Driver.currentLapTime,
                            DriverBestLap = t.Value.Driver.driverBestLap,
                            LastLapTime = t.Value.Driver.lastLapTime,
                            LastS1Time = t.Value.Driver.lastS1Time,
                            LastS2Time = t.Value.Driver.lastS2Time,
                            LastS3Time = t.Value.Driver.lastS3Time,
                            Speed = t.Value.Driver.speed,
                            RPM = t.Value.Driver.rpm,
                            Gear = t.Value.Driver.gear,
                            Charge = t.Value.charge,
                            Fuel = t.Value.fuel,
                            TireCompound = GetTireCompound(t.Value.tireCompound),
                            FLTemp = t.Value.flTemp,
                            FRTemp = t.Value.frTemp,
                            RLTemp = t.Value.rlTemp,
                            RRTemp = t.Value.rrTemp,
                            FLWear = t.Value.flWear,
                            FRWear = t.Value.frWear,
                            RLWear = t.Value.rlWear,
                            RRWear = t.Value.rrWear,
                            PaceMode = GetPaceMode(t.Value.paceMode),
                            FuelMode = GetFuelMode(t.Value.fuelMode),
                            ERSMode = GetERSMode(t.Value.ersMode),
                            DRSMode = GetDRSMode(t.Value.Driver.drsMode),
                            EngineTemp = t.Value.engineTemp,
                            EngineWear = t.Value.engineWear,
                            GearboxWear = t.Value.gearboxWear,
                            ERSWear = t.Value.ersWear
                        }
                    )
            };

            PluginManager.SetPropertyValue<F1ManagerPlotter>(
                propertyName,
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

        public void ReloadSettings(F1Manager2024PluginSettings settings)
        {
            Settings = settings;
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