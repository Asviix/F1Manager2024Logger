using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SimHub.Plugins;

namespace F1Manager2024Plugin
{
    public class MmfReader
    {
        public event Action<Telemetry> DataReceived;

        private bool _isReading;
        private Task _readingTask;
        private CancellationTokenSource _cts;
        private string _currentMapName;

        public void StartReading(string mapName)
        {
            if (string.IsNullOrWhiteSpace(mapName))
            {
                SimHub.Logging.Current.Info("No memory map name specified");
                return;
            }

            if (_isReading && mapName == _currentMapName)
            {
                return; // Already reading this map
            }

            StopReading(); // Stop any existing reading

            _currentMapName = mapName;
            _cts = new CancellationTokenSource();
            _isReading = true;

            _readingTask = Task.Run(() =>
            {
                try
                {
                    using (var mmf = MemoryMappedFile.OpenExisting(mapName, MemoryMappedFileRights.Read))
                    using (var accessor = mmf.CreateViewAccessor(0, Marshal.SizeOf<Telemetry>(), MemoryMappedFileAccess.Read))
                    {
                        byte[] buffer = new byte[Marshal.SizeOf<Telemetry>()];

                        while (_isReading && !_cts.IsCancellationRequested)
                        {

                            try
                            {
                                accessor.ReadArray(0, buffer, 0, buffer.Length);
                                GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                                var telemetry = Marshal.PtrToStructure<Telemetry>(handle.AddrOfPinnedObject());
                                handle.Free();

                                DataReceived?.Invoke(telemetry);
                                Thread.Sleep(10); // Adjust as needed
                            }
                            catch (Exception ex)
                            {
                                SimHub.Logging.Current.Error($"Read error: {ex.Message}");
                                Thread.Sleep(100);
                            }
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    StopReading();
                    Thread.Sleep(1000);
                    StartReading(mapName);
                }
            }, _cts.Token);
        }

        public void StopReading()
        {
            _isReading = false;
            _cts?.Cancel();
            try
            {
                _readingTask?.Wait(500);
            }
            catch (AggregateException) { } // Ignore task cancellation exceptions
            finally
            {
                _cts?.Dispose();
                _cts = null;
                _readingTask = null;
            }
        }
    }

    // Add the same struct definitions from Program.cs here
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Telemetry
    {
        public SessionTelemetry Session;
        public int cameraFocus;
        public float carFloatValue;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
        public CarTelemetry[] Car;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CarTelemetry
    {
        public int driverPos;
        public int currentLap;
        public int tireCompound;
        public int pitStopStatus;
        public int paceMode;
        public int fuelMode;
        public int ersMode;
        public float flTemp;
        public float frTemp;
        public float rlTemp;
        public float rrTemp;
        public float flWear;
        public float frWear;
        public float rlWear;
        public float rrWear;
        public float engineTemp;
        public float engineWear;
        public float gearboxWear;
        public float ersWear;
        public float charge;
        public float fuel;
        public DriverTelemetry Driver;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DriverTelemetry
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
    public struct SessionTelemetry
    {
        public float timeElapsed;
        public float rubber;
        public float bestSessionTime;
        public int trackId;
        public int sessionType;
        public WeatherTelemetry Weather;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WeatherTelemetry
    {
        public float airTemp;
        public float trackTemp;
        public int weather;
    }
}