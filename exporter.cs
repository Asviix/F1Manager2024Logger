using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using Newtonsoft.Json.Linq;
using SimHub.Plugins;

namespace F1Manager2024Plugin
{
    public class Exporter
    {
        private readonly Dictionary<string, string> _driverFilePaths = new();
        private readonly Dictionary<string, bool> _headersWritten = new();

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

        public int CarsOnGrid = 22;

        // Exports Data to CSV Files depending on the chosen settings.
        public void ExportData(string carName, Telemetry telemetry, int i, F1Manager2024PluginSettings Settings, string _lastRecordedData)
        {
            if (!Settings.ExporterEnabled || !Settings.TrackedDrivers.Contains(carName)) return; // Return if Exporter isn't Enabled of car isn't Tracked.
            try
            {
                string trackName = TelemetryHelpers.GetTrackName(telemetry.Session.trackId);
                string sessionType = TelemetryHelpers.GetSessionType(telemetry.Session.sessionType);

                string basePath = Settings.ExporterPath ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "F1ManagerTelemetry");

                string sessionFolder = Path.Combine(basePath, "exported_data", $"{trackName} {sessionType}");
                string carFolder = Path.Combine(sessionFolder, String.Join(" ", TelemetryHelpers.GetDriverFirstName(telemetry.Car[i].Driver.driverId), TelemetryHelpers.GetDriverLastName(telemetry.Car[i].Driver.driverId)));

                // Set the number of cars on the grid.
                if (telemetry.Car[i].Driver.rpm == 0)
                {
                    CarsOnGrid = telemetry.Car.Count(c => c.Driver.rpm > 0);
                }
                else
                {
                    CarsOnGrid = 22;
                }

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

                var lastRecordedData = JObject.Parse(_lastRecordedData);


                var telemetryData = new Dictionary<string, object>
                {
                    // Session data
                    ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    ["trackName"] = TelemetryHelpers.GetTrackName(telemetry.Session.trackId) ?? "",
                    ["sessionType"] = TelemetryHelpers.GetSessionType(telemetry.Session.sessionType) ?? "",
                    ["timeElapsed"] = telemetry.Session.timeElapsed,
                    ["Laps/Time Remaining"] = TelemetryHelpers.GetSessionRemaining(telemetry, CarsOnGrid).Mixed,

                    // Driver info
                    ["driverNumber"] = telemetry.Car[i].Driver.driverNumber,
                    ["driverFirstName"] = TelemetryHelpers.GetDriverFirstName(telemetry.Car[i].Driver.driverId) ?? "",
                    ["driverLastName"] = TelemetryHelpers.GetDriverLastName(telemetry.Car[i].Driver.driverId) ?? "",
                    ["teamName"] = TelemetryHelpers.GetTeamName(telemetry.Car[i].Driver.teamId, Settings) ?? "",
                    ["pitstopStatus"] = TelemetryHelpers.GetPitStopStatus(telemetry.Car[i].pitStopStatus) ?? "",
                    ["currentLap"] = telemetry.Car[i].currentLap + 1, // Adjust for index
                    ["turnNumber"] = telemetry.Car[i].Driver.turnNumber,
                    ["distanceTravelled"] = telemetry.Car[i].Driver.distanceTravelled,
                    ["position"] = telemetry.Car[i].Driver.position + 1, // Adjust for 0-based index
                    ["gapToLeader"] = TelemetryHelpers.GetGapLeader(telemetry, telemetry.Car[i].Driver.position, i, carNames),
                    ["carInFront"] = TelemetryHelpers.GetNameOfCarAhead(telemetry.Car[i].Driver.position, i, carNames),
                    ["gapInFront"] = TelemetryHelpers.GetGapInFront(telemetry, telemetry.Car[i].Driver.position, i, carNames),
                    ["carBehind"] = TelemetryHelpers.GetNameOfCarBehind(telemetry.Car[i].Driver.position, i, carNames, CarsOnGrid),
                    ["gapBehind"] = TelemetryHelpers.GetGapBehind(telemetry, telemetry.Car[i].Driver.position, i, carNames, CarsOnGrid),

                    // Tyres
                    ["compound"] = TelemetryHelpers.GetTireCompound(telemetry.Car[i].tireCompound) ?? "",
                    ["tire_age"] = (telemetry.Car[i].currentLap + 1) - (int)lastRecordedData["LastTireChangeLap"],
                    ["flSurfaceTemp"] = telemetry.Car[i].flSurfaceTemp,
                    ["flTemp"] = telemetry.Car[i].flTemp,
                    ["flBrakeTemp"] = telemetry.Car[i].flBrakeTemp,
                    ["frSurfaceTemp"] = telemetry.Car[i].frSurfaceTemp,
                    ["frTemp"] = telemetry.Car[i].frTemp,
                    ["frBrakeTemp"] = telemetry.Car[i].frBrakeTemp,
                    ["rlSurfaceTemp"] = telemetry.Car[i].rlSurfaceTemp,
                    ["rlTemp"] = telemetry.Car[i].rlTemp,
                    ["rlBrakeTemp"] = telemetry.Car[i].rlBrakeTemp,
                    ["rrSurfaceTemp"] = telemetry.Car[i].rrSurfaceTemp,
                    ["rrTemp"] = telemetry.Car[i].rrTemp,
                    ["rrBrakeTemp"] = telemetry.Car[i].rrBrakeTemp,
                    ["flDeg"] = telemetry.Car[i].flWear,
                    ["frDeg"] = telemetry.Car[i].frWear,
                    ["rlDeg"] = telemetry.Car[i].rlWear,
                    ["rrDeg"] = telemetry.Car[i].rrWear,

                    // Car telemetry
                    ["speed"] = telemetry.Car[i].Driver.speed,
                    ["SpeedST"] = lastRecordedData["SpeedST"],
                    ["rpm"] = telemetry.Car[i].Driver.rpm,
                    ["gear"] = telemetry.Car[i].Driver.gear,

                    // Components
                    ["engineTemp"] = telemetry.Car[i].engineTemp,
                    ["engineDeg"] = telemetry.Car[i].engineWear,
                    ["gearboxDeg"] = telemetry.Car[i].gearboxWear,
                    ["ersDeg"] = telemetry.Car[i].ersWear,

                    // Energy
                    ["charge"] = telemetry.Car[i].charge,
                    ["energyHarvested"] = telemetry.Car[i].energyHarvested,
                    ["energySpent"] = telemetry.Car[i].energySpent,
                    ["fuel"] = telemetry.Car[i].fuel,
                    ["fuelDelta"] = telemetry.Car[i].fuelDelta,

                    // Modes
                    ["paceMode"] = TelemetryHelpers.GetPaceMode(telemetry.Car[i].paceMode) ?? "",
                    ["fuelMode"] = TelemetryHelpers.GetFuelMode(telemetry.Car[i].fuelMode) ?? "",
                    ["ersMode"] = TelemetryHelpers.GetERSMode(telemetry.Car[i].ersMode) ?? "",
                    ["drsMode"] = TelemetryHelpers.GetDRSMode(telemetry.Car[i].Driver.drsMode) ?? "",

                    // Timings
                    ["currentLapTime"] = telemetry.Car[i].Driver.currentLapTime,
                    ["driverBestLap"] = telemetry.Car[i].Driver.driverBestLap,
                    ["lastLapTime"] = telemetry.Car[i].Driver.lastLapTime,
                    ["lastS1Time"] = lastRecordedData["S1Time"],
                    ["bestS1Time"] = lastRecordedData["BestS1Time"],
                    ["lastS2Time"] = lastRecordedData["S2Time"],
                    ["bestS2Time"] = lastRecordedData["BestS2Time"],
                    ["lastS3Time"] = lastRecordedData["S3Time"],
                    ["bestS3Time"] = lastRecordedData["BestS3Time"],

                    // Session info
                    ["bestSessionTime"] = telemetry.Session.bestSessionTime,
                    ["rubber"] = telemetry.Session.rubber,
                    ["airTemp"] = telemetry.Session.Weather.airTemp,
                    ["trackTemp"] = telemetry.Session.Weather.trackTemp,
                    ["weather"] = TelemetryHelpers.GetWeather(telemetry.Session.Weather.weather) ?? ""
                };

                // Write to CSV
                using var writer = new StreamWriter(filePath, true);
                if (!headersWritten)
                {
                    // Write headers in the specified order
                    writer.WriteLine(string.Join(",", telemetryData.Keys));
                    _headersWritten[carName] = true;
                }

                // Write values in the same order as headers
                writer.WriteLine(string.Join(",", telemetryData.Values.Select(v => EscapeCsvValue(v?.ToString()))));
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