using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using SimHub.Plugins;

namespace F1Manager2024Plugin
{
    public class Exporter
    {
        private readonly Dictionary<string, string> _driverFilePaths = new Dictionary<string, string>();
        private readonly Dictionary<string, bool> _headersWritten = new Dictionary<string, bool>();

        public void ExportData(string carName, CarTelemetry car, F1Manager2024PluginSettings Settings)
        {
            if (!Settings.ExporterEnabled || !Settings.TrackedDrivers.Contains(carName)) return; // Return if Exporter isn't Enabled of car isn't Tracked.
            try
            {
                string trackName = F1ManagerPlotter.GetTrackName(car.Driver.Session.trackId);
                string sessionType = F1ManagerPlotter.GetSessionType(car.Driver.Session.sessionType);

                string basePath = Settings.ExporterPath ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "F1ManagerTelemetry");

                string sessionFolder = Path.Combine(basePath, "exported_data", $"{trackName} {sessionType}");
                string carFolder = Path.Combine(sessionFolder, carName);

                Directory.CreateDirectory(carFolder);

                // Initialize file path for this driver if not exists
                if (!_driverFilePaths.ContainsKey(carName))
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    _driverFilePaths[carName] = Path.Combine(carFolder, $"{carName}_Telemetry_{timestamp}.csv");
                    _headersWritten[carName] = false;
                }

                string filePath = _driverFilePaths[carName];
                bool headersWritten = _headersWritten[carName];


                var telemetryData = new Dictionary<string, object>
                {
                    // Session data
                    ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    ["trackName"] = F1ManagerPlotter.GetTrackName(car.Driver.Session.trackId) ?? "",
                    ["sessionType"] = F1ManagerPlotter.GetSessionType(car.Driver.Session.sessionType) ?? "",
                    ["timeElapsed"] = car.Driver.Session.timeElapsed,

                    // Driver info
                    ["driverNumber"] = car.Driver.driverNumber,
                    ["pitstopStatus"] = F1ManagerPlotter.GetPitStopStatus(car.pitStopStatus) ?? "",
                    ["currentLap"] = car.currentLap + 1, // Adjust for index
                    ["turnNumber"] = car.Driver.turnNumber,
                    ["position"] = car.Driver.position + 1, // Adjust for 0-based index

                    // Tyres
                    ["compound"] = F1ManagerPlotter.GetTireCompound(car.tireCompound) ?? "",
                    ["flTemp"] = car.flTemp,
                    ["flDeg"] = car.flWear,
                    ["frTemp"] = car.frTemp,
                    ["frDeg"] = car.frWear,
                    ["rlTemp"] = car.rlTemp,
                    ["rlDeg"] = car.rlWear,
                    ["rrTemp"] = car.rrTemp,
                    ["rrDeg"] = car.rrWear,

                    // Car telemetry
                    ["speed"] = car.Driver.speed,
                    ["rpm"] = car.Driver.rpm,
                    ["gear"] = car.Driver.gear,

                    // Components
                    ["engineTemp"] = car.engineTemp,
                    ["engineDeg"] = car.engineWear,
                    ["gearboxDeg"] = car.gearboxWear,
                    ["ersDeg"] = car.ersWear,

                    // Energy
                    ["charge"] = car.charge,
                    ["fuel"] = car.fuel,

                    // Modes
                    ["paceMode"] = F1ManagerPlotter.GetPaceMode(car.paceMode) ?? "",
                    ["fuelMode"] = F1ManagerPlotter.GetFuelMode(car.fuelMode) ?? "",
                    ["ersMode"] = F1ManagerPlotter.GetERSMode(car.ersMode) ?? "",
                    ["drsMode"] = F1ManagerPlotter.GetDRSMode(car.Driver.drsMode) ?? "",

                    // Timings
                    ["currentLapTime"] = car.Driver.currentLapTime,
                    ["driverBestLap"] = car.Driver.driverBestLap,
                    ["lastLapTime"] = car.Driver.lastLapTime,
                    ["lastS1Time"] = car.Driver.lastS1Time,
                    ["lastS2Time"] = car.Driver.lastS2Time,
                    ["lastS3Time"] = car.Driver.lastS3Time,

                    // Session info
                    ["bestSessionTime"] = car.Driver.Session.bestSessionTime,
                    ["rubber"] = car.Driver.Session.rubber,
                    ["airTemp"] = car.Driver.Session.Weather.airTemp,
                    ["trackTemp"] = car.Driver.Session.Weather.trackTemp,
                    ["weather"] = F1ManagerPlotter.GetWeather(car.Driver.Session.Weather.weather) ?? ""
                };

                // Write to CSV
                using (var writer = new StreamWriter(filePath, true))
                {
                    if (!headersWritten)
                    {
                        // Write headers in the specified order
                        writer.WriteLine(string.Join(",", telemetryData.Keys));
                        _headersWritten[carName] = true;
                    }

                    // Write values in the same order as headers
                    writer.WriteLine(string.Join(",", telemetryData.Values.Select(v => EscapeCsvValue(v?.ToString()))));
                }
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error($"Export error for {carName}: {ex.Message}");
            }
        }

        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
                return $"\"{value.Replace("\"", "\"\"")}\"";

            return value;
        }
    }
}