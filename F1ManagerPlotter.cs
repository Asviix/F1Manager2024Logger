using SimHub.Plugins;
using Newtonsoft.Json;
using System;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using WoteverCommon;
using WoteverCommon.Extensions;

namespace F1Manager2024Plugin
{
    [PluginDescription("Makes F1 Manager 2024 Data available in SimHub !")]
    [PluginName("F1 Manager 2024 Telemetry Plotter")]
    [PluginAuthor("Thomas DEFRANCE")]
    public class F1ManagerPlotter : IPlugin, IWPFSettingsV2
    {
        public double version = 1.1; 
        public PluginManager PluginManager { get; set; }

        public F1Manager2024PluginSettings Settings;
        public MmfReader _mmfReader;
        public Exporter _exporter;
        private DateTime _lastDataTime;
        private float _lastTimeElapsed;
        private readonly object _dataLock = new();
        public Telemetry _lastData;

        private readonly float ExpectedCarValueSteam = 8021.863281f;
        private readonly float ExpectedCarValueEpic = 8214.523438f;
        private int CarsOnGrid = 22;

        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => "F1 Manager Plugin";

        // Add Drivers Properties
        public readonly string[] carNames = new string[]
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

        // Initialize the Plugin.
        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("----F1 MANAGER 2024 SIMHUB PLUGIN----");
            SimHub.Logging.Current.Info("Starting Plugin...");

            // Load Settings
            SimHub.Logging.Current.Info("Loading Settings...");
            Settings = this.ReadCommonSettings<F1Manager2024PluginSettings>("GeneralSettings", () => new F1Manager2024PluginSettings());
            SimHub.Logging.Current.Info("Settings Loaded.");

            SimHub.Logging.Current.Info("Unpacking Save File...");
            UnrealSaveUnpacker.UnpackSaveFile();
            SimHub.Logging.Current.Info("Save File Unpacked.");

            SimHub.Logging.Current.Info("Registering Debug Properties...");

            // Register properties for SimHub
            pluginManager.AddProperty("DEBUG_Status_IsMemoryMap_Connected", this.GetType(), false);
            pluginManager.AddProperty("DEBUG_Status_Game_Connected", this.GetType(), false);
            pluginManager.AddProperty("DEBUG_Game_Status", this.GetType(), typeof(string));

            // Create new Exporter
            SimHub.Logging.Current.Info("Creating Exporter...");
            _exporter = new Exporter();
            SimHub.Logging.Current.Info("Exporter Created.");

            // Create new Reader
            SimHub.Logging.Current.Info("Creating Reader...");
            _mmfReader = new MmfReader();
            _mmfReader.StartReading("F1ManagerTelemetry");
            SimHub.Logging.Current.Info("Reader Created.");
            _mmfReader.DataReceived += DataReceived;

            SimHub.Logging.Current.Info("Registering Properties...");

            #region Init Properties
            // Add Settings Properties

            string trackedDriver1 = Settings.TrackedDriversDashboard.Length > 0
                                ? Settings.TrackedDriversDashboard[0]
                                : null;

            string trackedDriver2 = Settings.TrackedDriversDashboard.Length > 1
                                ? Settings.TrackedDriversDashboard[1]
                                : null;

            pluginManager.AddProperty("TrackedDriver1", this.GetType(), trackedDriver1);
            pluginManager.AddProperty("TrackedDriver2", this.GetType(), trackedDriver2);

            // Add Game Properties
            pluginManager.AddProperty("CameraFocusedOn", GetType(), typeof(string), "The Car name the camera is focus on.");

            // Add Session Properties
            pluginManager.AddProperty("TimeSpeed", GetType(), typeof(float), "Time Fast-Forward Multiplicator.");
            pluginManager.AddProperty("TimeElapsed", GetType(), typeof(float), "Time Elapsed in the session.");
            pluginManager.AddProperty("LapsRemaining", GetType(), typeof(float), "Laps Remaining in the race.");
            pluginManager.AddProperty("TimeRemaining", GetType(), typeof(float), "Time Remaining in the session.");
            pluginManager.AddProperty("TrackName", GetType(), typeof(string), "Track Name.");
            pluginManager.AddProperty("BestSessionTime", GetType(), typeof(float), "Best Time in the session.");
            pluginManager.AddProperty("BestS1Time", GetType(), typeof(float), "Best S1 Time in the session.");
            pluginManager.AddProperty("BestS2Time", GetType(), typeof(float), "Best S2 Time in the session.");
            pluginManager.AddProperty("BestS3Time", GetType(), typeof(float), "Best S3 Time in the session.");
            pluginManager.AddProperty("RubberState", GetType(), typeof(float), "Rubber on Track.");
            pluginManager.AddProperty("SessionType", GetType(), typeof(string), "Type of the session.");
            pluginManager.AddProperty("SessionTypeShort", GetType(), typeof(string), "Short Type of the session.");
            pluginManager.AddProperty("AirTemp", GetType(), typeof(float), "Air Temperature in the session.");
            pluginManager.AddProperty("TrackTemp", GetType(), typeof(float), "Track Temperature in the session.");
            pluginManager.AddProperty("Weather", GetType(), typeof(string), "Weather in the session.");
            pluginManager.AddProperty("WaterOnTrack", GetType(), typeof(float), "Millimeters of water on track.");

            pluginManager.AddProperty("P1_Car", GetType(), typeof(string), "Name of the Car Currently in P1.");
            pluginManager.AddProperty("P2_Car", GetType(), typeof(string), "Name of the Car Currently in P2.");
            pluginManager.AddProperty("P3_Car", GetType(), typeof(string), "Name of the Car Currently in P3.");
            pluginManager.AddProperty("P4_Car", GetType(), typeof(string), "Name of the Car Currently in P4.");
            pluginManager.AddProperty("P5_Car", GetType(), typeof(string), "Name of the Car Currently in P5.");
            pluginManager.AddProperty("P6_Car", GetType(), typeof(string), "Name of the Car Currently in P6.");
            pluginManager.AddProperty("P7_Car", GetType(), typeof(string), "Name of the Car Currently in P7.");
            pluginManager.AddProperty("P8_Car", GetType(), typeof(string), "Name of the Car Currently in P8.");
            pluginManager.AddProperty("P9_Car", GetType(), typeof(string), "Name of the Car Currently in P9.");
            pluginManager.AddProperty("P10_Car", GetType(), typeof(string), "Name of the Car Currently in P10.");
            pluginManager.AddProperty("P11_Car", GetType(), typeof(string), "Name of the Car Currently in P11.");
            pluginManager.AddProperty("P12_Car", GetType(), typeof(string), "Name of the Car Currently in P12.");
            pluginManager.AddProperty("P13_Car", GetType(), typeof(string), "Name of the Car Currently in P13.");
            pluginManager.AddProperty("P14_Car", GetType(), typeof(string), "Name of the Car Currently in P14.");
            pluginManager.AddProperty("P15_Car", GetType(), typeof(string), "Name of the Car Currently in P15.");
            pluginManager.AddProperty("P16_Car", GetType(), typeof(string), "Name of the Car Currently in P16.");
            pluginManager.AddProperty("P17_Car", GetType(), typeof(string), "Name of the Car Currently in P17.");
            pluginManager.AddProperty("P18_Car", GetType(), typeof(string), "Name of the Car Currently in P18.");
            pluginManager.AddProperty("P19_Car", GetType(), typeof(string), "Name of the Car Currently in P19.");
            pluginManager.AddProperty("P20_Car", GetType(), typeof(string), "Name of the Car Currently in P20.");
            pluginManager.AddProperty("P21_Car", GetType(), typeof(string), "Name of the Car Currently in P21.");
            pluginManager.AddProperty("P22_Car", GetType(), typeof(string), "Name of the Car Currently in P22.");

            foreach (var name in carNames)
            {
                // Position and basic info
                pluginManager.AddProperty($"{name}_Position", GetType(), typeof(int), "Position");
                pluginManager.AddProperty($"{name}_PointsGain", GetType(), typeof(int), "Points gained at this position.");
                pluginManager.AddProperty($"{name}_DriverNumber", GetType(), typeof(int), "Driver Number");
                pluginManager.AddProperty($"{name}_DriverFirstName", GetType(), typeof(string), "Driver First Name");
                pluginManager.AddProperty($"{name}_DriverLastName", GetType(), typeof(string), "Driver Last Name");
                pluginManager.AddProperty($"{name}_DriverTeamName", GetType(), typeof(string), "Name of the Driver's Team.");
                pluginManager.AddProperty($"{name}_DriverTeamColor", GetType(), typeof(string), "Color of the Driver's Team.");
                pluginManager.AddProperty($"{name}_PitStopStatus", GetType(), typeof(string), "Pit Stop Status");
                pluginManager.AddProperty($"{name}_EstimatedPositionAfterPit", GetType(), typeof(int), "Estimated Position after Pit Stop.");

                // Status
                pluginManager.AddProperty($"{name}_TurnNumber", GetType(), typeof(int), "Turn Number");
                pluginManager.AddProperty($"{name}_CurrentLap", GetType(), typeof(int), "Current Lap");
                pluginManager.AddProperty($"{name}_DistanceTravelled", GetType(), typeof(float), "Number of meters travelled in the current lap.");

                // Timings
                pluginManager.AddProperty($"{name}_CurrentLapTime", GetType(), typeof(float), "Current Lap Time");
                pluginManager.AddProperty($"{name}_DriverBestLap", GetType(), typeof(float), "Driver Best Lap");
                pluginManager.AddProperty($"{name}_LastLapTime", GetType(), typeof(float), "Last Lap Time");
                pluginManager.AddProperty($"{name}_LastS1Time", GetType(), typeof(float), "Last Sector 1 Time");
                pluginManager.AddProperty($"{name}_BestS1Time", GetType(), typeof(float), "Best Sector 1 Time");
                pluginManager.AddProperty($"{name}_LastS2Time", GetType(), typeof(float), "Last Sector 2 Time");
                pluginManager.AddProperty($"{name}_BestS2Time", GetType(), typeof(float), "Best Sector 2 Time");
                pluginManager.AddProperty($"{name}_LastS3Time", GetType(), typeof(float), "Last Sector 3 Time");
                pluginManager.AddProperty($"{name}_BestS3Time", GetType(), typeof(float), "Best Sector 3 Time");
                pluginManager.AddProperty($"{name}_SpeedST", GetType(), typeof(float), "Speed at the Speed Trap");

                // Car telemetry
                pluginManager.AddProperty($"{name}_Speed", GetType(), typeof(int), "Speed (km/h)");
                pluginManager.AddProperty($"{name}_Rpm", GetType(), typeof(int), "RPM");
                pluginManager.AddProperty($"{name}_Gear", GetType(), typeof(int), "Gear");
                pluginManager.AddProperty($"{name}_Charge", GetType(), typeof(float), "ERS Charge");
                pluginManager.AddProperty($"{name}_EnergyHarvested", GetType(), typeof(float), "ERS Energy Harvested.");
                pluginManager.AddProperty($"{name}_EnergyDeployed", GetType(), typeof(float), "ERS Energy Deployed.");
                pluginManager.AddProperty($"{name}_Fuel", GetType(), typeof(float), "Fuel");
                pluginManager.AddProperty($"{name}_FuelDelta", GetType(), typeof(float), "Fuel Delta");

                // Tires
                pluginManager.AddProperty($"{name}_TireCompound", GetType(), typeof(string), "Tire Compound");
                pluginManager.AddProperty($"{name}_TireAge", GetType(), typeof(int), "Tire Compound");
                pluginManager.AddProperty($"{name}_flTemp", GetType(), typeof(float), "Front Left Temp");
                pluginManager.AddProperty($"{name}_flSurfaceTemp", GetType(), typeof(float), "Front Left Surface Temp");
                pluginManager.AddProperty($"{name}_flBrakeTemp", GetType(), typeof(float), "Front Left Brake Temp");
                pluginManager.AddProperty($"{name}_frTemp", GetType(), typeof(float), "Front Right Temp");
                pluginManager.AddProperty($"{name}_frSurfaceTemp", GetType(), typeof(float), "Front Right Surface Temp");
                pluginManager.AddProperty($"{name}_frBrakeTemp", GetType(), typeof(float), "Front Right Brake Temp");
                pluginManager.AddProperty($"{name}_rlTemp", GetType(), typeof(float), "Rear Left Temp");
                pluginManager.AddProperty($"{name}_rlSurfaceTemp", GetType(), typeof(float), "Rear Left Surface Temp");
                pluginManager.AddProperty($"{name}_rlBrakeTemp", GetType(), typeof(float), "Rear Left Brake Temp");
                pluginManager.AddProperty($"{name}_rrTemp", GetType(), typeof(float), "Rear Right Temp");
                pluginManager.AddProperty($"{name}_rrSurfaceTemp", GetType(), typeof(float), "Rear Right Surface Temp");
                pluginManager.AddProperty($"{name}_rrBrakeTemp", GetType(), typeof(float), "Rear Right Brake Temp");
                pluginManager.AddProperty($"{name}_flDeg", GetType(), typeof(float), "Front Left Wear");
                pluginManager.AddProperty($"{name}_frDeg", GetType(), typeof(float), "Front Right Wear");
                pluginManager.AddProperty($"{name}_rlDeg", GetType(), typeof(float), "Rear Left Wear");
                pluginManager.AddProperty($"{name}_rrDeg", GetType(), typeof(float), "Rear Right Wear");

                // Modes
                pluginManager.AddProperty($"{name}_PaceMode", GetType(), typeof(string), "Pace Mode");
                pluginManager.AddProperty($"{name}_FuelMode", GetType(), typeof(string), "Fuel Mode");
                pluginManager.AddProperty($"{name}_ERSMode", GetType(), typeof(string), "ERS Mode");
                pluginManager.AddProperty($"{name}_DRSMode", GetType(), typeof(string), "DRS Mode");
                pluginManager.AddProperty($"{name}_ERSAssist", GetType(), typeof(bool), "DRS Battle Assist");
                pluginManager.AddProperty($"{name}_OvertakeAggression", GetType(), typeof(string), "Overtake Aggression");
                pluginManager.AddProperty($"{name}_DefendApproach", GetType(), typeof(string), "Defend Approach");
                pluginManager.AddProperty($"{name}_DriveCleanAir", GetType(), typeof(bool), "Drive in Clean Air");
                pluginManager.AddProperty($"{name}_AvoidHighKerbs", GetType(), typeof(bool), "Avoid High Risk Kerbs");
                pluginManager.AddProperty($"{name}_DontFightTeammate", GetType(), typeof(bool), "Don't Fight Teammate");

                // Components
                pluginManager.AddProperty($"{name}_EngineTemp", GetType(), typeof(float), "Engine Temp");
                pluginManager.AddProperty($"{name}_EngineDeg", GetType(), typeof(float), "Engine Wear");
                pluginManager.AddProperty($"{name}_GearboxDeg", GetType(), typeof(float), "Gearbox Wear");
                pluginManager.AddProperty($"{name}_ERSDeg", GetType(), typeof(float), "ERS Wear");

                // Opponents Data
                pluginManager.AddProperty($"{name}_NameOfCarBehind", GetType(), typeof(string), "The name of the car behind that driver.");
                pluginManager.AddProperty($"{name}_NameOfCarAhead", GetType(), typeof(string), "The name of the car ahead that driver.");
                pluginManager.AddProperty($"{name}_GapAhead", GetType(), typeof(float), "The gap ahead of that driver.");
                pluginManager.AddProperty($"{name}_GapBehind", GetType(), typeof(float), "The gap behind of that driver.");
                pluginManager.AddProperty($"{name}_GapToLeader", GetType(), typeof(float), "The gap to the leader of that driver.");
            }
            #endregion

            SimHub.Logging.Current.Info("Started!");
            SimHub.Logging.Current.Info("----F1 MANAGER 2024 SIMHUB PLUGIN----");
        }

        public void DataReceived(Telemetry telemetry)
        {
            lock (_dataLock)
            {

                if (telemetry.carFloatValue != ExpectedCarValueSteam && telemetry.carFloatValue != ExpectedCarValueEpic) { UpdateStatus(true, false, "Game not in Session."); return; }
                try
                {
                    UnrealSaveUnpacker.UnpackSaveFile();

                    _lastData = telemetry;

                    UpdateProperties(_lastData, _lastDataTime, _lastTimeElapsed);
                    UpdateStatus(true, true, "Receiving Data");
                }
                catch (Exception ex)
                {
                    UpdateStatus(true, false, ex.ToString());
                }
            }
        }

        #region Helper Methods

        // Last Recorded Data used by the LapOrTurnChanged Method.
        public class LastRecordedData
        {
            public bool NewLapStarted { get; set; }
            public int LastTurnNumber { get; set; }
            public bool LastTurnRecorded { get; set; }
            public int LastLapNumber { get; set; }
            public int LastTire {  get; set; }
            public int LastTireChangeLap { get; set; }
            public float S1Time { get; set; }
            public float S2Time { get; set; }
            public float S3Time { get; set; }
            public float BestS1Time { get; set; }
            public float BestS2Time { get; set; }
            public float BestS3Time { get; set; }
            public void UpdateSectorTimes(float s1, float s2, float s3)
            {
                if (s1 != 0) S1Time = s1;
                if (s2 != 0) S2Time = s2;
                if (s3 != 0) S3Time = s3;

                if ((S1Time < BestS1Time || BestS1Time == 0))
                {
                    BestS1Time = S1Time;
                }

                if ((S2Time < BestS2Time || BestS2Time == 0))
                {
                    BestS2Time = S2Time;
                }

                if ((S3Time < BestS3Time || BestS3Time == 0))
                {
                    BestS3Time = S3Time;
                }
            }

            public int SpeedST { get; set; }
            public bool SpeedSTRecorded { get; set; }

            public void UpdateSTSpeed(int speed, int lap, float distance, float STDistance)
            {
                if (distance > (STDistance - 240) && distance < (STDistance + 240) && SpeedSTRecorded == false)
                {
                    SpeedST = speed;
                    SpeedSTRecorded = true;
                }
            }
        }

        // Get Best sector Times
        public (float MinS1, float MinS2, float MinS3) GetLowestSectorTimes()
        {
            float minS1 = float.MaxValue;
            float minS2 = float.MaxValue;
            float minS3 = float.MaxValue;

            foreach (var entry in _lastRecordedData.Values)
            {
                if (entry.S1Time != 0 && entry.S1Time < minS1)
                {
                    minS1 = entry.S1Time;
                }

                if (entry.S2Time != 0 && entry.S2Time < minS2)
                {
                    minS2 = entry.S2Time;
                }

                if (entry.S3Time != 0 && entry.S3Time < minS3)
                {
                    minS3 = entry.S3Time;
                }
            }

            // Handle case where no valid times were found (return 0 or another default)
            minS1 = minS1 == float.MaxValue ? 0 : minS1;
            minS2 = minS2 == float.MaxValue ? 0 : minS2;
            minS3 = minS3 == float.MaxValue ? 0 : minS3;

            return (minS1, minS2, minS3);
        }

        // Dictionary for the Last Recorded Data.
        private readonly Dictionary<string, LastRecordedData> _lastRecordedData = new();

        // Dictionary for the Car Historical Data.
        private readonly ConcurrentDictionary<string, Dictionary<int, Dictionary<int, Telemetry>>> _carHistory = new();

        // Dictionary for the Standings
        public static readonly ConcurrentDictionary<int, string> CarPositions = new();

        // Dictionary for the best lap times.
        public static readonly ConcurrentDictionary<string, float> CarBestLapTimes = new();

        // Dictionary for the gaps to leader.
        public static readonly ConcurrentDictionary<int, float> GapsToLeader = new();

        // GetDriversNames used by the SettingsControl to initialize the driver's list.
        public Dictionary<string, (string FirstName, string LastName)> GetDriversNames()
        {
            var result = new Dictionary<string, (string, string)>();

            if (_lastData.Car == null) return result;

            for (int i = 0; i < _lastData.Car.Length; i++)
            {
                var driverId = _lastData.Car[i].Driver.driverId;
                var name = carNames[i];
                var firstName = TelemetryHelpers.GetDriverFirstName(driverId);
                var lastName = TelemetryHelpers.GetDriverLastName(driverId);
                result[name] = (firstName, lastName);
            }

            return result;
        }

        private readonly object _historyLock = new();
        private const int MaxLapsToStore = 100; // Adjust as needed
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

        // Update the status of the Debug Properties in SimHub.
        private void UpdateStatus(bool connected, bool connected2, string message2)
        {
            UpdateValue("DEBUG_Status_IsMemoryMap_Connected", connected);
            UpdateValue("DEBUG_Status_Game_Connected", connected2);
            UpdateValue("DEBUG_Game_Status", message2);
        }

        // Update all properties in SimHub
        private void UpdateProperties(Telemetry telemetry, DateTime lastDataTime, float lastTimeElapsed)
        {
            foreach (var car in telemetry.Car)
            {
                string carName = carNames[car.Driver.position];
            }

            // Compute Time Fast-Forward Property
            var session = telemetry.Session;
            if (DateTime.UtcNow - lastDataTime > TimeSpan.FromSeconds(1))
            {
                UpdateValue("TimeSpeed", (session.timeElapsed - lastTimeElapsed));
                _lastDataTime = DateTime.UtcNow;
                _lastTimeElapsed = session.timeElapsed;
            }

            // Get Best Session Times
            var (bestS1, bestS2, bestS3) = GetLowestSectorTimes();


            // Update Game Properties
            UpdateValue("CameraFocusedOn", carNames[telemetry.cameraFocus]);

            // Update Session Properties
            UpdateValue("TrackName", TelemetryHelpers.GetTrackName(session.trackId));
            UpdateValue("TimeElapsed", session.timeElapsed);
            UpdateValue("LapsRemaining", TelemetryHelpers.GetSessionRemaining(telemetry, carNames).LapsRemaining);
            UpdateValue("TimeRemaining", TelemetryHelpers.GetSessionRemaining(telemetry, carNames).TimeRemaining);
            UpdateValue("BestSessionTime", TelemetryHelpers.GetBestSessionTime(telemetry));
            UpdateValue("BestS1Time", bestS1);
            UpdateValue("BestS2Time", bestS2);
            UpdateValue("BestS3Time", bestS3);
            UpdateValue("RubberState", session.rubber);
            UpdateValue("SessionType", TelemetryHelpers.GetSessionType(session.sessionType));
            UpdateValue("SessionTypeShort", TelemetryHelpers.GetShortSessionType(session.sessionType));
            UpdateValue("AirTemp", session.Weather.airTemp);
            UpdateValue("TrackTemp", session.Weather.trackTemp);
            UpdateValue("Weather", TelemetryHelpers.GetWeather(session.Weather.weather));
            UpdateValue("WaterOnTrack", session.Weather.waterOnTrack);

            // Set the number of cars on the grid
            if (telemetry.Session.sessionType == 6 || telemetry.Session.sessionType == 7) // Race session
            {
                // First check if we might have a 20-car grid (car20 has RPM 0)
                if (telemetry.Car[20].Driver.driverId == 0)
                {
                    // Count cars that are not retired AND have RPM > 0
                    CarsOnGrid = telemetry.Car.Count(c =>
                        c.Driver.driverId > 0 &&
                        c.pitStopStatus != 6); // 6 = In Garage (retired)
                }
                else
                {
                    // Full 22-car grid (minus any retirements)
                    CarsOnGrid = telemetry.Car.Count(c =>
                        c.pitStopStatus != 6); // Count all non-retired cars
                }
            }
            else
            {
                // For non-race sessions, use the original logic
                if (telemetry.Car[20].Driver.driverId == 0)
                {
                    CarsOnGrid = telemetry.Car.Count(c => c.Driver.driverId > 0);
                }
                else
                {
                    CarsOnGrid = 22;
                }
            }

            // Update Drivers Properties
            for (int i = 0; i < carNames.Length; i++)
            {
                var car = telemetry.Car[i];
                var name = carNames[i];

                // Update historical data
                if (LapOrTurnChanged(name, car, telemetry.Session.trackId))
                {
                    UpdateHistoricalData(name, telemetry, i, CarsOnGrid);

                    _exporter.ExportData(name, telemetry, i, Settings, _lastRecordedData[name].ToJson(), bestS1, bestS2, bestS3);
                }


                if ((session.sessionType is 6 or 7) && (car.pitStopStatus is 6 || car.Driver.rpm == 0))
                {
                    ResetProperties(telemetry, i, name, car.Driver.rpm);
                    continue;
                }

                if ((session.sessionType is not 6 or 7) && (car.Driver.driverId == 0))
                {
                    ResetProperties(telemetry, i, name, car.Driver.rpm);
                    continue;
                }

                if (TireChanged(name, car))
                {
                    UpdateValue($"{name}_TireAge", (car.currentLap + 1) - _lastRecordedData[name].LastTireChangeLap);
                }

                // Update Session Standings and Dictionary
                UpdateValue($"P{car.Driver.position + 1}_Car", name);
                CarPositions.AddOrUpdate(car.Driver.position, name, (_, __) => name);

                // Update best lap times dictionary
                CarBestLapTimes.AddOrUpdate(name, car.Driver.driverBestLap, (_, __) => car.Driver.driverBestLap);

                // Update Gap to Leader Dictionary
                GapsToLeader.AddOrUpdate(
                    telemetry.Car[i].Driver.position,
                    TelemetryHelpers.GetGapLeader(telemetry, telemetry.Car[i].Driver.position, i, carNames),
                    (_, __) => TelemetryHelpers.GetGapLeader(telemetry, telemetry.Car[i].Driver.position, i, carNames)
                );

                // Get Ahead and Behind Name
                string carAheadName = TelemetryHelpers.GetNameOfCarAhead(car.Driver.position, i, carNames);
                string carBehindName = TelemetryHelpers.GetNameOfCarBehind(car.Driver.position, i, carNames, CarsOnGrid);


                _lastRecordedData[name].UpdateSectorTimes(car.Driver.lastS1Time, car.Driver.lastS2Time, car.Driver.lastS3Time);
                _lastRecordedData[name].UpdateSTSpeed(car.Driver.speed, car.currentLap, car.Driver.distanceTravelled, TelemetryHelpers.GetSpeedTrapDistance(session.trackId));

                UpdateValue($"{name}_Position", (car.Driver.position) + 1); // Adjust for 0-based index
                UpdateValue($"{name}_PointsGain", TelemetryHelpers.GetPointsGained(car.Driver.position + 1, session.sessionType, TelemetryHelpers.GetBestSessionTime(telemetry) == car.Driver.driverBestLap));
                UpdateValue($"{name}_DriverNumber", car.Driver.driverNumber);
                UpdateValue($"{name}_PitStopStatus", TelemetryHelpers.GetPitStopStatus(car.pitStopStatus, session.sessionType));
                UpdateValue($"{name}_EstimatedPositionAfterPit", TelemetryHelpers.GetEstimatedPositionAfterPit(telemetry, telemetry.Car[i].Driver.position, i, carNames, CarsOnGrid));
                // Status
                UpdateValue($"{name}_TurnNumber", _lastRecordedData[name].LastTurnNumber);
                UpdateValue($"{name}_DriverFirstName", TelemetryHelpers.GetDriverFirstName(car.Driver.driverId));
                UpdateValue($"{name}_DriverLastName", TelemetryHelpers.GetDriverLastName(car.Driver.driverId));
                UpdateValue($"{name}_DriverTeamName", TelemetryHelpers.GetTeamName(car.Driver.teamId, Settings));
                UpdateValue($"{name}_DriverTeamColor", TelemetryHelpers.GetTeamColor(car.Driver.teamId, Settings));
                UpdateValue($"{name}_CurrentLap", (car.currentLap) + 1); // Adjust for Index
                UpdateValue($"{name}_DistanceTravelled", car.Driver.distanceTravelled);
                // Timings
                UpdateValue($"{name}_CurrentLapTime", car.Driver.currentLapTime);
                UpdateValue($"{name}_DriverBestLap", car.Driver.driverBestLap);
                UpdateValue($"{name}_LastLapTime", car.Driver.lastLapTime);
                UpdateValue($"{name}_LastS1Time", _lastRecordedData[name].S1Time);
                UpdateValue($"{name}_BestS1Time", _lastRecordedData[name].BestS1Time);
                UpdateValue($"{name}_LastS2Time", _lastRecordedData[name].S2Time);
                UpdateValue($"{name}_BestS2Time", _lastRecordedData[name].BestS2Time);
                UpdateValue($"{name}_LastS3Time", _lastRecordedData[name].S3Time);
                UpdateValue($"{name}_BestS3Time", _lastRecordedData[name].BestS3Time);
                UpdateValue($"{name}_SpeedST", _lastRecordedData[name].SpeedST);
                // Car telemetry
                UpdateValue($"{name}_Speed", car.Driver.speed);
                UpdateValue($"{name}_Rpm", car.Driver.rpm);
                UpdateValue($"{name}_Gear", car.Driver.gear);
                UpdateValue($"{name}_Charge", car.charge);
                UpdateValue($"{name}_EnergyHarvested", car.energyHarvested);
                UpdateValue($"{name}_EnergyDeployed", car.energySpent);
                UpdateValue($"{name}_Fuel", car.fuel);
                UpdateValue($"{name}_FuelDelta", car.fuelDelta);
                // Tires
                UpdateValue($"{name}_TireCompound", TelemetryHelpers.GetTireCompound(car.tireCompound, Settings));
                UpdateValue($"{name}_TireAge", (car.currentLap + 1) - _lastRecordedData[name].LastTireChangeLap);
                UpdateValue($"{name}_flSurfaceTemp", car.flSurfaceTemp);
                UpdateValue($"{name}_flTemp", car.flTemp);
                UpdateValue($"{name}_flBrakeTemp", car.flBrakeTemp);
                UpdateValue($"{name}_frSurfaceTemp", car.frSurfaceTemp);
                UpdateValue($"{name}_frTemp", car.frTemp);
                UpdateValue($"{name}_frBrakeTemp", car.frBrakeTemp);
                UpdateValue($"{name}_rlSurfaceTemp", car.rlSurfaceTemp);
                UpdateValue($"{name}_rlTemp", car.rlTemp);
                UpdateValue($"{name}_rlBrakeTemp", car.rlBrakeTemp);
                UpdateValue($"{name}_rrSurfaceTemp", car.rrSurfaceTemp);
                UpdateValue($"{name}_rrTemp", car.rrTemp);
                UpdateValue($"{name}_rrBrakeTemp", car.rrBrakeTemp);
                UpdateValue($"{name}_flDeg", car.flWear);
                UpdateValue($"{name}_frDeg", car.frWear);
                UpdateValue($"{name}_rlDeg", car.rlWear);
                UpdateValue($"{name}_rrDeg", car.rrWear);
                // Modes
                UpdateValue($"{name}_PaceMode", TelemetryHelpers.GetPaceMode(car.paceMode));
                UpdateValue($"{name}_FuelMode", TelemetryHelpers.GetFuelMode(car.fuelMode));
                UpdateValue($"{name}_ERSMode", TelemetryHelpers.GetERSMode(car.ersMode));
                UpdateValue($"{name}_DRSMode", TelemetryHelpers.GetDRSMode(car.Driver.drsMode));
                UpdateValue($"{name}_ERSAssist", Convert.ToBoolean(car.Driver.ERSAssist));
                UpdateValue($"{name}_OvertakeAggression", TelemetryHelpers.GetOvertakeMode(car.Driver.OvertakeAggression));
                UpdateValue($"{name}_DefendApproach", TelemetryHelpers.GetDefendMode(car.Driver.DefendApproach));
                UpdateValue($"{name}_DriveCleanAir", Convert.ToBoolean(car.Driver.DriveCleanAir));
                UpdateValue($"{name}_AvoidHighKerbs", Convert.ToBoolean(car.Driver.AvoidHighKerbs));
                UpdateValue($"{name}_DontFightTeammate", Convert.ToBoolean(car.Driver.DontFightTeammate));
                // Components
                UpdateValue($"{name}_EngineTemp", car.engineTemp);
                UpdateValue($"{name}_EngineDeg", car.engineWear);
                UpdateValue($"{name}_GearboxDeg", car.gearboxWear);
                UpdateValue($"{name}_ERSDeg", car.ersWear);

                // Opponents Data
                UpdateValue($"{name}_NameOfCarBehind", carBehindName);
                UpdateValue($"{name}_NameOfCarAhead", carAheadName);
                UpdateValue($"{name}_GapBehind", TelemetryHelpers.GetGapBehind(telemetry, telemetry.Car[i].Driver.position, i, carNames, CarsOnGrid));
                UpdateValue($"{name}_GapAhead", TelemetryHelpers.GetGapInFront(telemetry, telemetry.Car[i].Driver.position, i, carNames));
                UpdateValue($"{name}_GapToLeader", TelemetryHelpers.GetGapLeader(telemetry, telemetry.Car[i].Driver.position, i, carNames));
            }
        }

        private void ResetProperties(Telemetry telemetry, int i, string carName, int rpm)
        {
            int position = telemetry.Car[i].Driver.position + 1;

            if (carName is "MyTeam1") position = 21;
            if (carName is "MyTeam2") position = 22;

            // Update Session Standings and Dictionary
            UpdateValue($"P{position}_Car", carName);
            CarPositions.AddOrUpdate(position - 1, carName, (_, __) => carName);

            // Position and basic info
            UpdateValue($"{carName}_Position", position);
            UpdateValue($"{carName}_DriverNumber", 0);
            UpdateValue($"{carName}_EstimatedPositionAfterPit", 0);

            // Status
            UpdateValue($"{carName}_TurnNumber", 0);
            UpdateValue($"{carName}_CurrentLap", 0);
            UpdateValue($"{carName}_DistanceTravelled", 0f);

            // Timings
            UpdateValue($"{carName}_CurrentLapTime", 0f);
            UpdateValue($"{carName}_DriverBestLap", 0f);
            UpdateValue($"{carName}_LastLapTime", 0f);
            UpdateValue($"{carName}_LastS1Time", 0f);
            UpdateValue($"{carName}_BestS1Time", 0f);
            UpdateValue($"{carName}_LastS2Time", 0f);
            UpdateValue($"{carName}_BestS2Time", 0f);
            UpdateValue($"{carName}_LastS3Time", 0f);
            UpdateValue($"{carName}_BestS3Time", 0f);
            UpdateValue($"{carName}_SpeedST", 0);

            // Car telemetry
            UpdateValue($"{carName}_Speed", 0);
            UpdateValue($"{carName}_Rpm", 0);
            UpdateValue($"{carName}_Gear", 0);
            UpdateValue($"{carName}_Charge", 0f);
            UpdateValue($"{carName}_EnergyHarvested", 0f);
            UpdateValue($"{carName}_EnergyDeployed", 0f);
            UpdateValue($"{carName}_Fuel", 0f);
            UpdateValue($"{carName}_FuelDelta", 0f);

            // Tires
            UpdateValue($"{carName}_TireCompound", null);
            UpdateValue($"{carName}_TireAge", 0);
            UpdateValue($"{carName}_flSurfaceTemp", 0f);
            UpdateValue($"{carName}_flTemp", 0f);
            UpdateValue($"{carName}_flBrakeTemp", 0f);
            UpdateValue($"{carName}_frSurfaceTemp", 0f);
            UpdateValue($"{carName}_frTemp", 0f);
            UpdateValue($"{carName}_frBrakeTemp", 0f);
            UpdateValue($"{carName}_rlSurfaceTemp", 0f);
            UpdateValue($"{carName}_rlTemp", 0f);
            UpdateValue($"{carName}_rlBrakeTemp", 0f);
            UpdateValue($"{carName}_rrSurfaceTemp", 0f);
            UpdateValue($"{carName}_rrTemp", 0f);
            UpdateValue($"{carName}_rrBrakeTemp", 0f);
            UpdateValue($"{carName}_flDeg", 0f);
            UpdateValue($"{carName}_frDeg", 0f);
            UpdateValue($"{carName}_rlDeg", 0f);
            UpdateValue($"{carName}_rrDeg", 0f);

            // Modes
            UpdateValue($"{carName}_PaceMode", null);
            UpdateValue($"{carName}_FuelMode", null);
            UpdateValue($"{carName}_ERSMode", null);
            UpdateValue($"{carName}_DRSMode", null);

            // Components
            UpdateValue($"{carName}_EngineTemp", 0f);
            UpdateValue($"{carName}_EngineDeg", 0f);
            UpdateValue($"{carName}_GearboxDeg", 0f);
            UpdateValue($"{carName}_ERSDeg", 0f);
            UpdateValue($"{carName}_ERSAssist", false);
            UpdateValue($"{carName}_OvertakeAggression", null);
            UpdateValue($"{carName}_DefendApproach", null);
            UpdateValue($"{carName}_DriveCleanAir", false);
            UpdateValue($"{carName}_AvoidHighKerbs", false);
            UpdateValue($"{carName}_DontFightTeammate", false);

            // Opponents Data
            UpdateValue($"{carName}_NameOfCarBehind", null);
            UpdateValue($"{carName}_NameOfCarAhead", null);
            UpdateValue($"{carName}_GapBehind", 0f);
            UpdateValue($"{carName}_GapAhead", 0f);
            UpdateValue($"{carName}_GapToLeader", 0f);

            if (telemetry.Car[i].Driver.driverId != 0)
            {
                UpdateValue($"{carName}_DriverFirstName", TelemetryHelpers.GetDriverFirstName(telemetry.Car[i].Driver.driverId));
                UpdateValue($"{carName}_DriverLastName", TelemetryHelpers.GetDriverLastName(telemetry.Car[i].Driver.driverId));
                UpdateValue($"{carName}_DriverTeamName", TelemetryHelpers.GetTeamName(telemetry.Car[i].Driver.teamId, Settings));
                UpdateValue($"{carName}_PitStopStatus", "RETIRED");
            }
            else
            {
                UpdateValue($"{carName}_DriverFirstName", "NOT LOADED");
                UpdateValue($"{carName}_DriverLastName", "NOT LOADED");
                UpdateValue($"{carName}_DriverTeamName", "NOT LOADED");
                UpdateValue($"{carName}_PitStopStatus", "NOT LOADED");
            }

            // Reset the last recorded data for this car
            if (_lastRecordedData.ContainsKey(carName))
            {
                _lastRecordedData[carName] = new LastRecordedData();
            }

            // Reset the Gaps Dictionary
            GapsToLeader.AddOrUpdate(
                position,
                0f,
                (_, __) => 0f
            );
        }

        // Helper Function used to make Update Values easier.
        private void UpdateValue(string data, object message)
        {
            PluginManager.SetPropertyValue<F1ManagerPlotter>(data, message);
        }

        // Checks whether a specific car is in a new Turn or Lap.
        private bool LapOrTurnChanged(string carName, CarTelemetry car, int trackId)
        {
            try
            {
                if (!_lastRecordedData.ContainsKey(carName))
                {
                    _lastRecordedData[carName] = new LastRecordedData
                    {
                        NewLapStarted = false,
                        LastLapNumber = car.currentLap + 1,
                        LastTurnNumber = car.Driver.turnNumber,
                        LastTurnRecorded = false,
                        LastTire = car.tireCompound,
                        LastTireChangeLap = car.currentLap + 1,
                    };
                    return true;
                }

                int currentTurn = car.Driver.turnNumber;
                int currentLap = car.currentLap + 1;

                if (currentLap != _lastRecordedData[carName].LastLapNumber)
                {
                    _lastRecordedData[carName].NewLapStarted = true;
                    _lastRecordedData[carName].SpeedSTRecorded = false;
                    _lastRecordedData[carName].LastLapNumber = currentLap;
                    _lastRecordedData[carName].LastTurnNumber = 0;
                    return true;
                }

                if (currentTurn != _lastRecordedData[carName].LastTurnNumber && currentTurn >= 1 && currentTurn < TelemetryHelpers.GetTrackTurns(trackId))
                {
                    _lastRecordedData[carName].NewLapStarted = false;
                    _lastRecordedData[carName].LastTurnRecorded = false;
                    _lastRecordedData[carName].LastTurnNumber = currentTurn;
                    return true;
                }

                if (currentTurn == TelemetryHelpers.GetTrackTurns(trackId) && _lastRecordedData[carName].LastTurnRecorded == false)
                {
                    _lastRecordedData[carName].LastTurnRecorded = true;
                    _lastRecordedData[carName].LastTurnNumber = currentTurn;
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // Checks whether a specific car has had new tires fitted.
        private bool TireChanged(string carName, CarTelemetry car)
        {
            try
            {
                int currentTire = car.tireCompound;
                int LastTireChangedLap = car.currentLap + 1;

                bool shouldWrite = currentTire != _lastRecordedData[carName].LastTire;

                if (shouldWrite)
                {
                    _lastRecordedData[carName].LastTire = currentTire;
                    _lastRecordedData[carName].LastTireChangeLap = LastTireChangedLap;
                }

                return shouldWrite;
            }
            catch
            {
                return false;
            }
        }

        // Update the dictionaries of the Car's Historical Data.
        private void UpdateHistoricalData(string carName, Telemetry telemetry, int i, int CarsOnGrid)
        {
            // Check for session reset
            if (telemetry.carFloatValue != ExpectedCarValueSteam && telemetry.carFloatValue != ExpectedCarValueEpic)
            {
                ClearAllHistory();
            }

            int currentLap = telemetry.Car[i].currentLap + 1; // Don't forget to index
            int currentTurn = _lastRecordedData[carName].LastTurnNumber;

            if (currentLap < 1 || currentTurn < 0) return; // Skip invalid data

            lock (_historyLock)
            {
                // Initialize data structure if needed
                if (!_carHistory.ContainsKey(carName))
                {
                    _carHistory[carName] = new Dictionary<int, Dictionary<int, Telemetry>>();
                }

                if (!_carHistory[carName].ContainsKey(currentLap))
                {
                    _carHistory[carName][currentLap] = new Dictionary<int, Telemetry>();

                    // Clean up old laps if we've reached max
                    if (_carHistory[carName].Count > MaxLapsToStore)
                    {
                        int oldestLap = _carHistory[carName].Keys.Min();
                        _carHistory[carName].Remove(oldestLap);
                    }
                }

                // Store turn data
                _carHistory[carName][currentLap][currentTurn] = telemetry;

                // Update JSON properties
                UpdateLapProperty(telemetry, carName, currentLap, i, CarsOnGrid);
            }
        }

        // Update the Historical Data Properties in SimHub.
        private void UpdateLapProperty(Telemetry telemetry, string carName, int lapNumber, int i, int CarsOnGrid)
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

            // Get best Sector Times
            var (BestS1, BestS2, BestS3) = GetLowestSectorTimes();

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
                            TrackName = TelemetryHelpers.GetTrackName(t.Value.Session.trackId),
                            TimeElapsed = t.Value.Session.timeElapsed,
                            TelemetryHelpers.GetSessionRemaining(telemetry, carNames).LapsRemaining,
                            TelemetryHelpers.GetSessionRemaining(telemetry, carNames).TimeRemaining,
                            BestSessionTime = TelemetryHelpers.GetBestSessionTime(telemetry),
                            BestS1,
                            BestS2,
                            BestS3,
                            RubberState = t.Value.Session.rubber,
                            SessionType = TelemetryHelpers.GetSessionType(t.Value.Session.sessionType),
                            SessionTypeShort = TelemetryHelpers.GetShortSessionType(t.Value.Session.sessionType),
                            AirTemp = t.Value.Session.Weather.airTemp,
                            TrackTemp = t.Value.Session.Weather.trackTemp,
                            Weather = TelemetryHelpers.GetWeather(t.Value.Session.Weather.weather),
                            WaterOnTrack = t.Value.Session.Weather.waterOnTrack,

                            Position = t.Value.Car[i].Driver.position,
                            DriverNumber = t.Value.Car[i].Driver.driverNumber,
                            DriverFirstName = TelemetryHelpers.GetDriverFirstName(t.Value.Car[i].Driver.driverId),
                            DriverLastName = TelemetryHelpers.GetDriverLastName(t.Value.Car[i].Driver.driverId),
                            TeamName = TelemetryHelpers.GetTeamName(t.Value.Car[i].Driver.teamId, Settings),
                            PitStopStatus = TelemetryHelpers.GetPitStopStatus(t.Value.Car[i].pitStopStatus, t.Value.Session.sessionType),
                            TurnNumber = _lastRecordedData[carName].LastTurnNumber,
                            DistanceTravelled = t.Value.Car[i].Driver.distanceTravelled,
                            CurrentLap = t.Value.Car[i].currentLap + 1,
                            CurrentLapTime = t.Value.Car[i].Driver.currentLapTime,
                            DriverBestLap = t.Value.Car[i].Driver.driverBestLap,
                            LastLapTime = t.Value.Car[i].Driver.lastLapTime,
                            LastS1Time = _lastRecordedData[carName].S1Time,
                            _lastRecordedData[carName].BestS1Time,
                            LastS2Time = _lastRecordedData[carName].S2Time,
                            _lastRecordedData[carName].BestS2Time,
                            LastS3Time = _lastRecordedData[carName].S3Time,
                            _lastRecordedData[carName].BestS3Time,
                            _lastRecordedData[carName].SpeedST,
                            Speed = t.Value.Car[i].Driver.speed,
                            RPM = t.Value.Car[i].Driver.rpm,
                            Gear = t.Value.Car[i].Driver.gear,
                            Charge = t.Value.Car[i].charge,
                            EnergyHarvested = t.Value.Car[i].energyHarvested,
                            EnergySpent = t.Value.Car[i].energySpent,
                            Fuel = t.Value.Car[i].fuel,
                            FuelDelta = t.Value.Car[i].fuelDelta,
                            TireCompound = TelemetryHelpers.GetTireCompound(t.Value.Car[i].tireCompound, Settings),
                            TireAge = (t.Value.Car[i].currentLap + 1) - _lastRecordedData[carName].LastTireChangeLap,
                            FLDeg = t.Value.Car[i].flWear,
                            FLSurfaceTemp = t.Value.Car[i].flSurfaceTemp,
                            FLTemp = t.Value.Car[i].flTemp,
                            FLBrakeTemp = t.Value.Car[i].flBrakeTemp,
                            FRDeg = t.Value.Car[i].frWear,
                            FRSurfaceTemp = t.Value.Car[i].frSurfaceTemp,
                            FRTemp = t.Value.Car[i].frTemp,
                            FRBrakeTemp = t.Value.Car[i].frBrakeTemp,
                            RLDeg = t.Value.Car[i].rlWear,
                            RLSurfaceTemp = t.Value.Car[i].rlSurfaceTemp,
                            RLTemp = t.Value.Car[i].rlTemp,
                            RLBrakeTemp = t.Value.Car[i].rlBrakeTemp,
                            RRDeg = t.Value.Car[i].rrWear,
                            RRSurfaceTemp = t.Value.Car[i].rrSurfaceTemp,
                            RRTemp = t.Value.Car[i].rrTemp,
                            RRBrakeTemp = t.Value.Car[i].rrBrakeTemp,
                            PaceMode = TelemetryHelpers.GetPaceMode(t.Value.Car[i].paceMode),
                            FuelMode = TelemetryHelpers.GetFuelMode(t.Value.Car[i].fuelMode),
                            ERSMode = TelemetryHelpers.GetERSMode(t.Value.Car[i].ersMode),
                            DRSMode = TelemetryHelpers.GetDRSMode(t.Value.Car[i].Driver.drsMode),
                            ERSAssist = Convert.ToBoolean(t.Value.Car[i].Driver.ERSAssist),
                            DriveCleanAir = Convert.ToBoolean(t.Value.Car[i].Driver.DriveCleanAir),
                            AvoidHighKerbs = Convert.ToBoolean(t.Value.Car[i].Driver.AvoidHighKerbs),
                            DontFightTeammate = Convert.ToBoolean(t.Value.Car[i].Driver.DontFightTeammate),
                            OvertakeAggression = TelemetryHelpers.GetOvertakeMode(t.Value.Car[i].Driver.OvertakeAggression),
                            DefendApproach = TelemetryHelpers.GetDefendMode(t.Value.Car[i].Driver.DefendApproach),
                            EngineTemp = t.Value.Car[i].engineTemp,
                            EngineWear = t.Value.Car[i].engineWear,
                            GearboxWear = t.Value.Car[i].gearboxWear,
                            ERSWear = t.Value.Car[i].ersWear,
                            NameOfCarBehind = TelemetryHelpers.GetNameOfCarBehind(t.Value.Car[i].Driver.position, i, carNames, CarsOnGrid),
                            NameOfCarAhead = TelemetryHelpers.GetNameOfCarAhead(t.Value.Car[i].Driver.position, i, carNames),
                            GapBehind = TelemetryHelpers.GetGapBehind(telemetry, t.Value.Car[i].Driver.position, i, carNames, CarsOnGrid),
                            GapAhead = TelemetryHelpers.GetGapInFront(telemetry, t.Value.Car[i].Driver.position, i, carNames),
                            GapToLeader = TelemetryHelpers.GetGapLeader(telemetry, t.Value.Car[i].Driver.position, i, carNames)
                        }
                    )
            };

            PluginManager.SetPropertyValue<F1ManagerPlotter>(
                propertyName,
                JsonConvert.SerializeObject(lapData, Formatting.None)
            );
        }

        // Clear all Historical Data Properties in SimHub.
        public void ClearAllHistory()
        {
            lock (_historyLock)
            {
                foreach (var car in _carHistory.Keys)
                {
                    // Reset all properties
                    for (int i = 1; i <= MaxLapsToStore; i++)
                    {
                        PluginManager.SetPropertyValue(
                            $"{car}.History.Lap{i}",
                            this.GetType(),
                            null
                        );
                    }
                }

                _lastRecordedData.Clear();
                _carHistory.Clear();
                CarPositions.Clear();
                CarBestLapTimes.Clear();
                GapsToLeader.Clear();
            }
            SimHub.Logging.Current.Info("Cleared all historical data due to session reset");
        }

        // Reload all Settings.
        public void ReloadSettings(F1Manager2024PluginSettings settings)
        {
            if (settings.TrackedDriversDashboard.Length != 0)
            {
                if (settings.TrackedDriversDashboard.Length == 1)
                {
                    UpdateValue("TrackedDriver1", settings.TrackedDriversDashboard[0] ?? "");
                    UpdateValue("TrackedDriver2", null);
                }
                if (settings.TrackedDriversDashboard.Length == 2)
                {
                    UpdateValue("TrackedDriver1", settings.TrackedDriversDashboard[0] ?? "");
                    UpdateValue("TrackedDriver2", settings.TrackedDriversDashboard[1] ?? "");
                }
            }

            Settings = settings;
        }

        // Called when SimHub is closed.
        public void End(PluginManager pluginManager)
        {
            _mmfReader.DataReceived -= DataReceived;
            _mmfReader.StopReading();

            // Save settings
            this.SaveCommonSettings("GeneralSettings", Settings);
        }

        #endregion
    }
}