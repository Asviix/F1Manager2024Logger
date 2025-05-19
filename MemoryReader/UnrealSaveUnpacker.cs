using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using Dapper;

namespace MemoryReader
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

        private static string? _lastMd5Hash;
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
            int compressedSize = BitConverter.ToInt32(fileBytes, position);
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
            using (var outputStream = new MemoryStream())
            {
                // Create a new stream without the first 2 bytes
                using (var compressedStream = new MemoryStream(compressedData, 2, compressedData.Length - 2))
                using (var decompressionStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
                {
                    decompressionStream.CopyTo(outputStream);
                }
                return outputStream.ToArray();
            }
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

        public static System.Collections.Generic.List<T> ExecuteSql<T>(string sqlCommand, object? parameters = null, Action<string>? logger = null)
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
                using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    connection.Open();
                    logger?.Invoke($"Executing SQL: {sqlCommand}");

                    var result = connection.Query<T>(sqlCommand, parameters).ToList();
                    logger?.Invoke($"Returned {result.Count} rows of type {typeof(T).Name}");

                    return result;
                }
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
        public static T ExecuteScalar<T>(string sqlCommand, object? parameters = null, Action<string>? logger = null)
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
                using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    connection.Open();
                    logger?.Invoke($"Executing SQL (scalar): {sqlCommand}");

                    var result = connection.ExecuteScalar<T>(sqlCommand, parameters);
                    logger?.Invoke($"Returned scalar value of type {typeof(T).Name}");

                    return result;
                }
            }
            catch (Exception ex)
            {
                logger?.Invoke($"SQL execution failed: {ex.Message}");
                throw new InvalidOperationException("SQL command execution failed", ex);
            }
        }
    }
}
