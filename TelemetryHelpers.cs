using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimHub.Plugins;
using SimHub.Plugins.DataPlugins.ShakeItV3.UI.OutputSettings.Vibration;
using static F1Manager2024Plugin.F1ManagerPlotter;

namespace F1Manager2024Plugin
{

    public static class TelemetryHelpers
    {

        // Returns the track name based on ID.
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

        // Returns the track length based on ID.
        public static float GetTrackLength(int trackId)
        {
            return trackId switch
            {
                0 => 0f,
                1 => 5278f,
                2 => 5412f,
                3 => 5451f,
                4 => 6003f,
                5 => 4657f,
                6 => 3337f,
                7 => 4361f,
                8 => 5842f,
                9 => 4318f,
                10 => 5891f,
                11 => 6174f,
                12 => 4381f,
                13 => 7004f,
                14 => 5793f,
                15 => 4940f,
                16 => 5848f,
                17 => 5807f,
                18 => 4304f,
                19 => 5513f,
                20 => 4309f,
                21 => 5281f,
                22 => 5412f,
                23 => 4259f,
                24 => 5909f,
                25 => 6201f,
                26 => 5419f,
                _ => 0f
            };
        }

        // Returns the number of turns in a track based on ID.
        public static int GetTrackTurns(int trackId)
        {
            return trackId switch
            {
                0 => 0,
                1 => 14,
                2 => 15,
                3 => 16,
                4 => 20,
                5 => 14,
                6 => 19,
                7 => 14,
                8 => 15,
                9 => 10,
                10 => 18,
                11 => 27,
                12 => 14,
                13 => 19,
                14 => 11,
                15 => 19,
                16 => 18,
                17 => 18,
                18 => 17,
                19 => 20,
                20 => 15,
                21 => 16,
                22 => 19,
                23 => 14,
                24 => 19,
                25 => 17,
                26 => 16,
                _ => 0
            };
        }

        // Returns the distance in meters from the start-finish lines to the Speed Trap.
        public static float GetSpeedTrapDistance(int trackId)
        {
            return trackId switch
            {
                0 => 0f,
                1 => 231.402594f,
                2 => 554.268687f,
                3 => 4472.886543f,
                4 => 5142.278267f,
                5 => 599.251425f,
                6 => 1859.776249f,
                7 => 3550.89665f,
                8 => 2580.680579f,
                9 => 2014.860737f,
                10 => 4858.272171f,
                11 => 5301.036909f,
                12 => 315.89929f,
                13 => 1222.67089f,
                14 => 664.830088f,
                15 => 241.944879f,
                16 => 1070.783823f,
                17 => 5047.793003f,
                18 => 957.583701f,
                19 => 3552.430392f,
                20 => 262.152169f,
                21 => 2480.84897f,
                22 => 4579.952002f,
                23 => 570.120918f,
                24 => 4197.11918f,
                25 => 5018.287971f,
                26 => 599.526241f,
                _ => 0f
            };
        }

        // Returns to average Pit Lane Time Loss based on ID.
        public static float GetAveragePitLaneLoss(int trackId)
        {
            return trackId switch
            {
                0 => 0f,
                1 => 19f,
                2 => 23f,
                3 => 25f,
                4 => 19f,
                5 => 23f,
                6 => 20f,
                7 => 22f,
                8 => 0f,
                9 => 22f,
                10 => 22f,
                11 => 20f,
                12 => 21f,
                13 => 17f,
                14 => 25f,
                15 => 28f,
                16 => 0f,
                17 => 23f,
                18 => 25f,
                19 => 21f,
                20 => 23f,
                21 => 21f,
                22 => 21f,
                23 => 21f,
                24 => 29f,
                25 => 17f,
                26 => 24f,
                _ => 0f
            };
        }

        // Returns the estimated position after pitting.
        public static int GetEstimatedPositionAfterPit(Telemetry telemetry, int position, int i, string[] carNames, int CarsOnGrid)
        {
            if (telemetry.Session.sessionType is not 6 or 7) return 0;

            if (!F1ManagerPlotter.GapsToLeader.TryGetValue(position, out float currentGapToLeader))
            {
                return position;
            }

            float pitLaneLoss = GetAveragePitLaneLoss(telemetry.Session.trackId);
            float estimatedGapAfterPit = currentGapToLeader + pitLaneLoss;

            int carsToPass = F1ManagerPlotter.GapsToLeader.Count(kv =>
            kv.Key != position &&
            kv.Value > currentGapToLeader &&
            kv.Value <= estimatedGapAfterPit);

            return Math.Min(position + 1 + carsToPass, CarsOnGrid);
        }

        // Returns the number of laps based on ID.
        public static int GetTrackLaps(int trackId)
        {
            return trackId switch
            {
                0 => 0,    // Invalid
                1 => 58,    // Albert Park (Australia) - 2024
                2 => 57,   // Bahrain - 2024
                3 => 56,   // Shanghai - 2023 data (returning in 2024 after hiatus)
                4 => 51,   // Baku - 2023 data
                5 => 66,   // Barcelona - 2023 data
                6 => 78,   // Monaco - 2023 data
                7 => 70,   // Montreal - 2023 data
                8 => 53,   // Paul Ricard - 2022 data (not in 2023/2024)
                9 => 71,   // Red Bull Ring (Austria) - 2023 data
                10 => 52,  // Silverstone - 2023 data
                11 => 50,  // Jeddah - 2024
                12 => 70,  // Hungaroring - 2023 data
                13 => 44,  // Spa-Francorchamps - 2023 data
                14 => 53,  // Monza - 2023 data
                15 => 62,  // Marina Bay - 2023 data
                16 => 53,  // Sochi - 2021 data (not in recent seasons)
                17 => 53,  // Suzuka - 2023 data
                18 => 71,  // Hermanos Rodriguez (Mexico) - 2023 data
                19 => 56,  // COTA - 2023 data
                20 => 71,  // Interlagos - 2023 data
                21 => 58, // Yas Marina - 2023 data
                22 => 57,  // Miami - 2023 data
                23 => 72,  // Zandvoort - 2023 data
                24 => 63,  // Imola - 2023 data
                25 => 50,  // Las Vegas - 2023
                26 => 57,  // Qatar - 2023
                _ => 0     // Unknown
            };
        }

        // Returns the number of sprint laps based on ID and taking into a account all sprint races are 100KM + 1 Lap.
        public static int GetTrackLapsSprint(int trackId)
        {
            return trackId switch
            {
                0 => 0,   // Invalid
                1 => 19,  // Albert Park (100.282km)
                2 => 19,  // Bahrain (102.828km)
                3 => 19,  // Shanghai (103.569km)
                4 => 17,  // Baku (102.051km) - rounded up from 16.66 laps
                5 => 22,  // Barcelona (102.454km)
                6 => 30,  // Monaco (100.11km)
                7 => 23,  // Montreal (100.303km)
                8 => 18,  // Paul Ricard (105.156km)
                9 => 24,  // Red Bull Ring (103.632km)
                10 => 17, // Silverstone (100.147km)
                11 => 17, // Jeddah (104.958km)
                12 => 23, // Hungaroring (100.763km)
                13 => 15, // Spa (105.06km)
                14 => 18, // Monza (104.274km)
                15 => 21, // Marina Bay (103.74km)
                16 => 18, // Sochi (105.264km)
                17 => 18, // Suzuka (104.526km)
                18 => 24, // Mexico (103.296km)
                19 => 19, // COTA (104.747km)
                20 => 24, // Interlagos (103.416km)
                21 => 19, // Yas Marina (100.339km)
                22 => 19, // Miami (102.828km)
                23 => 24, // Zandvoort (102.216km)
                24 => 17, // Imola (100.453km)
                25 => 17, // Las Vegas (105.417km)
                26 => 19, // Qatar (102.961km)
                _ => 0    // Unknown
            };
        }

        // Returns the session type based on ID.
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

        // Returns the short session type based on ID.
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

        // Returns the session length based on ID.
        public static int GetSessionLength(int sessionId)
        {
            return sessionId switch
            {
                0 => 60,   // Practice 1 (1 hour)
                1 => 60,   // Practice 2 (1 hour)
                2 => 60,   // Practice 3 (1 hour)
                3 => 18,   // Qualifying 1 (~18 minutes)
                4 => 15,   // Qualifying 2 (~15 minutes)
                5 => 12,   // Qualifying 3 (~12 minutes)
                6 => 0,    // Race (determined by laps)
                7 => 0,    // Sprint (determined by laps)
                8 => 12,   // Sprint Qualifying 1 (~12 minutes)
                9 => 10,   // Sprint Qualifying 2 (~10 minutes)
                10 => 8,   // Sprint Qualifying 3 (~8 minutes)
                _ => 0     // Unknown
            };
        }

        // Returns the weather based on ID.
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
                16 => "Heavy Rain",
                _ => "Unknown"
            };
        }

        // Returns 3 values, TimeRemaining, LapsRemaining and Mixed depending on what track and session type that session is.
        public static (float TimeRemaining, float LapsRemaining, float Mixed) GetSessionRemaining(Telemetry telemetry, string[] carNames)
        {
            int sessionType = telemetry.Session.sessionType;
            int trackId = telemetry.Session.trackId;
            int P1Car = 0;

            // For Race or Sprint sessions (return laps remaining)
            if (sessionType == 6 || sessionType == 7)
            {
                // Looks for the car in first position (Laps update based on the leader's position)
                for (int j = 0; j < carNames.Length; j++)
                {
                    if (telemetry.Car[j].Driver.position == 0)
                    {
                        P1Car = telemetry.Car[j].driverPos;
                        break;
                    }
                }

                int totalLaps = (sessionType == 6) ? GetTrackLaps(trackId) : GetTrackLapsSprint(trackId);
                float currentLap = telemetry.Car[P1Car].currentLap + 1; // Convert 0-index to lap count
                float lapsRemaining = totalLaps - currentLap;
                return (0, lapsRemaining, lapsRemaining);
            }
            // For all other sessions (return time remaining in seconds)
            else
            {
                int sessionDuration = GetSessionLength(sessionType) * 60;
                float timeRemaining =   sessionDuration - telemetry.Session.timeElapsed;
                return (timeRemaining, 0, timeRemaining);
            }
        }

        // Returns the points gains based on position and session type.
        public static int GetPointsGained(int position, int sessionId, bool isFastest)
        {
            int basePoints = 0;
            if (sessionId is 6)
            {
                basePoints = position switch
                {
                    1 => 25,
                    2 => 18,
                    3 => 15,
                    4 => 12,
                    5 => 10,
                    6 => 8,
                    7 => 6,
                    8 => 4,
                    9 => 2,
                    10 => 1,
                    _ => 0
                };
            }
            else if (sessionId is 7)
            {
                basePoints = position switch
                {
                    1 => 8,
                    2 => 7,
                    3 => 6,
                    4 => 5,
                    5 => 4,
                    6 => 3,
                    7 => 2,
                    8 => 1,
                    9 => 0,
                    10 => 0,
                    _ => 0
                };
            }

            if (sessionId is 8 or 7 && isFastest is true)
            {
                basePoints += 1;
            }
            return basePoints;
        }

        // Returns the Driver's First Name based on ID.
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

        // Returns the Driver's Last Name based on ID.
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

        // Returns the Team's Name based on ID.
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

        // Returns the Team's Color based on ID.
        public static string GetTeamColor(int teamId, F1Manager2024PluginSettings Settings)
        {
            if (Settings.CustomTeamColor != null && teamId == 32) return Settings.CustomTeamColor;

            return teamId switch
            {
                1 => "#e80030",
                2 => "#ff8300",
                3 => "#3974c7",
                4 => "#2af4d3",
                5 => "#0095cd",
                6 => "#67c5ff",
                7 => "#b8bcbf",
                8 => "#6994ff",
                9 => "#55e355",
                10 => "#249b74",
                32 => "#FFFFFF",
                _ => "#FFFFFF",
            };
        }

        // Returns the PitStop State based on ID.
        public static string GetPitStopStatus(int pitStop, int sessionType)
        {
            string None = "None";
            if (sessionType is 6 or 7)
            {
                None = "On Track";
            }

            return pitStop switch
            {
                0 => None,
                1 => "Requested",
                2 => "Entering",
                3 => "Queuing",
                4 => "Stopped",
                5 => "Exiting",
                6 => "In Garage",
                7 => "Jack Up",
                8 => "Releasing",
                9 => "Car Setup",
                10 => "Approach",
                11 => "Penalty",
                12 => "Releasing",
                _ => "Unknown"
            };
        }

        // Returns the Tire Compound based on ID.
        public static string GetTireCompound(int compound)
        {
            return compound switch
            {
                0 or 1 or 2 or 3 or 4 or 5 or 6 or 7 => "Soft",
                8 or 9 or 10 => "Medium",
                11 or 12 => "Hard",
                13 or 14 or 15 or 16 or 17 => "Intermediates",
                18 or 19 => "Wet",
                _ => "Unknown"
            };
        }

        // Returns the Pace Mode based on ID.
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

        // Returns the Fuel Mode based on ID.
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

        // Returns the ERS Mode based on ID.
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

        // Returns the DRS Mode based on ID.
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

        // Returns the name of the car Currently behind the driver in [i] position
        public static string GetNameOfCarBehind(int position, int i, string[] carNames, int CarsOnGrid)
        {
            // Return the current car if it's last in the grid
            if ((CarsOnGrid == 22 && position == 21) || (CarsOnGrid == 20 && position == 19))
            {
                return carNames[i];
            }

            // Lookup car behind
            if (F1ManagerPlotter.CarPositions.TryGetValue(position + 1, out string carBehind))
                return carBehind;

            return "Unknown";
        }

        // Returns the name of the car Currently ahead the driver in [i] position
        public static string GetNameOfCarAhead(int position, int i, string[] carNames)
        {
            if (position == 0) return carNames[i];

            // Lookup car ahead
            if (F1ManagerPlotter.CarPositions.TryGetValue(position - 1, out string carAhead))
                return carAhead;

            return "Unknown";
        }

        // Returns the gap of the car behind the driver in [i] position
        public static float GetGapBehind(Telemetry telemetry, int position, int i, string[] carNames, int CarsOnGrid)
        {
            // Handle last position cases
            if ((CarsOnGrid == 22 && position == 21) || (CarsOnGrid == 20 && position == 19))
            {
                return 0f;
            }

            try
            {
                if(F1ManagerPlotter.CarPositions.TryGetValue(position + 1, out string carBehind))

                if (string.IsNullOrEmpty(carBehind)) return 0f;

                // Find the index of the car behind
                int behindIndex = Array.IndexOf(carNames, carBehind);
                if (behindIndex == -1) return 0f;

                // Calculate gap based on session type
                if (telemetry.Session.sessionType is 6 or 7) // Race or Sprint
                {
                    return telemetry.Car[behindIndex].Driver.GapToLeader - telemetry.Car[i].Driver.GapToLeader;
                }
                else // Other sessions
                {
                    return telemetry.Car[behindIndex].Driver.driverBestLap - telemetry.Car[i].Driver.driverBestLap;
                }
            }
            catch
            {
                return 0f;
            }
        }

        // Returns the gap of the car ahead of the driver in [i] position
        public static float GetGapInFront(Telemetry telemetry, int position, int i, string[] carNames)
        {
            // Handle first position cases
            if (position == 0)
            {
                return 0f;
            }

            try
            {
                // Lookup car ahead
                if (F1ManagerPlotter.CarPositions.TryGetValue(position - 1, out string carAhead))

                    if (string.IsNullOrEmpty(carAhead)) return 0f;

                // Find the index of the car behind
                int behindIndex = Array.IndexOf(carNames, carAhead);
                if (behindIndex == -1) return 0f;

                // Calculate gap based on session type
                if (telemetry.Session.sessionType is 6 or 7) // Race or Sprint
                {
                    return telemetry.Car[behindIndex].Driver.GapToLeader - telemetry.Car[i].Driver.GapToLeader;
                }
                else // Other sessions
                {
                    return telemetry.Car[behindIndex].Driver.driverBestLap - telemetry.Car[i].Driver.driverBestLap;
                }
            }
            catch
            {
                return 0f;
            }
        }

        // Returns the gap to the leader of the driver in [i] position
        public static float GetGapLeader(Telemetry telemetry, int position, int i, string[] carNames)
        {
            // Handle first position cases
            if (position == 0)
            {
                return 0f;
            }

            try
            {
                // Lookup car ahead
                if (F1ManagerPlotter.CarPositions.TryGetValue(0, out string carLeader))

                    if (string.IsNullOrEmpty(carLeader)) return 0f;

                // Find the index of the lead car
                int behindIndex = Array.IndexOf(carNames, carLeader);
                if (behindIndex == -1) return 0f;

                // Calculate gap based on session type
                if (telemetry.Session.sessionType is 6 or 7) // Race or Sprint
                {
                    return telemetry.Car[behindIndex].Driver.GapToLeader - telemetry.Car[i].Driver.GapToLeader;
                }
                else // Other sessions
                {
                    return telemetry.Car[behindIndex].Driver.driverBestLap - telemetry.Car[i].Driver.driverBestLap;
                }
            }
            catch
            {
                return 0f;
            }
        }
    }
}
