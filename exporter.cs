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

        public void ExportData(string carName, Telemetry telemetry, int i, F1Manager2024PluginSettings Settings)
        {
            if (!Settings.ExporterEnabled || !Settings.TrackedDrivers.Contains(carName)) return; // Return if Exporter isn't Enabled of car isn't Tracked.
            try
            {
                string trackName = F1ManagerPlotter.GetTrackName(telemetry.Session.trackId);
                string sessionType = F1ManagerPlotter.GetSessionType(telemetry.Session.sessionType);

                string basePath = Settings.ExporterPath ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "F1ManagerTelemetry");

                string sessionFolder = Path.Combine(basePath, "exported_data", $"{trackName} {sessionType}");
                string carFolder = Path.Combine(sessionFolder, String.Join(" ", F1ManagerPlotter.GetDriverFirstName(telemetry.Car[i].Driver.driverId), F1ManagerPlotter.GetDriverLastName(telemetry.Car[i].Driver.driverId)));

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
                    ["trackName"] = F1ManagerPlotter.GetTrackName(telemetry.Session.trackId) ?? "",
                    ["sessionType"] = F1ManagerPlotter.GetSessionType(telemetry.Session.sessionType) ?? "",
                    ["timeElapsed"] = telemetry.Session.timeElapsed,

                    // Driver info
                    ["driverNumber"] = telemetry.Car[i].Driver.driverNumber,
                    ["driverFirstName"] = F1ManagerPlotter.GetDriverFirstName(telemetry.Car[i].Driver.driverId) ?? "",
                    ["driverLastName"] = F1ManagerPlotter.GetDriverLastName(telemetry.Car[i].Driver.driverId) ?? "",
                    ["teamName"] = F1ManagerPlotter.GetTeamName(telemetry.Car[i].Driver.teamId, Settings) ?? "",
                    ["pitstopStatus"] = F1ManagerPlotter.GetPitStopStatus(telemetry.Car[i].pitStopStatus) ?? "",
                    ["currentLap"] = telemetry.Car[i].currentLap + 1, // Adjust for index
                    ["turnNumber"] = telemetry.Car[i].Driver.turnNumber,
                    ["position"] = telemetry.Car[i].Driver.position + 1, // Adjust for 0-based index

                    // Tyres
                    ["compound"] = F1ManagerPlotter.GetTireCompound(telemetry.Car[i].tireCompound) ?? "",
                    ["flTemp"] = telemetry.Car[i].flTemp,
                    ["flDeg"] = telemetry.Car[i].flWear,
                    ["frTemp"] = telemetry.Car[i].frTemp,
                    ["frDeg"] = telemetry.Car[i].frWear,
                    ["rlTemp"] = telemetry.Car[i].rlTemp,
                    ["rlDeg"] = telemetry.Car[i].rlWear,
                    ["rrTemp"] = telemetry.Car[i].rrTemp,
                    ["rrDeg"] = telemetry.Car[i].rrWear,

                    // Car telemetry
                    ["speed"] = telemetry.Car[i].Driver.speed,
                    ["rpm"] = telemetry.Car[i].Driver.rpm,
                    ["gear"] = telemetry.Car[i].Driver.gear,

                    // Components
                    ["engineTemp"] = telemetry.Car[i].engineTemp,
                    ["engineDeg"] = telemetry.Car[i].engineWear,
                    ["gearboxDeg"] = telemetry.Car[i].gearboxWear,
                    ["ersDeg"] = telemetry.Car[i].ersWear,

                    // Energy
                    ["charge"] = telemetry.Car[i].charge,
                    ["fuel"] = telemetry.Car[i].fuel,

                    // Modes
                    ["paceMode"] = F1ManagerPlotter.GetPaceMode(telemetry.Car[i].paceMode) ?? "",
                    ["fuelMode"] = F1ManagerPlotter.GetFuelMode(telemetry.Car[i].fuelMode) ?? "",
                    ["ersMode"] = F1ManagerPlotter.GetERSMode(telemetry.Car[i].ersMode) ?? "",
                    ["drsMode"] = F1ManagerPlotter.GetDRSMode(telemetry.Car[i].Driver.drsMode) ?? "",

                    // Timings
                    ["currentLapTime"] = telemetry.Car[i].Driver.currentLapTime,
                    ["driverBestLap"] = telemetry.Car[i].Driver.driverBestLap,
                    ["lastLapTime"] = telemetry.Car[i].Driver.lastLapTime,
                    ["lastS1Time"] = telemetry.Car[i].Driver.lastS1Time,
                    ["lastS2Time"] = telemetry.Car[i].Driver.lastS2Time,
                    ["lastS3Time"] = telemetry.Car[i].Driver.lastS3Time,

                    // Session info
                    ["bestSessionTime"] = telemetry.Session.bestSessionTime,
                    ["rubber"] = telemetry.Session.rubber,
                    ["airTemp"] = telemetry.Session.Weather.airTemp,
                    ["trackTemp"] = telemetry.Session.Weather.trackTemp,
                    ["weather"] = F1ManagerPlotter.GetWeather(telemetry.Session.Weather.weather) ?? ""
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