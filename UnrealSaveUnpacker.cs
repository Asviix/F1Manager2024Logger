using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using WoteverCommon.WPF.Converters;

namespace F1Manager2024Plugin
{
    /// <summary>
    /// Provides functionality to unpack Unreal Engine save files containing compressed database segments
    /// </summary>
    public static class UnrealSaveUnpacker
    {
        private const string CHUNK1_NAME = "chunk1";
        private const string MAIN_DB_NAME = "main.db";
        private const string BACKUP_DB_NAME = "backup1.db";
        private const string BACKUP_DB2_NAME = "backup2.db";

        // Signature that marks the beginning of the database section
        private static readonly byte[] NONE_NONE_SIGNATURE = new byte[]
        {
            0x00, 0x05, 0x00, 0x00, 0x00, 0x4E, 0x6F, 0x6E, 0x65, // "None" with length prefix
            0x00, 0x05, 0x00, 0x00, 0x00, 0x4E, 0x6F, 0x6E, 0x65, 0x00 // Another "None" with length prefix
        };

        private static string _lastMd5Hash;
        private static DateTime _lastCheckTime = DateTime.MinValue;
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5); // Check every 5 seconds

        private static readonly object _fileCheckLock = new();

        /// <summary>
        /// Unpacks an Unreal save file into its constituent database files
        /// </summary>
        /// <exception cref="InvalidDataException">Thrown when the save file is malformed</exception>
        /// <exception cref="FileNotFoundException">Thrown when the save file is not found</exception>
        public static void UnpackSaveFile()
        {
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string saveFilePath = Path.Combine(basePath, "F1Manager24", "Saved", "SaveGames", "autosave.sav");
            string outputDirectory = Path.Combine(basePath, "F1Manager24", "Saved", "SaveGames", "Unpacked");

            lock (_fileCheckLock)
            {
                if (DateTime.Now - _lastCheckTime < CheckInterval)
                {
                    return;
                }

                _lastCheckTime = DateTime.Now;

                if (!File.Exists(saveFilePath))
                {
                    throw new FileNotFoundException($"Save file not found: {saveFilePath}");
                }

                string currentHash;
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(saveFilePath))
                {
                    currentHash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                }

                if (currentHash == _lastMd5Hash)
                {
                    return; // No changes detected
                }

                _lastMd5Hash = currentHash;

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                byte[] fileBytes = File.ReadAllBytes(saveFilePath);
                int dbSectionOffset = FindDatabaseSectionOffset(fileBytes);

                ExtractChunk1(fileBytes, dbSectionOffset, outputDirectory);
                ExtractDatabases(fileBytes, dbSectionOffset, outputDirectory);

                SaveDataCache.UpdateCache();
            }
        }

        private static int FindDatabaseSectionOffset(byte[] fileBytes)
        {
            int sigPosition = ByteArrayIndexOf(fileBytes, NONE_NONE_SIGNATURE);
            if (sigPosition == -1)
            {
                throw new InvalidDataException("Could not find database section signature in save file");
            }

            // Skip the signature and 4 unknown bytes
            return sigPosition + NONE_NONE_SIGNATURE.Length + 4;
        }

        private static void ExtractChunk1(byte[] fileBytes, int dbSectionOffset, string outputDirectory)
        {
            string chunk1Path = Path.Combine(outputDirectory, CHUNK1_NAME);
            byte[] chunk1Data = new byte[dbSectionOffset];
            Buffer.BlockCopy(fileBytes, 0, chunk1Data, 0, dbSectionOffset);
            File.WriteAllBytes(chunk1Path, chunk1Data);
        }

        private static void ExtractDatabases(byte[] fileBytes, int dbSectionOffset, string outputDirectory)
        {
            int position = dbSectionOffset;

            // Read the compressed data size
            if (position + 4 > fileBytes.Length)
            {
                throw new InvalidDataException("Save file truncated at compressed size field");
            }
            _ = BitConverter.ToInt32(fileBytes, position);
            position += 4;

            // Read the three database sizes
            var databaseSizes = new Dictionary<string, int>
            {
                { Path.Combine(outputDirectory, MAIN_DB_NAME), ReadDatabaseSize(fileBytes, ref position) },
                { Path.Combine(outputDirectory, BACKUP_DB_NAME), ReadDatabaseSize(fileBytes, ref position) },
                { Path.Combine(outputDirectory, BACKUP_DB2_NAME), ReadDatabaseSize(fileBytes, ref position) }
            };

            // Get the compressed data
            byte[] compressedData = new byte[fileBytes.Length - position];
            Buffer.BlockCopy(fileBytes, position, compressedData, 0, compressedData.Length);

            // Decompress the database block
            byte[] decompressedData = DecompressData(compressedData);

            // Write each database file
            int currentPosition = 0;
            foreach (var dbInfo in databaseSizes)
            {
                if (dbInfo.Value <= 0) continue;

                if (currentPosition + dbInfo.Value > decompressedData.Length)
                {
                    throw new InvalidDataException($"Database section corrupted: expected {dbInfo.Value} bytes but only {decompressedData.Length - currentPosition} available");
                }

                byte[] dbData = new byte[dbInfo.Value];
                Buffer.BlockCopy(decompressedData, currentPosition, dbData, 0, dbInfo.Value);
                File.WriteAllBytes(dbInfo.Key, dbData);
                currentPosition += dbInfo.Value;
            }
        }

        private static int ReadDatabaseSize(byte[] fileBytes, ref int position)
        {
            if (position + 4 > fileBytes.Length)
            {
                throw new InvalidDataException("Save file truncated at database size field");
            }
            int size = BitConverter.ToInt32(fileBytes, position);
            position += 4;
            return size;
        }

        private static byte[] DecompressData(byte[] compressedData)
        {
            // ZLib format requires skipping the first 2 bytes (header) for .NET's DeflateStream
            using var outputStream = new MemoryStream();
            // Create a new stream without the first 2 bytes
            using (var compressedStream = new MemoryStream(compressedData, 2, compressedData.Length - 2))
            using (var decompressionStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
            {
                decompressionStream.CopyTo(outputStream);
            }
            return outputStream.ToArray();
        }

        private static int ByteArrayIndexOf(byte[] source, byte[] pattern)
        {
            for (int i = 0; i <= source.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (source[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }
    }

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
            public static List<F1Teams> F1Teams => SaveDataCache.GetCachedValue<List<F1Teams>>("F1Teams");
        }

        public static class Queries
        {
            public const string PointScheme = "SELECT \"CurrentValue\" FROM \"Regulations_Enum_Changes\" WHERE \"Name\" = 'PointScheme'";
            public const string FastestLapPoint = "SELECT \"CurrentValue\" FROM \"Regulations_Enum_Changes\" WHERE \"Name\" = 'FastestLapBonusPoint'";
            public const string PolePositionPoint = "SELECT \"CurrentValue\" FROM \"Regulations_Enum_Changes\" WHERE \"Name\" = 'PolePositionBonusPoint'";
            public const string DoublePointsLastRace = "SELECT \"CurrentValue\" FROM \"Regulations_Enum_Changes\" WHERE \"Name\" = 'DoubleLastRacePoints'";
            public const string CurrentSeason = "SELECT \"CurrentSeason\" FROM \"Player_State\"";
            public const string CurrentRace = "SELECT \"RaceID\" FROM \"Save_Weekend\"";
            public const string driverNameData = "SELECT d.\"StaffID\" as \"Id\", d.\"FirstName\" as \"RawFirstName\", d.\"LastName\" as \"RawLastName\", d.\"DriverCode\" as \"RawDriverCode\", c.\"TeamID\" as \"TeamID\" FROM \"Staff_DriverData_View\" d JOIN \"Staff_Contracts_View\" c ON d.\"StaffID\" = c.\"StaffID\" WHERE c.\"Formula\" = '1' ORDER BY d.\"StaffID\" ASC";
            public const string F1Teams = "SELECT d.\"TeamID\" as \"TeamId\", d.\"TeamNameLocKey\" as \"RawTeamName\", c.\"Colour\" as \"RawColour\" FROM \"Teams\" d JOIN \"Teams_Colours\" c ON d.\"TeamID\" = c.\"TeamID\" WHERE \"Formula\" = '1' ORDER BY \"TeamID\" ASC";

            public static string GetRaceIdOfLastRaceQuery()
            {
                return $"SELECT \"RaceID\" FROM \"Races\" WHERE \"SeasonID\" = '{CachedValues.CurrentSeason}' ORDER BY \"RaceID\" DESC LIMIT 1";
            }
        }

        public static void UpdateCache()
        {
            lock (_cacheLock)
            {
                _cachedValues["PointScheme"] = SaveFileQuery.ExecuteScalar<int>(Queries.PointScheme);
                _cachedValues["FastestLapPoint"] = SaveFileQuery.ExecuteScalar<int>(Queries.FastestLapPoint);
                _cachedValues["PolePositionPoint"] = SaveFileQuery.ExecuteScalar<int>(Queries.PolePositionPoint);
                _cachedValues["DoublePointsLastRace"] = SaveFileQuery.ExecuteScalar<int>(Queries.DoublePointsLastRace);
                _cachedValues["CurrentSeason"] = SaveFileQuery.ExecuteScalar<int>(Queries.CurrentSeason);
                _cachedValues["CurrentRace"] = SaveFileQuery.ExecuteScalar<int>(Queries.CurrentRace);
                _cachedValues["RaceIdOfLastRace"] = SaveFileQuery.ExecuteScalar<int>(Queries.GetRaceIdOfLastRaceQuery());
                _cachedValues["driverNameData"] = SaveFileQuery.ExecuteSql<DriverNameData>(Queries.driverNameData);
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
