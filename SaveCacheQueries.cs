using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;

namespace F1Manager2024Plugin
{
    public static class SaveFileQuery
    {
        /// <summary>
        /// Executes a SQL query against the unpacked save file database
        /// </summary>
        /// <typeparam name="T">Type to return (use dynamic for unknown structures)</typeparam>
        /// <param name="sqlCommand">SQL command to execute</param>
        /// <param name="parameters">Optional parameters</param>
        /// <param name="logger">Optional logging action</param>
        /// <returns>List of results in specified type</returns>

        public static System.Collections.Generic.List<T> ExecuteSql<T>(string sqlCommand, object parameters = null, Action<string> logger = null)
        {
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbPath = Path.Combine(basePath, "F1Manager24", "Saved", "SaveGames", "Unpacked", "main.db");

            if (!File.Exists(dbPath))
            {
                logger?.Invoke($"Database not found at {dbPath}");
                throw new FileNotFoundException("Unpacked database not found.");
            }

            try
            {
                using var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
                connection.Open();
                logger?.Invoke($"Executing SQL: {sqlCommand}");

                var result = connection.Query<T>(sqlCommand, parameters).ToList();
                logger?.Invoke($"Returned {result.Count} rows of type {typeof(T).Name}");

                return result;
            }
            catch (Exception ex)
            {
                logger?.Invoke($"SQL execution failed: {ex.Message}");
                throw new InvalidOperationException("SQL command execution failed", ex);
            }
        }

        /// <summary>
        /// Executes a scalar SQL query against the unpacked save file database
        /// </summary>
        /// <typeparam name="T">Type to return</typeparam>
        /// <param name="sqlCommand">SQL command to execute</param>
        /// <param name="parameters">Optional parameters</param>
        /// <param name="logger">Optional logging action</param>
        /// <returns>Single result value</returns>
        public static T ExecuteScalar<T>(string sqlCommand, object parameters = null, Action<string> logger = null)
        {
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbPath = Path.Combine(basePath, "F1Manager24", "Saved", "SaveGames", "Unpacked", "main.db");

            if (!File.Exists(dbPath))
            {
                logger?.Invoke($"Database not found at {dbPath}");
                throw new FileNotFoundException("Unpacked database not found - run UnpackSaveFile() first");
            }

            try
            {
                using var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
                connection.Open();
                logger?.Invoke($"Executing SQL (scalar): {sqlCommand}");

                var result = connection.ExecuteScalar<T>(sqlCommand, parameters);
                logger?.Invoke($"Returned scalar value of type {typeof(T).Name}");

                if (result == null && !default(T)!.Equals(null))
                {
                    throw new InvalidOperationException("Query returned null for a non-nullable type.");
                }

                return result!;
            }
            catch (Exception ex)
            {
                logger?.Invoke($"SQL execution failed: {ex.Message}");
                throw new InvalidOperationException("SQL command execution failed", ex);
            }
        }
    }

    public class SaveDataCache
    {
        private static readonly object _cacheLock = new();
        private static readonly ConcurrentDictionary<string, object> _cachedValues = new();

        public class DriverNameData
        {
            public int Id { get; set; }
            public string RawFirstName { get; set; }
            public string RawLastName { get; set; }
            public string RawDriverCode { get; set; }
            public int TeamID { get; set; }
            public string FirstName => ExtractName(RawFirstName);
            public string LastName => ExtractName(RawLastName);
            public string DriverCode => ExtractName(RawDriverCode);

            private static string ExtractName(string resourceString)
            {
                if (string.IsNullOrEmpty(resourceString))
                {
                    return resourceString;
                }

                if (!resourceString.StartsWith("[") || !resourceString.EndsWith("]"))
                {
                    return resourceString;
                }

                var cleanString = resourceString.Trim('[', ']');
                var parts = cleanString.Split('_');
                var lastPart = parts.LastOrDefault() ?? cleanString;

                var result = Regex.Replace(lastPart, @"\d+$", "");
                return result;
            }
        }

        public class TyreSetData
        {
            public int CarID { get; set; }
            public int TyreSetID { get; set; }
            public int WeekendTyreType { get; set; }
        }

        public class F1Teams
        {
            public int TeamId { get; set; }
            public string RawTeamName { get; set; }
            public string RawColour { get; set; }
            public string TeamColour => ConvertRawColour(RawColour);
            public string TeamName => ExtractTeamName(RawTeamName);

            private static string ConvertRawColour(string decimalColor)
            {
                if (!long.TryParse(decimalColor, out long argbValue))
                {
                    return "#123456"; // Default black if parsing fails
                }

                // Convert to 6-digit hex RGB (skip alpha channel)
                return "#" + (argbValue & 0xFFFFFF).ToString("X6");
            }

            private static string ExtractTeamName(string resourceString)
            {
                if (string.IsNullOrEmpty(resourceString))
                    return resourceString;

                // Handle custom team format: [STRING_LITERAL:Value=|Peugeot Sport|]
                if (resourceString.StartsWith("[STRING_LITERAL:Value=|") && resourceString.EndsWith("|]"))
                {
                    return resourceString
                        .Substring("[STRING_LITERAL:Value=|".Length)
                        .TrimEnd('|', ']');
                }

                // Handle standard resource format: [TeamName_F1_MercedesAMGPetronasF1]
                if (resourceString.StartsWith("[") && resourceString.EndsWith("]"))
                {
                    var cleanString = resourceString.Trim('[', ']');
                    var parts = cleanString.Split('_');
                    var namePart = parts.LastOrDefault() ?? cleanString;

                    // Special cases for known team names
                    var knownTeams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["MercedesAMGPetronasF1"] = "Mercedes AMG Petronas F1",
                        ["McLaren"] = "McLaren"
                    };

                    if (knownTeams.TryGetValue(namePart, out var formattedName))
                        return formattedName;

                    // Generic solution for unknown team names
                    return AddSpacesToTeamName(namePart);
                }

                // Return as-is if not a special format
                return resourceString;
            }

            private static string AddSpacesToTeamName(string name)
            {
                if (string.IsNullOrEmpty(name))
                    return name;

                var sb = new StringBuilder();
                for (int i = 0; i < name.Length; i++)
                {
                    // Skip adding space for first character
                    if (i > 0 && char.IsUpper(name[i]))
                    {
                        // Don't add space if previous character was uppercase (like AMG)
                        // Unless next character is lowercase (like "F1Team" -> "F1 Team")
                        bool shouldAddSpace = !char.IsUpper(name[i - 1]) ||
                                            (i < name.Length - 1 && char.IsLower(name[i + 1]));

                        if (shouldAddSpace)
                            sb.Append(' ');
                    }
                    sb.Append(name[i]);
                }

                // Special handling for "F1" at the end
                return sb.ToString()
                    .Replace("F 1", "F1")  // Fix cases where F1 was split
                    .Replace(" F1", " F1"); // Ensure consistent spacing
            }
        }

        public static class CachedValues
        {
            public static int PointScheme => SaveDataCache.GetCachedValue<int>("PointScheme");
            public static int FastestLapPoint => SaveDataCache.GetCachedValue<int>("FastestLapPoint");
            public static int PolePositionPoint => SaveDataCache.GetCachedValue<int>("PolePositionPoint");
            public static int DoublePointsLastRace => SaveDataCache.GetCachedValue<int>("DoublePointsLastRace");
            public static int CurrentSeason => SaveDataCache.GetCachedValue<int>("CurrentSeason");
            public static int CurrentRace => SaveDataCache.GetCachedValue<int>("CurrentRace");
            public static int RaceIdOfLastRace => SaveDataCache.GetCachedValue<int>("RaceIdOfLastRace");
            public static List<DriverNameData> DriverNameData => SaveDataCache.GetCachedValue<List<DriverNameData>>("driverNameData");
            public static List<TyreSetData> TyreSetData => SaveDataCache.GetCachedValue<List<TyreSetData>>("TyreSetData");
            public static List<F1Teams> F1Teams => SaveDataCache.GetCachedValue<List<F1Teams>>("F1Teams");
        }

        public static class Queries
        {
            public const string PointScheme = @"
                SELECT ""CurrentValue"" 
                FROM ""Regulations_Enum_Changes"" 
                WHERE ""Name"" = 'PointScheme'";

            public const string FastestLapPoint = @"
                SELECT ""CurrentValue"" 
                FROM ""Regulations_Enum_Changes"" 
                WHERE ""Name"" = 'FastestLapBonusPoint'";

            public const string PolePositionPoint = @"
                SELECT ""CurrentValue"" 
                FROM ""Regulations_Enum_Changes"" 
                WHERE ""Name"" = 'PolePositionBonusPoint'";

            public const string DoublePointsLastRace = @"
                SELECT ""CurrentValue"" 
                FROM ""Regulations_Enum_Changes"" 
                WHERE ""Name"" = 'DoubleLastRacePoints'";

            public const string CurrentSeason = @"
                SELECT ""CurrentSeason"" 
                FROM ""Player_State""";

            public const string CurrentRace = @"
                SELECT ""RaceID"" 
                FROM ""Save_Weekend""";

            public const string DriverData = @"
                SELECT 
                    driver.""StaffID"" as ""Id"", 
                    driver.""FirstName"" as ""RawFirstName"", 
                    driver.""LastName"" as ""RawLastName"", 
                    driver.""DriverCode"" as ""RawDriverCode"", 
                    team.""TeamID"" as ""TeamID""
                FROM ""Staff_DriverData_View"" driver 
                JOIN ""Staff_Contracts_View"" team ON driver.""StaffID"" = team.""StaffID""
                WHERE team.""Formula"" = '1' 
                ORDER BY driver.""StaffID"" ASC";

            public const string TyreSetData = @"
                SELECT
                    tyre.""CarID"",
                    tyre.""TyreSetID"",
                    tyre.""WeekendTyreType""
                FROM ""Save_CarTyreAllocation"" tyre
                ORDER BY ""CarID"" ASC";

            public const string F1Teams = @"
                SELECT 
                    team.""TeamID"" as ""TeamId"", 
                    team.""TeamNameLocKey"" as ""RawTeamName"", 
                    colour.""Colour"" as ""RawColour"" 
                FROM ""Teams"" team
                JOIN ""Teams_Colours"" colour ON team.""TeamID"" = colour.""TeamID"" 
                WHERE team.""Formula"" = '1' 
                ORDER BY ""TeamID"" ASC";

            public static string GetRaceIdOfLastRaceQuery()
            {
                return $@"
                    SELECT ""RaceID"" 
                    FROM ""Races"" 
                    WHERE ""SeasonID"" = '{CachedValues.CurrentSeason}' 
                    ORDER BY ""RaceID"" DESC 
                    LIMIT 1";
            }
        }

        public static void UpdateCache()
        {
            var Query = new UESaveTool();
            Query.UnpackSaveFile();

            lock (_cacheLock)
            {
                _cachedValues["PointScheme"] = SaveFileQuery.ExecuteScalar<int>(Queries.PointScheme);
                _cachedValues["FastestLapPoint"] = SaveFileQuery.ExecuteScalar<int>(Queries.FastestLapPoint);
                _cachedValues["PolePositionPoint"] = SaveFileQuery.ExecuteScalar<int>(Queries.PolePositionPoint);
                _cachedValues["DoublePointsLastRace"] = SaveFileQuery.ExecuteScalar<int>(Queries.DoublePointsLastRace);
                _cachedValues["CurrentSeason"] = SaveFileQuery.ExecuteScalar<int>(Queries.CurrentSeason);
                _cachedValues["CurrentRace"] = SaveFileQuery.ExecuteScalar<int>(Queries.CurrentRace);
                _cachedValues["RaceIdOfLastRace"] = SaveFileQuery.ExecuteScalar<int>(Queries.GetRaceIdOfLastRaceQuery());
                _cachedValues["driverNameData"] = SaveFileQuery.ExecuteSql<DriverNameData>(Queries.DriverData);
                _cachedValues["TyreSetData"] = SaveFileQuery.ExecuteSql<TyreSetData>(Queries.TyreSetData);
                _cachedValues["F1Teams"] = SaveFileQuery.ExecuteSql<F1Teams>(Queries.F1Teams);
            }
        }

        public static T GetCachedValue<T>(string key, T defaultValue = default)
        {
            if (_cachedValues.TryGetValue(key, out var value))
            {
                return (T)value;
            }

            if (defaultValue == null && !default(T)!.Equals(null))
            {
                throw new InvalidOperationException($"No cached value found for key '{key}' and no default value provided.");
            }

            return defaultValue!;
        }
    }
}
