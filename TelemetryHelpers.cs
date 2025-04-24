using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F1Manager2024Plugin
{
    public static class TelemetryHelpers
    {

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

        public static string GetNameOfCarBehind(Telemetry telemetry, int position, int i, string[] carNames, int CarsOnGrid)
        {
            if (CarsOnGrid == 22 && position == 21) return carNames[i];

            if (CarsOnGrid == 20 && position == 19) return carNames[i];

            for (int j = 0; j < CarsOnGrid; j++)
            {
                if (telemetry.Car[j].Driver.position == position + 1) return carNames[j];
            }

            return "Unknown";
        }

        public static string GetNameOfCarAhead(Telemetry telemetry, int position, int i, string[] carNames, int CarsOnGrid)
        {
            if (position == 0) return carNames[i];

            for (int j = 0; j < CarsOnGrid; j++)
            {
                if (telemetry.Car[j].Driver.position == position - 1) return carNames[j];
            }

            return "Unknown";
        }

        public static float GetGapBehind(Telemetry telemetry, int position, int i, string[] carNames, int CarsOnGrid)
        {
            if (CarsOnGrid == 22 && position == 21) return 0f;

            if (CarsOnGrid == 20 && position == 19) return 0f;

            for (int j = 0; j < CarsOnGrid; j++)
            {
                if (telemetry.Car[j].Driver.position == position + 1)
                {
                    if (telemetry.Session.sessionType is 6 or 7)
                    {
                        return telemetry.Car[j].Driver.GapToLeader - telemetry.Car[i].Driver.GapToLeader;
                    }
                    else
                    {
                        return telemetry.Car[j].Driver.driverBestLap - telemetry.Car[i].Driver.driverBestLap;
                    }
                }
            }

            return 0f;
        }

        public static float GetGapInFront(Telemetry telemetry, int position, int i, string[] carNames, int CarsOnGrid)
        {
            if (position == 0) return 0f;

            for (int j = 0; j < CarsOnGrid; j++)
            {
                if (telemetry.Car[j].Driver.position == position - 1)
                {
                    if (telemetry.Session.sessionType is 6 or 7)
                    {
                        return telemetry.Car[i].Driver.GapToLeader - telemetry.Car[j].Driver.GapToLeader;
                    }
                    else
                    {
                        return telemetry.Car[i].Driver.driverBestLap - telemetry.Car[j].Driver.driverBestLap;
                    }
                }
            }

            return 0f;
        }

        public static float GetGapLeader(Telemetry telemetry, int position, int i, string[] carNames, int CarsOnGrid)
        {
            if (position == 0) return 0f;

            for (int j = 0; j < CarsOnGrid; j++)
            {
                if (telemetry.Car[j].Driver.position == 0)
                {
                    if (telemetry.Session.sessionType is 6 or 7)
                    {
                        return telemetry.Car[i].Driver.GapToLeader - telemetry.Car[j].Driver.GapToLeader;
                    }
                    else
                    {
                        return telemetry.Car[i].Driver.driverBestLap - telemetry.Car[j].Driver.driverBestLap;
                    }
                }
            }

            return 0f;
        }
    }
}
