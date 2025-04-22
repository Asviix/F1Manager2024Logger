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
using System.Windows.Forms;

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

        private readonly float ExpectedCarValue = 8021.86f;

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
            pluginManager.AddProperty("DEBUG_Status_IsMemoryMap_Connected", this.GetType(), false);
            pluginManager.AddProperty("DEBUG_Status_MemoryMap_Status", this.GetType(), "Waiting for Mapped Memory");
            pluginManager.AddProperty("DEBUG_Game_Status", this.GetType(), typeof(string));

            // Load Settings
            Settings = this.ReadCommonSettings<F1Manager2024PluginSettings>("GeneralSettings", () => new F1Manager2024PluginSettings());
            
            // Create new Exporter
            _exporter = new Exporter();

            // Create new Reader
            _mmfReader = new MmfReader();
            _mmfReader.StartReading("F1ManagerTelemetry");
            _mmfReader.DataReceived += DataReceived;

            #region Init Properties
            // Add Game Properties
            pluginManager.AddProperty("CameraFocusedOn", GetType(), typeof(string), "The Car name the camera is focus on.");

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
                pluginManager.AddProperty($"{name}_DriverFirstName", GetType(), typeof(string), "Driver First Name");
                pluginManager.AddProperty($"{name}_DriverLastName", GetType(), typeof(string), "Driver Last Name");
                pluginManager.AddProperty($"{name}_DriverTeamName", GetType(), typeof(string), "Name of the Driver's Team.");
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

        public void DataReceived(Telemetry telemetry)
        {
            lock (_dataLock)
            {
                if (telemetry.carFloatValue != ExpectedCarValue) { UpdateStatus(false, "Connected.", "Game not in Session."); return; }
                try
                {
                    _lastData = telemetry;

                    UpdateProperties(_lastData, _lastDataTime, _lastTimeElapsed);
                    UpdateStatus(true, "Connected", "Game in Session");
                }
                catch (Exception)
                {
                    UpdateStatus(false, "Error processing data", "Game in Session");
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

        private readonly ConcurrentDictionary<string, Dictionary<int, Dictionary<int, Telemetry>>> _carHistory = new ConcurrentDictionary<string, Dictionary<int, Dictionary<int, Telemetry>>>();

        public Dictionary<string, (string FirstName, string LastName)> GetDriversNames()
        {
            var result = new Dictionary<string, (string, string)>();

            if (_lastData.Car == null) return result;

            for (int i = 0; i < Math.Min(_lastData.Car.Length, carNames.Length); i++)
            {
                var driverId = _lastData.Car[i].Driver.driverId;
                var name = carNames[i];
                var firstName = GetDriverFirstName(driverId);
                var lastName = GetDriverLastName(driverId);
                result[name] = (firstName, lastName);
            }

            return result;
        }

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

        private void UpdateStatus(bool connected, string message, string message2)
        {
            UpdateValue("DEBUG_Status_IsMemoryMap_Connected", connected);
            UpdateValue("DEBUG_Status_MemoryMap_Status", message);
            UpdateValue("DEBUG_Game_Status", message2);
        }

        private void UpdateProperties(Telemetry telemetry, DateTime lastDataTime, float lastTimeElapsed)
        {
            if (telemetry.Car == null || telemetry.Car.Length < 22) return;

            // Compute Time Fast-Forward Property
            var session = telemetry.Session;
            if (DateTime.UtcNow - lastDataTime > TimeSpan.FromSeconds(1))
            {
                UpdateValue("TimeSpeed", (session.timeElapsed - lastTimeElapsed));
                _lastDataTime = DateTime.UtcNow;
                _lastTimeElapsed = session.timeElapsed;
            }

            // Update Game Properties
            UpdateValue("CameraFocusedOn", carNames[telemetry.cameraFocus]);

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
                    UpdateHistoricalData(name, telemetry, i);

                    _exporter.ExportData(name, telemetry, i, Settings);
                }

                UpdateValue($"{name}_Position", (car.Driver.position) + 1); // Adjust for 0-based index
                UpdateValue($"{name}_DriverNumber", car.Driver.driverNumber);
                UpdateValue($"{name}_PitStopStatus", GetPitStopStatus(car.pitStopStatus));
                // Status
                UpdateValue($"{name}_TurnNumber", car.Driver.turnNumber);
                UpdateValue($"{name}_DriverFirstName", GetDriverFirstName(car.Driver.driverId));
                UpdateValue($"{name}_DriverLastName", GetDriverLastName(car.Driver.driverId));
                UpdateValue($"{name}_DriverTeamName", GetTeamName(car.Driver.teamId, Settings));
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

        public static string GetDriverFirstName(int driverId)
        {
            return driverId switch
            {
                1 => "Lewis",
                2 => "Charles",
                3 => "Alexander",
                8 => "Valtteri",
                9 => "Sebastian",
                10 => "Max",
                11 => "Carlos",
                12 => "Lando",
                13 => "Daniel",
                14 => "Esteban",
                15 => "Pierre",
                16 => "Daniil",
                17 => "Sergio",
                18 => "Lance",
                19 => "Kimi",
                20 => "Antonio",
                22 => "Stoffel",
                23 => "George",
                24 => "Nicholas",
                74 => "Mick",
                76 => "Nyck",
                77 => "Fernando",
                78 => "Roy",
                79 => "Nikita",
                80 => "Pietro",
                81 => "Yuki",
                82 => "Robert",
                83 => "Nico",
                85 => "Denis",
                87 => "Theo",
                88 => "Ralph",
                91 => "Jehan",
                94 => "Marcus",
                95 => "Liam",
                96 => "Juri",
                99 => "Richard",
                102 => "Oscar",
                104 => "Marino",
                105 => "Guanyu",
                106 => "Felipe",
                107 => "Frederik",
                108 => "Alexander",
                109 => "Juan Manuel",
                110 => "Amaury",
                112 => "Laszlo",
                114 => "Ido",
                115 => "Kaylen",
                116 => "Logan",
                117 => "Enzo",
                119 => "Roman",
                120 => "Ayumu",
                121 => "Jak",
                123 => "Rafael",
                125 => "Calan",
                127 => "Victor",
                128 => "Caio",
                130 => "Dennis",
                131 => "Olli",
                132 => "Arthur",
                133 => "Clement",
                135 => "Jack",
                140 => "Jake",
                141 => "Cem",
                142 => "Oliver",
                143 => "Gregoire",
                144 => "Isack",
                242 => "Zane",
                243 => "Francesco",
                244 => "Hunter",
                245 => "Pepe",
                246 => "William",
                247 => "Zak",
                248 => "Franco",
                249 => "Reece",
                250 => "David",
                251 => "Ayrton",
                252 => "Kush",
                253 => "Brad",
                254 => "Romain",
                255 => "Kevin",
                256 => "Pascal",
                257 => "Felipe",
                258 => "Michael",
                259 => "Pedro",
                260 => "Rubens",
                263 => "Jack",
                264 => "Sebastien",
                265 => "Enzo",
                267 => "Nazim",
                268 => "Oliver",
                269 => "Federico",
                270 => "David",
                272 => "Jonny",
                273 => "Filip",
                274 => "Zdenek",
                275 => "Lirim",
                276 => "David",
                277 => "Roberto",
                278 => "Niko",
                279 => "Gabriel",
                280 => "Gabriele",
                281 => "Paul",
                282 => "Dino",
                283 => "Mari",
                284 => "Christian",
                285 => "Pato",
                286 => "Nikola",
                287 => "Tommy",
                288 => "Oliver",
                289 => "Leonardo",
                300 => "Oliver",
                301 => "Sebastian",
                302 => "Hugh",
                303 => "Alejandro",
                304 => "Nikita",
                305 => "Taylor",
                306 => "Sophia",
                307 => "Roberto",
                308 => "Piotr",
                322 => "Luke",
                351 => "Miguel",
                359 => "Luc",
                371 => "Mckenzy",
                373 => "Arvid",
                374 => "Sami",
                375 => "Martinius",
                376 => "Andrea Kimi",
                377 => "Ritomo",
                378 => "Joshua",
                379 => "Tim",
                380 => "Noel",
                381 => "Laurens",
                382 => "Charlie",
                383 => "Santiago",
                384 => "Callum",
                385 => "Cian",
                386 => "Joshua",
                387 => "Kacper",
                388 => "Matias",
                389 => "Joseph",
                390 => "Maxwell",
                394 => "Tasanapol",
                398 => "Ryo",
                399 => "Alexander",
                400 => "Lena",
                401 => "Carrie",
                402 => "Chloe",
                405 => "Abbi",
                406 => "Nicola",
                407 => "Kean",
                408 => "Jessica",
                409 => "Tina",
                410 => "Bianca",
                411 => "Ugo",
                413 => "Robert",
                416 => "James",
                417 => "Kabir",
                418 => "Maya",
                419 => "Aurelia",
                436 => "Amna",
                437 => "Hamda",
                438 => "Emely",
                439 => "Tuukka",
                547 => "Hiroko",
                548 => "Minna",
                549 => "Jennifer",
                550 => "Anne-Marie",
                551 => "Stephanie",
                552 => "Claudio",
                553 => "Ludwig",
                567 => "Waseem",
                _ => "Unknown"
            };
        }

        public static string GetDriverLastName(int driverId)
        {
            return driverId switch
            {
                1 => "Hamilton",
                2 => "Leclerc",
                3 => "Albon",
                8 => "Bottas",
                9 => "Vettel",
                10 => "Verstappen",
                11 => "Sainz",
                12 => "Norris",
                13 => "Ricciardo",
                14 => "Ocon",
                15 => "Gasly",
                16 => "Kvyat",
                17 => "Perez",
                18 => "Stroll",
                19 => "Raikkonen",
                20 => "Giovinazzi",
                22 => "Vandoorne",
                23 => "Russell",
                24 => "Latifi",
                74 => "Schumacher",
                76 => "de Vries",
                77 => "Alonso",
                78 => "Nissany",
                79 => "Mazepin",
                80 => "Fittipaldi",
                81 => "Tsunoda",
                82 => "Kubica",
                83 => "Hulkenberg",
                85 => "Moreau",
                87 => "Pourchaire",
                88 => "Boschung",
                91 => "Daruvala",
                94 => "Armstrong",
                95 => "Lawson",
                96 => "Vips",
                99 => "Verschoor",
                102 => "Piastri",
                104 => "Sato",
                105 => "Zhou",
                106 => "Drugovich",
                107 => "Vesti",
                108 => "Smolyar",
                109 => "Correa",
                110 => "Cordeel",
                112 => "Toth",
                114 => "Cohen",
                115 => "Frederick",
                116 => "Sargeant",
                117 => "Fittipaldi",
                119 => "Stanek",
                120 => "Iwasa",
                121 => "Crawford",
                123 => "Villagomez",
                125 => "Williams",
                127 => "Martins",
                128 => "Collet",
                130 => "Hauger",
                131 => "Caldwell",
                132 => "Leclerc",
                133 => "Novalak",
                135 => "Doohan",
                140 => "Hughes",
                141 => "Bolukbasi",
                142 => "Bearman",
                143 => "Saucy",
                144 => "Hadjar",
                242 => "Maloney",
                243 => "Pizzi",
                244 => "Yeany",
                245 => "Marti",
                246 => "Alatalo",
                247 => "O'Sullivan",
                248 => "Colapinto",
                249 => "Ushijima",
                250 => "Vidales",
                251 => "Simmons",
                252 => "Maini",
                253 => "Benavides",
                254 => "Grosjean",
                255 => "Magnussen",
                256 => "Wehrlein",
                257 => "Massa",
                258 => "Schumacher",
                259 => "De La Rosa",
                260 => "Barrichello",
                263 => "Aitken",
                264 => "Buemi",
                265 => "Trulli",
                267 => "Azman",
                268 => "Rasmussen",
                269 => "Malvestiti",
                270 => "Schumacher",
                272 => "Edgar",
                273 => "Ugran",
                274 => "Chovanec",
                275 => "Zendeli",
                276 => "Beckmann",
                277 => "Merhi",
                278 => "Kari",
                279 => "Bortoleto",
                280 => "Mini",
                281 => "Aron",
                282 => "Beganovic",
                283 => "Boya",
                284 => "Mansell",
                285 => "O'Ward",
                286 => "Tsolov",
                287 => "Smith",
                288 => "Goethe",
                289 => "Fornaroli",
                300 => "Gray",
                301 => "Montoya",
                302 => "Barter",
                303 => "Garcia",
                304 => "Bedrin",
                305 => "Barnard",
                306 => "Florsch",
                307 => "Faria",
                308 => "Wisnicki",
                322 => "Browning",
                351 => "Baltazar",
                359 => "Dupont",
                371 => "Cresswell",
                373 => "Lindblad",
                374 => "Meguetounif",
                375 => "Stenshorne",
                376 => "Antonelli",
                377 => "Miyata",
                378 => "Durksen",
                379 => "Tramnitz",
                380 => "Leon",
                381 => "van Hoepen",
                382 => "Wurz",
                383 => "Ramos",
                384 => "Voisin",
                385 => "Shields",
                386 => "Dufek",
                387 => "Sztuka",
                388 => "Zagazeta",
                389 => "Loake",
                390 => "Esterson",
                394 => "Inthraphuvasak",
                398 => "Hirakawa",
                399 => "Dunne",
                400 => "Buhler",
                401 => "Schreiner",
                402 => "Chambers",
                405 => "Pulling",
                406 => "Lacorte",
                407 => "Nakamura-Berta",
                408 => "Hawkins",
                409 => "Hausmann",
                410 => "Bustamante",
                411 => "Ugochukwu",
                413 => "Shwartzman",
                416 => "Hedley",
                417 => "Anurag",
                418 => "Weug",
                419 => "Nobels",
                436 => "Al Qubaisi",
                437 => "Al Qubaisi",
                438 => "de Heus",
                439 => "Taponen",
                547 => "Ueda",
                548 => "Bruun",
                549 => "Randall",
                550 => "Bertin",
                551 => "Augar",
                552 => "Alvarez",
                553 => "Sommer",
                567 => "Nazari",
                _ => "Unknown"
            };
        }

        public static string GetTeamName(int teamId, F1Manager2024PluginSettings Settings)
        {
            if (Settings.CustomTeamName != null && teamId == 32) return Settings.CustomTeamName;

            return teamId switch
            {
                1 => "Ferrari",
                2 => "McLaren",
                3 => "Red Bull Racing",
                4 => "Mercedes AMG Petronas F1",
                5 => "Alpine",
                6 => "Williams Racing",
                7 => "Haas F1",
                8 => "Racing Bulls",
                9 => "Kick Sauber",
                10 => "Aston Martin",
                32 => "Custom Team",
                _ => "Unknown",
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

        private void UpdateHistoricalData(string carName, Telemetry telemetry, int i)
        {
            // Check for session reset
            float currentTime = (float)(telemetry.Session.timeElapsed);
            if (telemetry.carFloatValue != ExpectedCarValue)
            {
                ClearAllHistory();
            }

            int currentLap = telemetry.Car[i].currentLap + 1; // Don't forget to index
            int currentTurn = telemetry.Car[i].Driver.turnNumber;

            if (currentLap < 1 || currentTurn < 1) return; // Skip invalid data

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
                UpdateLapProperty(carName, currentLap, i);
            }
        }

        private void UpdateLapProperty(string carName, int lapNumber, int i)
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
                            TrackName = GetTrackName(t.Value.Session.trackId),
                            TimeElapsed = t.Value.Session.timeElapsed,
                            BestSessionTime = t.Value.Session.bestSessionTime,
                            RubberState = t.Value.Session.rubber,
                            SessionType = GetSessionType(t.Value.Session.sessionType),
                            SessionTypeShort = GetShortSessionType(t.Value.Session.sessionType),
                            AirTemp = t.Value.Session.Weather.airTemp,
                            TrackTemp = t.Value.Session.Weather.trackTemp,
                            Weather = GetWeather(t.Value.Session.Weather.weather),

                            Position = t.Value.Car[i].Driver.position,
                            DriverNumber = t.Value.Car[i].Driver.driverNumber,
                            DriverFirstName = GetDriverFirstName(t.Value.Car[i].Driver.driverId),
                            DriverLastName = GetDriverLastName(t.Value.Car[i].Driver.driverId),
                            TeamName = GetTeamName(t.Value.Car[i].Driver.teamId, Settings),
                            PitStopStatus = GetPitStopStatus(t.Value.Car[i].pitStopStatus),
                            TurnNumber = t.Value.Car[i].Driver.turnNumber,
                            CurrentLap = t.Value.Car[i].currentLap,
                            CurrentLapTime = t.Value.Car[i].Driver.currentLapTime,
                            DriverBestLap = t.Value.Car[i].Driver.driverBestLap,
                            LastLapTime = t.Value.Car[i].Driver.lastLapTime,
                            LastS1Time = t.Value.Car[i].Driver.lastS1Time,
                            LastS2Time = t.Value.Car[i].Driver.lastS2Time,
                            LastS3Time = t.Value.Car[i].Driver.lastS3Time,
                            Speed = t.Value.Car[i].Driver.speed,
                            RPM = t.Value.Car[i].Driver.rpm,
                            Gear = t.Value.Car[i].Driver.gear,
                            Charge = t.Value.Car[i].charge,
                            Fuel = t.Value.Car[i].fuel,
                            TireCompound = GetTireCompound(t.Value.Car[i].tireCompound),
                            FLTemp = t.Value.Car[i].flTemp,
                            FRTemp = t.Value.Car[i].frTemp,
                            RLTemp = t.Value.Car[i].rlTemp,
                            RRTemp = t.Value.Car[i].rrTemp,
                            FLWear = t.Value.Car[i].flWear,
                            FRWear = t.Value.Car[i].frWear,
                            RLWear = t.Value.Car[i].rlWear,
                            RRWear = t.Value.Car[i].rrWear,
                            PaceMode = GetPaceMode(t.Value.Car[i].paceMode),
                            FuelMode = GetFuelMode(t.Value.Car[i].fuelMode),
                            ERSMode = GetERSMode(t.Value.Car[i].ersMode),
                            DRSMode = GetDRSMode(t.Value.Car[i].Driver.drsMode),
                            EngineTemp = t.Value.Car[i].engineTemp,
                            EngineWear = t.Value.Car[i].engineWear,
                            GearboxWear = t.Value.Car[i].gearboxWear,
                            ERSWear = t.Value.Car[i].ersWear
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