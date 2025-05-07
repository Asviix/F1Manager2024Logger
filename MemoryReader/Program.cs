using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Threading;
using Memory;

namespace MemoryReader
{
    class Program
    {
        const string ProcessName = "F1Manager24";
        const long MinMemoryUsage = 1024 * 1024 * 100; // 100MB
        const string MemoryMapName = "F1ManagerTelemetry";
        const int DriverCount = 22;
        const int UpdateRateHz = 5000; // 5000Hz

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

        public static class SimHubPluginInstaller
        {
            const string PluginName = "F1Manager2024Plugin.dll";
            const string PDBName = "F1Manager2024Plugin.pdb";
            const string SimHubEnvVar = "SIMHUB_INSTALL_PATH";
            const string SimHubProcessName = "SimHubWPF";
            const string SimHubExeName = "SimHubWPF.exe";

            public static void EnsurePluginInstalled()
            {
                try
                {
                    string simHubPath = GetSimHubPath();
                    if (simHubPath == null)
                    {
                        Console.WriteLine("SimHub installation not found");
                        return;
                    }

                    // Kill SimHub process if running
                    KillSimHubProcess();

                    bool shouldCopy = false;

                    string destPluginPath = Path.Combine(simHubPath, PluginName);
                    string destPDBPath = Path.Combine(simHubPath, PDBName);
                    string sourcePluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PluginName);
                    string sourcePDBPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PDBName);
                    string sourceVersion = FileVersionInfo.GetVersionInfo(sourcePluginPath).FileVersion;

                    if (!File.Exists(sourcePluginPath) || !File.Exists(sourcePDBPath))
                    {
                        Console.WriteLine($"Source plugin file not found at: {sourcePluginPath}");
                        return;
                    }

                    if (File.Exists(destPluginPath) && File.Exists(destPDBPath))
                    {
                        string destVersion = FileVersionInfo.GetVersionInfo(destPluginPath).FileVersion;
                        if (sourceVersion != destVersion)
                        {
                            Console.WriteLine($"Plugin version mismatch: {sourceVersion} != {destVersion}");
                            shouldCopy = true;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Plugin not found in SimHub installation, copying...");
                        shouldCopy = true;
                    }

                    if (shouldCopy)
                    {
                        try
                        {
                            // Force overwrite the file
                            File.Copy(sourcePluginPath, destPluginPath, overwrite: true);
                            Thread.Sleep(500);
                            File.Copy(sourcePDBPath, destPDBPath, overwrite: true);
                            Console.WriteLine($"Successfully installed plugin to: {destPluginPath}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error copying plugin: {ex.Message}");
                            // Try again after a short delay in case of file locks
                            Thread.Sleep(500);
                            File.Copy(sourcePluginPath, destPluginPath, overwrite: true);
                            File.Copy(sourcePDBPath, destPDBPath, overwrite: true);
                            Console.WriteLine($"Successfully installed plugin on second attempt: {destPluginPath}");
                        }
                    }

                    // Start SimHub
                    StartSimHub(simHubPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error installing plugin: {ex.ToString()}");
                }
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
                string envPath = Environment.GetEnvironmentVariable(SimHubEnvVar, EnvironmentVariableTarget.User);
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
        }

        private static readonly Mem _mem = new();

        static void Main()
        {
            Console.Title = "F1 Manager 2024 Memory Reader";
            Console.WriteLine("F1ManagerTelemetryWriter starting...");

            Thread.Sleep(1000);

            SimHubPluginInstaller.EnsurePluginInstalled();

            Thread.Sleep(1000);

            while (!AttachToHighMemoryProcess())
            {
                Thread.Sleep(1000);
            }

            Console.WriteLine($"Attached to F1Manager24 process (ID: {_mem.GetProcIdFromName(ProcessName)})");

            using var mmf = MemoryMappedFile.CreateOrOpen(MemoryMapName, Marshal.SizeOf<Telemetry>(), MemoryMappedFileAccess.ReadWrite);
            using var accessor = mmf.CreateViewAccessor(0, Marshal.SizeOf<Telemetry>(), MemoryMappedFileAccess.Write);

            byte[] buffer = new byte[Marshal.SizeOf<Telemetry>()];
            int delay = 1000 / UpdateRateHz;

            Console.WriteLine("----------\nMemory Map Created, Data is being sent to SimHub.\n----------");
            Console.WriteLine("To Exit, press \"CTRL+C\" or Close to window.");

            while (true)
            {
                Telemetry telemetry = ReadTelemetry();

                GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                Marshal.StructureToPtr(telemetry, handle.AddrOfPinnedObject(), false);
                accessor.WriteArray(0, buffer, 0, buffer.Length);
                handle.Free();

                Thread.Sleep(delay);
            }
        }

        static Telemetry ReadTelemetry()
        {
            var telemetry = new Telemetry
            {
                Car = new CarTelemetry[DriverCount]
            };

            string carBasePtr = "F1Manager24.exe+0x798F570,0x150,0x3E8,0x130,0x0,0x28";
            string gameObjPtr = "F1Manager24.exe+0x0798F570,0x150,0x448";

            for (int i = 0; i < DriverCount; i++)
            {
                int carOffset = 0x10D8 * i;

                try
                {
                    telemetry.carFloatValue = _mem.ReadFloat(carBasePtr + ",0x0");
                }
                catch {
                    telemetry.carFloatValue = 0f;
                    return telemetry; ;
                }

                if (telemetry.carFloatValue != 8021.86f) return telemetry;

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

                telemetry.Car[i].flSurfaceTemp = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x97C):X}", round: false);
                telemetry.Car[i].flTemp = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x980):X}", round:false);
                telemetry.Car[i].frSurfaceTemp = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x988):X}", round: false);
                telemetry.Car[i].frTemp = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x98C):X}", round: false);
                telemetry.Car[i].rlSurfaceTemp = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x994):X}", round: false);
                telemetry.Car[i].rlTemp = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x998):X}", round: false);
                telemetry.Car[i].rrSurfaceTemp = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x9A0):X}", round: false);
                telemetry.Car[i].rrTemp = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x9A4):X}", round: false);

                telemetry.Car[i].flWear = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x984):X}", round: false);
                telemetry.Car[i].frWear = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x990):X}", round: false);
                telemetry.Car[i].rlWear = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x99C):X}", round: false);
                telemetry.Car[i].rrWear = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x9A8):X}", round: false);

                telemetry.Car[i].engineTemp = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x77C):X}", round: false);
                telemetry.Car[i].engineWear = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x784):X}", round: false);
                telemetry.Car[i].gearboxWear = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x78C):X}", round: false);
                telemetry.Car[i].ersWear = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x788):X}", round: false);

                telemetry.Car[i].charge = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x878):X}", round: false);
                telemetry.Car[i].energyHarvested = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x884):X}", round: false);
                telemetry.Car[i].energySpent = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x888):X}", round: false);
                telemetry.Car[i].fuel = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x778):X}", round: false);
                telemetry.Car[i].fuelDelta = _mem.ReadFloat(carBasePtr + $",0x{(carOffset + 0x7C8):X}", round: false);

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
                telemetry.Car[i].Driver.driverBestLap = _mem.ReadFloat(driverPtr + ",0x538", round: false);
                telemetry.Car[i].Driver.currentLapTime = _mem.ReadFloat(driverPtr + ",0x544", round: false);
                telemetry.Car[i].Driver.lastLapTime = _mem.ReadFloat(driverPtr + ",0x540", round: false);
                telemetry.Car[i].Driver.lastS1Time = _mem.ReadFloat(driverPtr + ",0x548", round: false);
                telemetry.Car[i].Driver.lastS2Time = _mem.ReadFloat(driverPtr + ",0x550", round: false);
                telemetry.Car[i].Driver.lastS3Time = _mem.ReadFloat(driverPtr + ",0x558", round: false);
                telemetry.Car[i].Driver.distanceTravelled = _mem.ReadFloat(driverPtr + ",0x87C", round: false);
                telemetry.Car[i].Driver.GapToLeader = _mem.ReadFloat(driverPtr + ",0x53C", round: false);
                telemetry.Car[i].flBrakeTemp = _mem.ReadFloat(driverPtr + ",0x5D0", round: false);
                telemetry.Car[i].frBrakeTemp = _mem.ReadFloat(driverPtr + ",0x5D4", round: false);
                telemetry.Car[i].rlBrakeTemp = _mem.ReadFloat(driverPtr + ",0x5D8", round: false);
                telemetry.Car[i].rrBrakeTemp = _mem.ReadFloat(driverPtr + ",0x5DC", round: false);

                telemetry.cameraFocus = _mem.ReadInt(gameObjPtr + ",0x23C");

                string sessionPtr = gameObjPtr + ",0x260";

                telemetry.Session.timeElapsed = _mem.ReadFloat(sessionPtr + ",0x148", round:false);
                telemetry.Session.rubber = _mem.ReadFloat(sessionPtr + ",0x278", round: false);
                telemetry.Session.trackId = _mem.ReadInt(sessionPtr + ",0x228");
                telemetry.Session.sessionType = _mem.ReadInt(sessionPtr + ",0x288");
                telemetry.Session.Weather.waterOnTrack = _mem.ReadFloat(sessionPtr + ",0xA132C8", round: false);

                string weatherPtr = sessionPtr + $",0xA12990";
                telemetry.Session.Weather.airTemp = _mem.ReadFloat(weatherPtr + ",0xAC", round: false);
                telemetry.Session.Weather.trackTemp = _mem.ReadFloat(weatherPtr + ",0xB0", round: false);
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
                return _mem.OpenProcess(targetProcess.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error attaching to process: {ex.Message}");
                return false;
            }
        }
    }
}