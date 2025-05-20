using System.Diagnostics;
using System.Globalization;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace MemoryReader
{
    class Program
    {

        const string ProcessName = "F1Manager24";
        const long MinMemoryUsage = 1024 * 1024 * 100; // 100MB
        const string MemoryMapName = "F1ManagerTelemetry";
        const int DriverCount = 22;
        const int UpdateRateHz = 5000; // 5000Hz

        const string PluginName = "F1Manager2024Plugin.dll";
        const string PDBName = "F1Manager2024Plugin.pdb";
        const string configName = "F1Manager2024Plugin.dll.config";
        const string SQLIteInteropName = "SQLite.Interop.dll";
        const string SystemDataSQLiteName = "System.Data.SQLite.dll";
        const string SystemDataSQLiteEF6Name = "System.Data.SQLite.EF6.dll";
        const string SystemDataSQLiteLinqName = "System.Data.SQLite.Linq.dll";
        const string DapperName = "Dapper.dll";
        const string SimHubEnvVar = "SIMHUB_INSTALL_PATH";
        const string SimHubProcessName = "SimHubWPF";
        const string SimHubExeName = "SimHubWPF.exe";

        public static readonly List<string[]> _menuItems =
        [
            ["Start"],
            ["Properties", "Documentation", "GitHub"],
            ["Discord", "Overtake"],
            ["Exit"]
        ];

        public static (int row, int col) _cursor = (0, 0);


        #region Constants
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Telemetry
        {
            public SessionTelemetry Session;
            public int cameraFocus;
            public float carFloatValue;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = DriverCount)]
            public CarTelemetry[] Car;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct CarTelemetry
        {
            public int driverPos;
            public int currentLap;
            public int tireCompound;
            public int pitStopStatus;
            public int paceMode;
            public int fuelMode;
            public int ersMode;
            public float flSurfaceTemp;
            public float flTemp;
            public float flBrakeTemp;
            public float frSurfaceTemp;
            public float frTemp;
            public float frBrakeTemp;
            public float rlSurfaceTemp;
            public float rlTemp;
            public float rlBrakeTemp;
            public float rrSurfaceTemp;
            public float rrTemp;
            public float rrBrakeTemp;
            public float flWear;
            public float frWear;
            public float rlWear;
            public float rrWear;
            public float engineTemp;
            public float engineWear;
            public float gearboxWear;
            public float ersWear;
            public float charge;
            public float energyHarvested;
            public float energySpent;
            public float fuel;
            public float fuelDelta;
            public DriverTelemetry Driver;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct DriverTelemetry
        {
            public int teamId;
            public int driverNumber;
            public int driverId;
            public int turnNumber;
            public int speed;
            public int rpm;
            public int gear;
            public int position;
            public int drsMode;
            public int ERSAssist;
            public int OvertakeAggression;
            public int DefendApproach;
            public int DriveCleanAir;
            public int AvoidHighKerbs;
            public int DontFightTeammate;
            public float driverBestLap;
            public float currentLapTime;
            public float lastLapTime;
            public float lastS1Time;
            public float lastS2Time;
            public float lastS3Time;
            public float distanceTravelled;
            public float GapToLeader;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct SessionTelemetry
        {
            public float timeElapsed;
            public float rubber;
            public int trackId;
            public int sessionType;
            public WeatherTelemetry Weather;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct WeatherTelemetry
        {
            public float airTemp;
            public float trackTemp;
            public int weather;
            public float waterOnTrack;
        }
        #endregion

        private static readonly MemoryReader _mem = new();
        private static bool _isRunning = true;

        static async Task Main()
        {
            Console.Title = "Memory Reader";

            if (!OperatingSystem.IsWindows())
            {
                Console.WriteLine("This code is only supported on Windows.");
                throw new PlatformNotSupportedException("This code is only supported on Windows.");
            }

            var updateChecker = new GitHubUpdateChecker();
            bool hasUpdate = await updateChecker.CheckForUpdates();
            while (_isRunning)
            {
                Console.Clear();
                Console.CursorVisible = false;
                DisplayMenuHeader(hasUpdate);

                var input = Console.ReadKey(true).Key;

                switch (input)
                {
                    case ConsoleKey.UpArrow:
                        _cursor.row = Math.Max(0, _cursor.row - 1);
                        // Ensure column stays within bounds for new row
                        _cursor.col = Math.Min(_cursor.col, _menuItems[_cursor.row].Length - 1);
                        break;

                    case ConsoleKey.DownArrow:
                        _cursor.row = Math.Min(_menuItems.Count - 1, _cursor.row + 1);
                        // Ensure column stays within bounds for new row
                        _cursor.col = Math.Min(_cursor.col, _menuItems[_cursor.row].Length - 1);
                        break;

                    case ConsoleKey.LeftArrow:
                        _cursor.col = Math.Max(0, _cursor.col - 1);
                        break;

                    case ConsoleKey.RightArrow:
                        _cursor.col = Math.Min(_menuItems[_cursor.row].Length - 1, _cursor.col + 1);
                        break;

                    case ConsoleKey.Enter:
                        ExecuteSelectedOption();
                        break;
                }
            }

            Console.WriteLine("\nSuccessfully stopped, you can close this window.");
            Console.Read();
        }

        private static void DisplayMenuHeader(bool hasUpdate)
        {
            Console.Clear();

            MultiColorConsole.WriteCenteredColored($@"+------------------------------------------------------------------------------------+", ("+------------------------------------------------------------------------------------+", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|  _____ _   __  __    _    _   _    _    ____ _____ ____    ____   ___ ____  _  _   |", ("|", ConsoleColor.DarkRed), (@"  _____ _   __  __    _    _   _    _    ____ _____ ____    ____   ___ ____  _  _   ", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"| |  ___/ | |  \/  |  / \  | \ | |  / \  / ___| ____|  _ \  |___ \ / _ \___ \| || |  |", ("|", ConsoleColor.DarkRed), (@" |  ___/ | |  \/  |  / \  | \ | |  / \  / ___| ____|  _ \  |___ \ / _ \___ \| || |  ", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"| | |_  | | | |\/| | / _ \ |  \| | / _ \| |  _|  _| | |_) |   __) | | | |__) | || |_ |", ("|", ConsoleColor.DarkRed), (@" | |_  | | | |\/| | / _ \ |  \| | / _ \| |  _|  _| | |_) |   __) | | | |__) | || |_ ", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"| |  _| | | | |  | |/ ___ \| |\  |/ ___ \ |_| | |___|  _ <   / __/| |_| / __/|__   _||", ("|", ConsoleColor.DarkRed), (@" |  _| | | | |  | |/ ___ \| |\  |/ ___ \ |_| | |___|  _ <   / __/| |_| / __/|__   _|", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"| |_|__ |_| |_| _|_/_/ _ \_\_| \_/_/   \_\____|_____|_| \_\_|_____|\___/_____|  |_|  |", ("|", ConsoleColor.DarkRed), (@" |_|__ |_| |_| _|_/_/ _ \_\_| \_/_/   \_\____|_____|_| \_\_|_____|\___/_____|  |_|  ", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|                                                                                    |", ("|", ConsoleColor.DarkRed), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|         ____ ___ __  __ _   _ _   _ ____    ____  _    _   _  ____ ___ _   _       |", ("|", ConsoleColor.DarkRed), (@"         ____ ___ __  __ _   _ _   _ ____    ____  _    _   _  ____ ___ _   _       ", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|        / ___|_ _|  \/  | | | | | | | __ )  |  _ \| |  | | | |/ ___|_ _| \ | |      |", ("|", ConsoleColor.DarkRed), (@"        / ___|_ _|  \/  | | | | | | | __ )  |  _ \| |  | | | |/ ___|_ _| \ | |      ", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|        \___ \| || |\/| | |_| | | | |  _ \  | |_) | |  | | | | |  _ | ||  \| |      |", ("|", ConsoleColor.DarkRed), (@"        \___ \| || |\/| | |_| | | | |  _ \  | |_) | |  | | | | |  _ | ||  \| |      ", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|         ___) | || |  | |  _  | |_| | |_) | |  __/| |__| |_| | |_| || || |\  |      |", ("|", ConsoleColor.DarkRed), (@"         ___) | || |  | |  _  | |_| | |_) | |  __/| |__| |_| | |_| || || |\  |      ", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|        |____/___|_|  |_|_| |_|\___/|____/  |_|   |_____\___/ \____|___|_| \_|      |", ("|", ConsoleColor.DarkRed), (@"        |____/___|_|  |_|_| |_|\___/|____/  |_|   |_____\___/ \____|___|_| \_|      ", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|                                                                                    |", ("|", ConsoleColor.DarkRed), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|                                                                                    |", ("|", ConsoleColor.DarkRed), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"| - START TELEMETRY READER                                                           |", ("|", ConsoleColor.DarkRed), ("START TELEMETRY READER", _menuItems[_cursor.row][_cursor.col] == "Start" ? ConsoleColor.Yellow : ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|                                                                                    |", ("|", ConsoleColor.DarkRed), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"| - [Properties] - [Documentation] - [GitHub]                                        |", ("|", ConsoleColor.DarkRed), ("[Properties]", _menuItems[_cursor.row][_cursor.col] == "Properties" ? ConsoleColor.Yellow : ConsoleColor.White), ("[Documentation]", _menuItems[_cursor.row][_cursor.col] == "Documentation" ? ConsoleColor.Yellow : ConsoleColor.White), ("[GitHub]", _menuItems[_cursor.row][_cursor.col] == "GitHub" ? ConsoleColor.Yellow : ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|                                                                                    |", ("|", ConsoleColor.DarkRed), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"| - [Discord] - [Overtake.gg]                                                        |", ("|", ConsoleColor.DarkRed), ("[Discord]", _menuItems[_cursor.row][_cursor.col] == "Discord" ? ConsoleColor.Yellow : ConsoleColor.White), ("[Overtake.gg]", _menuItems[_cursor.row][_cursor.col] == "Overtake" ? ConsoleColor.Yellow : ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|                                                                                    |", ("|", ConsoleColor.DarkRed), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"| - [EXIT]                                                                           |", ("|", ConsoleColor.DarkRed), ("[EXIT]", _menuItems[_cursor.row][_cursor.col] == "Exit" ? ConsoleColor.Yellow : ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|                                                                                    |", ("|", ConsoleColor.DarkRed), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"+------------------------------------------------------------------------------------+", ("+------------------------------------------------------------------------------------+", ConsoleColor.DarkRed));

            MultiColorConsole.WriteCenteredColored($"Version: RELEASE 1.1");
            if (hasUpdate)
            {
                MultiColorConsole.WriteCenteredColored($"A new version is available!", ("A new version is available!", ConsoleColor.Red));
            }
            else
            {
                MultiColorConsole.WriteCenteredColored($"You are using the latest version.");
            }
            Console.WriteLine();
            MultiColorConsole.WriteCenteredColored($"Press the arrow keys to navigate, [ENTER] to select an option.");
            Console.WriteLine();
            MultiColorConsole.WriteCenteredColored($"Copyright Asviix 2025", ("Copyright Asviix 2025", ConsoleColor.DarkGray));
        }

        static void ExecuteSelectedOption()
        {
            string selected = _menuItems[_cursor.row][_cursor.col];
            switch (selected)
            {
                case "Start":
                    StartTelemetryReader();
                    break;

                case "Documentation": case "Properties":
                    OpenDocumentation();
                    break;

                case "GitHub":
                    OpenGitHub();
                    break;

                case "Discord":
                    OpenDiscord();
                    break;
                
                case "Overtake":
                    OpenOvertake();
                    break;

                case "Exit":
                    _isRunning = false;
                    break;
            }
        }

        private static void DisplayTelemetryHeader(string status)
        {
            const int boxWidth = 66;
            string statusLine = $"| Status: {status}".PadRight(boxWidth) + "|";

            Console.Clear();

            MultiColorConsole.WriteCenteredColored($@"+-----------------------------------------------------------------+", ("+-----------------------------------------------------------------+", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|      _____ _____ _     _____ __  __ _____ _____ ______   __     |", ("|", ConsoleColor.DarkRed), (@"      _____ _____ _     _____ __  __ _____ _____ ______   __     ", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|     |_   _| ____| |   | ____|  \/  | ____|_   _|  _ \ \ / /     |", ("|", ConsoleColor.DarkRed), (@"     |_   _| ____| |   | ____|  \/  | ____|_   _|  _ \ \ / /     ", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|       | | |  _| | |   |  _| | |\/| |  _|   | | | |_) \ V /      |", ("|", ConsoleColor.DarkRed), (@"       | | |  _| | |   |  _| | |\/| |  _|   | | | |_) \ V /      ", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|       | | | |___| |___| |___| |  | | |___  | | |  _ < | |       |", ("|", ConsoleColor.DarkRed), (@"       | | | |___| |___| |___| |  | | |___  | | |  _ < | |       ", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|       |_| |_____|_____|_____|_|  |_|_____| |_| |_| \_\|_|       |", ("|", ConsoleColor.DarkRed), (@"       |_| |_____|_____|_____|_|  |_|_____| |_| |_| \_\|_|       ", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|               ____  _____    _    ____  _____ ____              |", ("|", ConsoleColor.DarkRed), (@"               ____  _____    _    ____  _____ ____              ", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|              |  _ \| ____|  / \  |  _ \| ____|  _ \             |", ("|", ConsoleColor.DarkRed), (@"              |  _ \| ____|  / \  |  _ \| ____|  _ \             ", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|              | |_) |  _|   / _ \ | | | |  _| | |_) |            |", ("|", ConsoleColor.DarkRed), (@"              | |_) |  _|   / _ \ | | | |  _| | |_) |            ", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|              |  _ <| |___ / ___ \| |_| | |___|  _ <             |", ("|", ConsoleColor.DarkRed), (@"              |  _ <| |___ / ___ \| |_| | |___|  _ <             ", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|              |_| \_\_____/_/   \_\____/|_____|_| \_\            |", ("|", ConsoleColor.DarkRed), (@"              |_| \_\_____/_/   \_\____/|_____|_| \_\            ", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|                                                                 |", ("|", ConsoleColor.DarkRed), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored(statusLine, ("|", ConsoleColor.DarkRed), ($"Status: {status}", ConsoleColor.White), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"|                                                                 |", ("|", ConsoleColor.DarkRed), ("|", ConsoleColor.DarkRed));
            MultiColorConsole.WriteCenteredColored($@"+-----------------------------------------------------------------+", ("+-----------------------------------------------------------------+", ConsoleColor.DarkRed));
        }

        private static void StartTelemetryReader()
        {
            DisplayTelemetryHeader("Starting...");

            EnsurePluginInstalled();
            Thread.Sleep(1000);

            if (!AttachToHighMemoryProcess())
            {
                DisplayTelemetryHeader("Failed to attach to process");
                Thread.Sleep(2000);
                return;
            }

            DisplayTelemetryHeader("Connected to process");
            using var mmf = MemoryMappedFile.CreateOrOpen(MemoryMapName, Marshal.SizeOf<Telemetry>(), MemoryMappedFileAccess.ReadWrite);
            using var accessor = mmf.CreateViewAccessor(0, Marshal.SizeOf<Telemetry>(), MemoryMappedFileAccess.Write);

            byte[] buffer = new byte[Marshal.SizeOf<Telemetry>()];
            int delay = 1000 / UpdateRateHz;

            DisplayTelemetryHeader("Connected to game, press any key to stop telemetry...");

            // Run until key is pressed
            while (!Console.KeyAvailable)
            {
                Telemetry telemetry = ReadTelemetry();

                GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                Marshal.StructureToPtr(telemetry, handle.AddrOfPinnedObject(), false);
                accessor.WriteArray(0, buffer, 0, buffer.Length);
                handle.Free();

                Thread.Sleep(delay);
            }

            // Clear the key that was pressed
            Console.ReadKey(true);
        }

        private static void OpenDocumentation()
        {
            string propertiesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Properties.pdf");
            string documentationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documentation.pdf");

            try
            {
                string selected = _menuItems[_cursor.row][_cursor.col];
                switch (selected)
                {
                    case "Properties":
                        if (File.Exists(propertiesPath))
                        {
                            Process.Start(new ProcessStartInfo(propertiesPath) { UseShellExecute = true });
                        }
                        else
                        {
                            Console.WriteLine("Properties file not found.");
                        }
                        break;
                    case "Documentation":
                        if (File.Exists(documentationPath))
                        {
                            Process.Start(new ProcessStartInfo(documentationPath) { UseShellExecute = true });
                        }
                        else
                        {
                            Console.WriteLine("Documentation file not found.");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening documentation: {ex.Message}");
            }
        }

        private static void OpenGitHub()
        {
            string url = "https://github.com/Asviix/F1Manager2024Logger";

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening GitHub: {ex.Message}");
            }
        }

        private static void OpenDiscord()
        {
            string url = "https://discord.gg/gTMQJUNDxk";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening Discord: {ex.Message}");
            }
        }

        private static void OpenOvertake()
        {
            string url = "https://www.overtake.gg/downloads/f1-manager-2024-simhub-plugin.76597/";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening Overtake: {ex.Message}");
            }
        }

        static Telemetry ReadTelemetry()
        {

            var telemetry = new Telemetry
            {
                Car = new CarTelemetry[DriverCount]
            };

            string baseAddress;
            string SteamBaseAddress = "0x798F570";
            string EpicBaseAddress = "0x079F53F0";

            float SteamTestValue = _mem.ReadFloat($"F1Manager24.exe+{SteamBaseAddress},0x150,0x3E8,0x130,0x0,0x28,0x0", round: false);
            float EpicTestValue = _mem.ReadFloat($"F1Manager24.exe+{EpicBaseAddress},0x150,0x3E8,0x130,0x0,0x28,0x0", round: false);

            if (SteamTestValue == 8021.863281f)
            {
                baseAddress = SteamBaseAddress;
            }
            else if (EpicTestValue == 8214.523438f)
            {
                baseAddress = EpicBaseAddress;
            }
            else
            {
                return telemetry;
            }

            string carBasePtr = $"F1Manager24.exe+{baseAddress},0x150,0x3E8,0x130,0x0,0x28";
            string gameObjPtr = $"F1Manager24.exe+{baseAddress},0x150,0x448";

            telemetry.carFloatValue = _mem.ReadFloat(carBasePtr + ",0x0", round: false);

            for (int i = 0; i < DriverCount; i++)
            {
                int carOffset = 0x10D8 * i;

                telemetry.Car[i].driverPos = _mem.ReadInt(carBasePtr + $",0x{(carOffset + 0x710):X}");
                telemetry.Car[i].currentLap = _mem.ReadInt(carBasePtr + $",0x{(carOffset + 0x7E4):X}");
                telemetry.Car[i].pitStopStatus = _mem.ReadByte(carBasePtr + $",0x{(carOffset + 0x8A8):X}");
                telemetry.Car[i].tireCompound = _mem.ReadByte(carBasePtr + $",0x{(carOffset + 0xEF9):X}");
                telemetry.Car[i].paceMode = _mem.ReadByte(carBasePtr + $",0x{(carOffset + 0xEF1):X}");
                telemetry.Car[i].fuelMode = _mem.ReadByte(carBasePtr + $",0x{(carOffset + 0xEF0):X}");
                telemetry.Car[i].ersMode = _mem.ReadByte(carBasePtr + $",0x{(carOffset + 0xEF2):X}");
                telemetry.Car[i].Driver.ERSAssist = _mem.ReadByte(carBasePtr + $",0x{(carOffset + 0xEF3):X}");
                telemetry.Car[i].Driver.OvertakeAggression = _mem.ReadByte(carBasePtr + $",0x{(carOffset + 0xEF4):X}");
                telemetry.Car[i].Driver.DefendApproach = _mem.ReadByte(carBasePtr + $",0x{(carOffset + 0xEF5):X}");
                telemetry.Car[i].Driver.DriveCleanAir = _mem.ReadByte(carBasePtr + $",0x{(carOffset + 0xEF6):X}");
                telemetry.Car[i].Driver.AvoidHighKerbs = _mem.ReadByte(carBasePtr + $",0x{(carOffset + 0xEF7):X}");
                telemetry.Car[i].Driver.DontFightTeammate = _mem.ReadByte(carBasePtr + $",0x{(carOffset + 0xEF8):X}");

                telemetry.Car[i].flSurfaceTemp = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x97C):X}");
                telemetry.Car[i].flTemp = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x980):X}", round:false);
                telemetry.Car[i].frSurfaceTemp = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x988):X}");
                telemetry.Car[i].frTemp = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x98C):X}");
                telemetry.Car[i].rlSurfaceTemp = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x994):X}");
                telemetry.Car[i].rlTemp = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x998):X}");
                telemetry.Car[i].rrSurfaceTemp = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x9A0):X}");
                telemetry.Car[i].rrTemp = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x9A4):X}");

                telemetry.Car[i].flWear = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x984):X}");
                telemetry.Car[i].frWear = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x990):X}");
                telemetry.Car[i].rlWear = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x99C):X}");
                telemetry.Car[i].rrWear = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x9A8):X}");

                telemetry.Car[i].engineTemp = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x77C):X}");
                telemetry.Car[i].engineWear = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x784):X}");
                telemetry.Car[i].gearboxWear = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x78C):X}");
                telemetry.Car[i].ersWear = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x788):X}");

                telemetry.Car[i].charge = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x878):X}");
                telemetry.Car[i].energyHarvested = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x884):X}");
                telemetry.Car[i].energySpent = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x888):X}");
                telemetry.Car[i].fuel = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x778):X}");
                telemetry.Car[i].fuelDelta = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x7C8):X}");

                string driverPtr = carBasePtr + $",0x{(carOffset + 0x708):X}";

                telemetry.Car[i].Driver.teamId = _mem.ReadByte(driverPtr + ",0x579");
                telemetry.Car[i].Driver.driverNumber = _mem.ReadInt(driverPtr + ",0x58C");
                telemetry.Car[i].Driver.driverId = _mem.ReadInt(driverPtr + ",0x590");
                telemetry.Car[i].Driver.turnNumber = _mem.ReadInt(driverPtr + ",0x530");
                telemetry.Car[i].Driver.speed = _mem.ReadInt(driverPtr + ",0x4F0");
                telemetry.Car[i].Driver.rpm = _mem.ReadInt(driverPtr + ",0x4EC");
                telemetry.Car[i].Driver.gear = _mem.ReadInt(driverPtr + ",0x524");
                telemetry.Car[i].Driver.position = _mem.ReadInt(driverPtr + ",0x528");
                telemetry.Car[i].Driver.drsMode = _mem.ReadByte(driverPtr + ",0x521");
                telemetry.Car[i].Driver.driverBestLap = _mem.ReadFloat(driverPtr + ",0x538");
                telemetry.Car[i].Driver.currentLapTime = _mem.ReadFloat(driverPtr + ",0x544");
                telemetry.Car[i].Driver.lastLapTime = _mem.ReadFloat(driverPtr + ",0x540");
                telemetry.Car[i].Driver.lastS1Time = _mem.ReadFloat(driverPtr + ",0x548");
                telemetry.Car[i].Driver.lastS2Time = _mem.ReadFloat(driverPtr + ",0x550");
                telemetry.Car[i].Driver.lastS3Time = _mem.ReadFloat(driverPtr + ",0x558");
                telemetry.Car[i].Driver.distanceTravelled = _mem.ReadFloat(driverPtr + ",0x87C");
                telemetry.Car[i].Driver.GapToLeader = _mem.ReadFloat(driverPtr + ",0x53C");
                telemetry.Car[i].flBrakeTemp = _mem.ReadFloat(driverPtr + ",0x5D0");
                telemetry.Car[i].frBrakeTemp = _mem.ReadFloat(driverPtr + ",0x5D4");
                telemetry.Car[i].rlBrakeTemp = _mem.ReadFloat(driverPtr + ",0x5D8");
                telemetry.Car[i].rrBrakeTemp = _mem.ReadFloat(driverPtr + ",0x5DC");

                telemetry.cameraFocus = _mem.ReadInt(gameObjPtr + ",0x23C");

                string sessionPtr = gameObjPtr + ",0x260";

                telemetry.Session.timeElapsed = _mem.ReadFloat(sessionPtr + ",0x148", round:false);
                telemetry.Session.rubber = _mem.ReadFloat(sessionPtr + ",0x278");
                telemetry.Session.trackId = _mem.ReadInt(sessionPtr + ",0x228");
                telemetry.Session.sessionType = _mem.ReadInt(sessionPtr + ",0x288");
                telemetry.Session.Weather.waterOnTrack = _mem.ReadFloat(sessionPtr + ",0xA132C8");

                string weatherPtr = sessionPtr + $",0xA12990";
                telemetry.Session.Weather.airTemp = _mem.ReadFloat(weatherPtr + ",0xAC");
                telemetry.Session.Weather.trackTemp = _mem.ReadFloat(weatherPtr + ",0xB0");
                telemetry.Session.Weather.weather = _mem.ReadInt(weatherPtr + ",0xBC");
            }
            return telemetry;
        }

        static bool AttachToHighMemoryProcess()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(ProcessName);

                if (processes.Length == 0)
                {
                    return false;
                }

                Process? targetProcess = null;
                foreach (Process p in processes)
                {
                    try
                    {
                        p.Refresh();
                        if (p.WorkingSet64 >= MinMemoryUsage)
                        {
                            targetProcess = p;
                            break;
                        }
                        Console.WriteLine($"Found low-memory process (PID: {p.Id}, Memory: {p.WorkingSet64 / 1024 / 1024}MB)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error checking process {p.Id}: {ex.Message}");
                    }
                }

                if (targetProcess == null)
                {
                    Console.WriteLine($"No {ProcessName} process using ≥100MB memory found.");
                    return false;
                }

                Console.WriteLine($"Found target process (PID: {targetProcess.Id}, Memory: {targetProcess.WorkingSet64 / 1024 / 1024}MB)");
                _mem.OpenProcess(targetProcess.Id);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error attaching to process: {ex.Message}");
                return false;
            }
        }

        public static void EnsurePluginInstalled()
        {
            try
            {
                string? simHubPath = GetSimHubPath();
                if (simHubPath == null)
                {
                    Console.WriteLine("SimHub installation not found");
                    return;
                }

                // Kill SimHub process if running
                KillSimHubProcess();

                // Define all required files
                var requiredFiles = new Dictionary<string, string>
                {
                    { PluginName, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PluginName) },
                    { PDBName, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PDBName) },
                    { configName, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configName) },
                    { SQLIteInteropName, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SQLIteInteropName) },
                    { SystemDataSQLiteName, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SystemDataSQLiteName) },
                    { SystemDataSQLiteEF6Name, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SystemDataSQLiteEF6Name) },
                    { SystemDataSQLiteLinqName, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SystemDataSQLiteLinqName) },
                    { DapperName, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DapperName) }
                };

                // Check if any files are missing or need updating
                bool needsCopy = false;

                // First check if main plugin needs update
                if (File.Exists(requiredFiles[PluginName]))
                {
                    string? sourceVersion = FileVersionInfo.GetVersionInfo(requiredFiles[PluginName]).FileVersion;
                    string destPluginPath = Path.Combine(simHubPath, PluginName);

                    if (File.Exists(destPluginPath))
                    {
                        string? destVersion = FileVersionInfo.GetVersionInfo(destPluginPath).FileVersion;
                        if (sourceVersion != destVersion)
                        {
                            Console.WriteLine($"Plugin version mismatch: {sourceVersion} != {destVersion}");
                            needsCopy = true;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Plugin not found in SimHub installation");
                        needsCopy = true;
                    }
                }

                // Check if any dependency is missing
                foreach (var file in requiredFiles)
                {
                    string destPath = Path.Combine(simHubPath, file.Key);

                    if (!File.Exists(destPath) && File.Exists(file.Value))
                    {
                        Console.WriteLine($"Dependency missing: {file.Key}");
                        needsCopy = true;
                        break;
                    }
                }

                // Copy all files if needed
                if (needsCopy)
                {
                    Console.WriteLine("Copying plugin and dependencies...");

                    foreach (var file in requiredFiles)
                    {
                        string sourcePath = file.Value;
                        string destPath = Path.Combine(simHubPath, file.Key);

                        if (File.Exists(sourcePath))
                        {
                            try
                            {
                                // Ensure directory exists
                                if (destPath is null)
                                    throw new ArgumentNullException(nameof(destPath));

                                string? directory = Path.GetDirectoryName(destPath);
                                if (directory is not null)
                                    Directory.CreateDirectory(directory);

                                // Copy with retry logic
                                RetryFileCopy(sourcePath, destPath);
                                Console.WriteLine($"Copied: {file.Key}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to copy {file.Key}: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Source file not found: {file.Key}");
                        }
                    }
                }

                // Start SimHub
                StartSimHub(simHubPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error installing plugin: {ex}");
            }
        }

        private static void RetryFileCopy(string source, string dest, int maxRetries = 3, int delayMs = 500)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    File.Copy(source, dest, overwrite: true);
                    return;
                }
                catch when (i < maxRetries - 1)
                {
                    Thread.Sleep(delayMs);
                }
            }
            throw new IOException($"Failed to copy {source} to {dest} after {maxRetries} attempts");
        }

        private static bool KillSimHubProcess()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(SimHubProcessName);
                if (processes.Length == 0) return false;

                Console.WriteLine($"Found {processes.Length} SimHub process(es), attempting to close...");

                foreach (Process process in processes)
                {
                    try
                    {
                        process.Kill();
                        Thread.Sleep(2000); // Wait up to 5 seconds for process to exit
                        Console.WriteLine($"Successfully closed SimHub process (PID: {process.Id})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error closing SimHub process (PID: {process.Id}): {ex.Message}");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for SimHub processes: {ex.Message}");
                return false;
            }
        }

        private static void StartSimHub(string simHubPath)
        {
            try
            {
                string simHubExePath = Path.Combine(simHubPath, SimHubExeName);
                if (!File.Exists(simHubExePath))
                {
                    Console.WriteLine($"SimHub executable not found at: {simHubExePath}");
                    Thread.Sleep(1000);
                    return;
                }

                Process.Start(simHubExePath);
                Console.WriteLine("Successfully started SimHub");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting SimHub: {ex.Message}");
            }
        }

        private static string? GetSimHubPath()
        {
            // 1. Check environment variable
            string? envPath = Environment.GetEnvironmentVariable(SimHubEnvVar, EnvironmentVariableTarget.User);
            if (!string.IsNullOrEmpty(envPath)) return envPath;

            // 2. Check common installation paths
            string[] commonPaths = [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SimHub"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "SimHub"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SimHub")
            ];

            foreach (var path in commonPaths)
            {
                if (Directory.Exists(path)) return path;
            }

            return null;
        }

        public static class MultiColorConsole
        {
            public static void WriteCenteredColored(string text, params (string text, ConsoleColor color)[] coloredParts)
            {
                int consoleWidth = Console.WindowWidth;
                int totalLength = text.Length;
                int startPos = (consoleWidth - totalLength) / 2;

                if (startPos < 0) startPos = 0;

                Console.SetCursorPosition(startPos, Console.CursorTop);

                int currentIndex = 0;
                foreach (var part in coloredParts)
                {
                    // Write any uncolored text before this part
                    if (currentIndex < text.IndexOf(part.text, currentIndex))
                    {
                        Console.Write(text.Substring(currentIndex, text.IndexOf(part.text, currentIndex) - currentIndex));
                    }

                    // Write the colored part
                    Console.ForegroundColor = part.color;
                    Console.Write(part.text);
                    Console.ResetColor();

                    currentIndex = text.IndexOf(part.text, currentIndex) + part.text.Length;
                }

                // Write any remaining text
                if (currentIndex < text.Length)
                {
                    Console.Write(text.Substring(currentIndex));
                }

                Console.WriteLine();
            }
        }
    }

    class GitHubUpdateChecker
    {
        private const string CurrentVersion = "1.1";
        private const string RepoUrl = "https://github.com/Asviix/F1Manager2024Logger";

        public async Task<bool> CheckForUpdates()
        {
            try
            {
                var latestVersion = await GetLatestVersion();

                if (IsVersionNewer(latestVersion))
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch
            {
                return true;
            }
        }

        private async Task<string> GetLatestVersion()
        {
            using var httpClient = new HttpClient();

            // GitHub requires a User-Agent header
            httpClient.DefaultRequestHeaders.Add("User-Agent", "MyAppUpdateChecker");

            // Get the Atom feed for releases
            var atomFeed = await httpClient.GetStringAsync($"{RepoUrl}/releases.atom");

            // Parse the XML to get the latest version
            var doc = XDocument.Parse(atomFeed);
            var ns = XNamespace.Get("http://www.w3.org/2005/Atom");

            // The first entry contains the latest release
            var latestEntry = doc.Root?.Element(ns + "entry");
            if (latestEntry == null)
            {
                throw new Exception("No releases found in Atom feed");
            }
            if (latestEntry == null)
                throw new Exception("No releases found in Atom feed");

            // The title contains the version (format: "Release v1.2.3")
            var title = latestEntry.Element(ns + "title")?.Value;
            if (string.IsNullOrWhiteSpace(title))
                throw new Exception("Could not parse release version");

            // Extract version number (handles formats like "v1.2.3" or "Release 1.2.3")
            return ExtractVersionFromTitle(title);
        }

        private string ExtractVersionFromTitle(string title)
        {
            // Handle different title formats:
            // "Release v1.2.3"
            // "v1.2.3"
            // "1.2.3"

            // Find the first sequence that looks like a version number
            var start = title.IndexOf('v') + 1;
            if (start == 0) start = title.IndexOf(' ') + 1;
            if (start < 0) start = 0;

            // Take everything from the version start to the end or next space
            var end = title.IndexOf(' ', start);
            if (end < 0) end = title.Length;

            return title[start..end].Trim();
        }

        private bool IsVersionNewer(string latestVersion)
        {
            try
            {
                // Normalize versions by removing 'v' prefix
                var current = Version.Parse(CurrentVersion.Substring(latestVersion.IndexOf('_')));
                var latest = Version.Parse(latestVersion.Substring(latestVersion.IndexOf('_')));
                return latest > current;
            }
            catch
            {
                // Fallback to string comparison if version parsing fails
                return string.CompareOrdinal(latestVersion, CurrentVersion) > 0;
            }
        }
    }

    public class Proc
    {
        public Process? Process { get; set; }
        public IntPtr Handle { get; set; }
        public bool Is64Bit { get; set; }
        public ProcessModule? MainModule { get; set; }
    }

    public class MemoryReader: IDisposable
    {

        public Proc mProc = new Proc();

        private IntPtr _processHandle = IntPtr.Zero;
        private bool _disposed = false;

        // Windows API imports
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
            UInt32 dwDesiredAccess,
            bool bInheritHandle,
            Int32 dwProcessId
            );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, UIntPtr nSize, out ulong lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] IntPtr lpBuffer, UIntPtr nSize, out ulong lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32")]
        public static extern bool IsWow64Process(IntPtr hProcess, out bool lpSystemInfo);

        // Process access rights needed
        private const int PROCESS_VM_READ = 0x0010;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;

        public IntPtr GetModuleAddressByName(string name)
        {
            // Ensure mProc.Process is not null before accessing its Modules property
            if (mProc.Process == null)
            {
                throw new InvalidOperationException("Process is not initialized.");
            }

            // Use SingleOrDefault safely by checking for null before accessing BaseAddress
            var module = mProc.Process.Modules.Cast<ProcessModule>()
                .SingleOrDefault(m => string.Equals(m.ModuleName, name, StringComparison.OrdinalIgnoreCase));

            return module == null ? throw new InvalidOperationException($"Module with name '{name}' not found.") : module.BaseAddress;
        }

        public UIntPtr GetCode(string name, int size = 16)
        {
            string theCode = "";
            theCode = name;

            if (String.IsNullOrEmpty(theCode))
                return UIntPtr.Zero;

            // remove spaces
            if (theCode.Contains(" "))
                theCode.Replace(" ", String.Empty);

            string newOffsets = theCode;
            if (theCode.Contains("+"))
                newOffsets = theCode.Substring(theCode.IndexOf('+') + 1);

            byte[] memoryAddress = new byte[size];

            if (!theCode.Contains("+") && !theCode.Contains(","))
            {
                try
                {
                    return new UIntPtr(Convert.ToUInt64(theCode, 16));
                }
                catch
                {
                    Console.WriteLine("Error in GetCode(). Failed to read address " + theCode);
                    return UIntPtr.Zero;
                }
            }

            if (newOffsets.Contains(','))
            {
                List<Int64> offsetsList = new List<Int64>();

                string[] newerOffsets = newOffsets.Split(',');
                foreach (string oldOffsets in newerOffsets)
                {
                    string test = oldOffsets;
                    if (oldOffsets.Contains("0x")) test = oldOffsets.Replace("0x", "");
                    Int64 preParse = 0;
                    if (!oldOffsets.Contains("-"))
                        preParse = Int64.Parse(test, NumberStyles.AllowHexSpecifier);
                    else
                    {
                        test = test.Replace("-", "");
                        preParse = Int64.Parse(test, NumberStyles.AllowHexSpecifier);
                        preParse = preParse * -1;
                    }
                    offsetsList.Add(preParse);
                }
                Int64[] offsets = offsetsList.ToArray();

                bool mainBase = (theCode.ToLower().Contains("base") || theCode.ToLower().Contains("main")) && !theCode.ToLower().Contains(".dll") && !theCode.ToLower().Contains(".exe");

                if (mainBase)
                    // Updated line to handle possible null reference for mProc.MainModule
                    if (mProc.MainModule != null)
                    {
                        ReadProcessMemory(mProc.Handle, (UIntPtr)((Int64)mProc.MainModule.BaseAddress + offsets[0]), memoryAddress, (UIntPtr)size, IntPtr.Zero);
                    }
                    else
                    {
                        throw new InvalidOperationException("MainModule is null. Ensure the process is properly initialized.");
                    }
                else if (!mainBase && theCode.Contains("+"))
                {
                    string[] moduleName = theCode.Split('+');
                    IntPtr altModule = IntPtr.Zero;
                    if (!moduleName[0].ToLower().Contains(".dll") && !moduleName[0].ToLower().Contains(".exe") && !moduleName[0].ToLower().Contains(".bin"))
                        altModule = (IntPtr)Int64.Parse(moduleName[0], System.Globalization.NumberStyles.HexNumber);
                    else
                    {
                        try
                        {
                            altModule = GetModuleAddressByName(moduleName[0]);
                        }
                        catch
                        {
                            Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                            //Debug.WriteLine("Modules: " + string.Join(",", mProc.Modules));
                        }
                    }
                    ReadProcessMemory(mProc.Handle, (UIntPtr)((Int64)altModule + offsets[0]), memoryAddress, (UIntPtr)size, IntPtr.Zero);
                }
                else // no offsets
                    ReadProcessMemory(mProc.Handle, (UIntPtr)(offsets[0]), memoryAddress, (UIntPtr)size, IntPtr.Zero);

                Int64 num1 = BitConverter.ToInt64(memoryAddress, 0);

                UIntPtr base1 = (UIntPtr)0;

                for (int i = 1; i < offsets.Length; i++)
                {
                    base1 = new UIntPtr(Convert.ToUInt64(num1 + offsets[i]));
                    ReadProcessMemory(mProc.Handle, base1, memoryAddress, (UIntPtr)size, IntPtr.Zero);
                    num1 = BitConverter.ToInt64(memoryAddress, 0);
                }
                return base1;
            }
            else
            {
                Int64 trueCode = Convert.ToInt64(newOffsets, 16);
                IntPtr altModule = IntPtr.Zero;

                bool mainBase = (theCode.ToLower().Contains("base") || theCode.ToLower().Contains("main")) && !theCode.ToLower().Contains(".dll") && !theCode.ToLower().Contains(".exe");

                if (mainBase)
                    if (mProc.MainModule != null)
                    {
                        altModule = mProc.MainModule.BaseAddress;
                    }
                    else
                    {
                        throw new InvalidOperationException("MainModule is null. Ensure the process is properly initialized.");
                    }
                else if (!mainBase && theCode.Contains("+"))
                {
                    string[] moduleName = theCode.Split('+');
                    if (!moduleName[0].ToLower().Contains(".dll") && !moduleName[0].ToLower().Contains(".exe") && !moduleName[0].ToLower().Contains(".bin"))
                    {
                        string theAddr = moduleName[0];
                        if (theAddr.Contains("0x")) theAddr = theAddr.Replace("0x", "");
                        altModule = (IntPtr)Int64.Parse(theAddr, NumberStyles.HexNumber);
                    }
                    else
                    {
                        try
                        {
                            altModule = GetModuleAddressByName(moduleName[0]);
                        }
                        catch
                        {
                            Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                            //Debug.WriteLine("Modules: " + string.Join(",", mProc.Modules));
                        }
                    }
                }
                else
                    altModule = GetModuleAddressByName(theCode.Split('+')[0]);
                return (UIntPtr)((Int64)altModule + trueCode);
            }
        }

        public void OpenProcess(int processId)
        {
            mProc.Process = Process.GetProcessById(processId);

            mProc.Handle = OpenProcess(0x1F0FFF, true, processId);

            mProc.Is64Bit = Environment.Is64BitOperatingSystem && (IsWow64Process(mProc.Handle, out bool retVal) && !retVal);

            mProc.MainModule = mProc.Process.MainModule;
        }

        public int ReadByte(string code)
        {
            byte[] memoryTiny = new byte[1];

            UIntPtr theCode = GetCode(code);
            if (theCode != UIntPtr.Zero && theCode.ToUInt64() >= 0x10000)
            {
                if (ReadProcessMemory(mProc.Handle, theCode, memoryTiny, (UIntPtr)1, IntPtr.Zero))
                    return memoryTiny[0];

                return 0;
            }

            return 0;
        }

        public int ReadInt(string code)
        {
            byte[] memory = new byte[4];
            UIntPtr theCode = GetCode(code);
            if (theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return 0;

            if (ReadProcessMemory(mProc.Handle, theCode, memory, (UIntPtr)4, IntPtr.Zero))
                return BitConverter.ToInt32(memory, 0);
            else
                return 0;
        }

        public float ReadFloat(string code, bool round = false)
        {
            byte[] memory = new byte[4];

            UIntPtr theCode = GetCode(code);
            if (theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return 0;

            try
            {
                if (ReadProcessMemory(mProc.Handle, theCode, memory, (UIntPtr)4, IntPtr.Zero))
                {
                    float address = BitConverter.ToSingle(memory, 0);
                    float returnValue = (float)address;
                    if (round)
                        returnValue = (float)Math.Round(address, 2);
                    return returnValue;
                }
                else
                    return 0;
            }
            catch
            {
                return 0;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Free managed resources
                }
                _disposed = true;
            }
        }

        ~MemoryReader()
        {
            Dispose(false);
        }
    }
}