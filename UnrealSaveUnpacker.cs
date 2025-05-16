using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace F1Manager2024Plugin
{
    public class UnrealSaveUnpacker
    {
        private const string NoneNoneSignature = "\x00\x05\x00\x00\x00\x4E\x6F\x6E\x65\x00\x05\x00\x00\x00\x4E\x6F\x6E\x65\x00";
        private const string Chunk1Name = "chunk1";
        private const string MainDbName = "main.db";
        private const string BackupDb1Name = "backup1.db";
        private const string BackupDb2Name = "backup2.db";

        public void UnpackSaveFile(string inputFile, string outputDirectory)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            using (var fileStream = new FileStream(inputFile, FileMode.Open))
            using (var reader = new BinaryReader(fileStream))
            {
                // Read entire file into memory (similar to mmap in Python)
                byte[] fileData = reader.ReadBytes((int)fileStream.Length);

                // Find the database section
                int dbSectionOffset = FindDatabaseSection(fileData);
                if (dbSectionOffset == -1)
                {
                    throw new InvalidDataException("Could not find database section in save file");
                }

                // Save the header chunk
                SaveChunk1(outputDirectory, fileData, dbSectionOffset);

                // Process the database section
                ProcessDatabaseSection(outputDirectory, fileData, dbSectionOffset);
            }
        }

        private int FindDatabaseSection(byte[] fileData)
        {
            byte[] signatureBytes = System.Text.Encoding.ASCII.GetBytes(NoneNoneSignature);
            for (int i = 0; i < fileData.Length - signatureBytes.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < signatureBytes.Length; j++)
                {
                    if (fileData[i + j] != signatureBytes[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    return i + signatureBytes.Length + 4; // +4 for unknown bytes
                }
            }
            return -1;
        }

        private void SaveChunk1(string outputDirectory, byte[] fileData, int dbSectionOffset)
        {
            string chunkPath = Path.Combine(outputDirectory, Chunk1Name);
            using (var fs = new FileStream(chunkPath, FileMode.Create))
            {
                fs.Write(fileData, 0, dbSectionOffset);
            }
        }

        private void ProcessDatabaseSection(string outputDirectory, byte[] fileData, int dbSectionOffset)
        {
            int position = dbSectionOffset;

            // Read compressed size
            int compressedSize = BitConverter.ToInt32(fileData, position);
            position += 4;

            // Read database sizes
            int mainDbSize = BitConverter.ToInt32(fileData, position);
            position += 4;
            int backup1DbSize = BitConverter.ToInt32(fileData, position);
            position += 4;
            int backup2DbSize = BitConverter.ToInt32(fileData, position);
            position += 4;

            // Decompress the data
            byte[] compressedData = new byte[compressedSize];
            Array.Copy(fileData, position, compressedData, 0, compressedSize);
            byte[] decompressedData = DecompressZlib(compressedData);

            // Extract databases
            int dataPosition = 0;
            ExtractDatabase(outputDirectory, MainDbName, decompressedData, ref dataPosition, mainDbSize);
            ExtractDatabase(outputDirectory, BackupDb1Name, decompressedData, ref dataPosition, backup1DbSize);
            ExtractDatabase(outputDirectory, BackupDb2Name, decompressedData, ref dataPosition, backup2DbSize);
        }

        private byte[] DecompressZlib(byte[] compressedData)
        {
            return Ionic.Zlib.ZlibStream.UncompressBuffer(compressedData);
        }

        private void ExtractDatabase(string outputDirectory, string dbName,
                                   byte[] decompressedData, ref int position, int size)
        {
            if (size == 0) return;

            string dbPath = Path.Combine(outputDirectory, dbName);
            using (var fs = new FileStream(dbPath, FileMode.Create))
            {
                fs.Write(decompressedData, position, size);
            }
            position += size;
        }
    }

    public class SaveDataAnalyzer
    {
        public void AnalyzeSave(string dbPath)
        {
            string connectionString = $"Data Source={dbPath};Version=3;";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Get all tables
                var tables = GetTables(connection);

                foreach (var table in tables)
                {
                    Console.WriteLine($"Table: {table}");
                    DisplayTableContents(connection, table);
                }
            }
        }

        private List<string> GetTables(SQLiteConnection connection)
        {
            var tables = new List<string>();
            using (var command = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table'", connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    tables.Add(reader.GetString(0));
                }
            }
            return tables;
        }

        private void DisplayTableContents(SQLiteConnection connection, string tableName)
        {
            using (var command = new SQLiteCommand($"PRAGMA table_info({tableName})", connection))
            using (var reader = command.ExecuteReader())
            {
                Console.WriteLine("Columns:");
                while (reader.Read())
                {
                    Console.WriteLine($"- {reader["name"]} ({reader["type"]})");
                }
            }

            using (var command = new SQLiteCommand($"SELECT * FROM {tableName} LIMIT 5", connection))
            using (var reader = command.ExecuteReader())
            {
                Console.WriteLine("First 5 rows:");
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    Console.Write($"{reader.GetName(i)}\t");
                }
                Console.WriteLine();

                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        Console.Write($"{reader[i]}\t");
                    }
                    Console.WriteLine();
                }
            }
        }
    }
}
