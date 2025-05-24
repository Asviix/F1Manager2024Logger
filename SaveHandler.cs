using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using F1Manager2024Plugin;

public class UESaveTool
{
    private const string MAIN_DB_NAME = "main.db";
    private const string BACKUP_DB_NAME = "backup1.db";
    private const string BACKUP_DB2_NAME = "backup2.db";
    private const string CHUNK1_NAME = "chunk1";

    private static string _lastMd5Hash;
    private static DateTime _lastCheckTime = DateTime.MinValue;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);
    private static readonly object _fileCheckLock = new();

    private readonly BinaryReader reader;
    private readonly MemoryStream stream;
    private readonly string _outputDirectory;

    public UESaveTool(string outputDirectory = null)
    {
        _outputDirectory = outputDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "F1Manager24", "Saved", "SaveGames", "Unpacked");
    }

    public void UnpackSaveFile(string saveFilePath = null)
    {
        saveFilePath = saveFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "F1Manager24", "Saved", "SaveGames", "autosave.sav");

        lock (_fileCheckLock)
        {
            try
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

                if (!Directory.Exists(_outputDirectory))
                {
                    Directory.CreateDirectory(_outputDirectory);
                }

                byte[] fileBytes = File.ReadAllBytes(saveFilePath);
                int dbSectionOffset = FindDatabaseSectionOffset(fileBytes);

                ExtractChunk1(fileBytes, dbSectionOffset);
                ExtractDatabases(fileBytes, dbSectionOffset);

                SaveDataCache.UpdateCache();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to unpack save file", ex);
            }
        }
    }

    private int FindDatabaseSectionOffset(byte[] fileBytes)
    {
        // Define the None...None signature to search for
        byte[] noneSignature = new byte[] {
        0x00, 0x05, 0x00, 0x00, 0x00, 0x4E, 0x6F, 0x6E, 0x65,  // None
        0x00, 0x05, 0x00, 0x00, 0x00, 0x4E, 0x6F, 0x6E, 0x65   // None
        };

        // Find the last occurrence of the signature
        int lastOffset = -1;
        for (int i = 0; i <= fileBytes.Length - noneSignature.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < noneSignature.Length; j++)
            {
                if (fileBytes[i + j] != noneSignature[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
            {
                lastOffset = i;
            }
        }

        if (lastOffset == -1)
        {
            throw new InvalidDataException("Could not find None...None signature in save file");
        }

        // Skip the signature and 5 unknown bytes
        return lastOffset + noneSignature.Length + 5;
    }

    private void ExtractChunk1(byte[] fileBytes, int dbSectionOffset)
    {
        string chunk1Path = Path.Combine(_outputDirectory, CHUNK1_NAME);
        byte[] chunk1Data = new byte[dbSectionOffset];
        Buffer.BlockCopy(fileBytes, 0, chunk1Data, 0, dbSectionOffset);
        File.WriteAllBytes(chunk1Path, chunk1Data);
    }

    private void ExtractDatabases(byte[] fileBytes, int dbSectionOffset)
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
            { Path.Combine(_outputDirectory, MAIN_DB_NAME), ReadDatabaseSize(fileBytes, ref position) },
            { Path.Combine(_outputDirectory, BACKUP_DB_NAME), ReadDatabaseSize(fileBytes, ref position) },
            { Path.Combine(_outputDirectory, BACKUP_DB2_NAME), ReadDatabaseSize(fileBytes, ref position) }
        };

        // Get the compressed data
        byte[] compressedData = new byte[fileBytes.Length - position];
        Buffer.BlockCopy(fileBytes, position, compressedData, 0, compressedData.Length);

        // Decompress the database block with multiple attempts if needed
        byte[] decompressedData = DecompressDataWithFallback(compressedData);

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

    private byte[] DecompressDataWithFallback(byte[] compressedData)
    {
        try
        {
            // First try with ZLib header skip (standard case)
            return DecompressData(compressedData, skipHeader: true);
        }
        catch
        {
            try
            {
                // If that fails, try without skipping header (some custom saves)
                return DecompressData(compressedData, skipHeader: false);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Failed to decompress data with both methods. The save file might use an unsupported compression format.", ex);
            }
        }
    }

    private byte[] DecompressData(byte[] compressedData, bool skipHeader)
    {
        using var outputStream = new MemoryStream();
        int offset = skipHeader ? 2 : 0;
        int length = compressedData.Length - offset;

        using (var compressedStream = new MemoryStream(compressedData, offset, length))
        using (var decompressionStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
        {
            decompressionStream.CopyTo(outputStream);
        }
        return outputStream.ToArray();
    }

    private int ReadDatabaseSize(byte[] fileBytes, ref int position)
    {
        if (position + 4 > fileBytes.Length)
        {
            throw new InvalidDataException("Save file truncated at database size field");
        }
        int size = BitConverter.ToInt32(fileBytes, position);
        position += 4;
        return size;
    }

    private Dictionary<string, object> ReadProperties()
    {
        var properties = new Dictionary<string, object>();

        while (true)
        {
            string name = ReadString();
            if (string.IsNullOrEmpty(name))
            {
                break;
            }

            string type = ReadString();
            int size = reader.ReadInt32();
            long startPos = stream.Position;

            properties[name] = ReadPropertyValue(type, size);

            // Ensure we read exactly 'size' bytes
            long bytesRead = stream.Position - startPos;
            if (bytesRead < size)
            {
                stream.Seek(size - bytesRead, SeekOrigin.Current);
            }
        }

        return properties;
    }

    private object ReadPropertyValue(string type, int size)
    {
        switch (type)
        {
            case "IntProperty":
                return reader.ReadInt32();
            case "FloatProperty":
                return reader.ReadSingle();
            case "BoolProperty":
                return reader.ReadBoolean();
            case "StrProperty":
            case "NameProperty":
                return ReadString();
            case "ArrayProperty":
                return ReadArrayProperty();
            case "MapProperty":
                return ReadMapProperty();
            case "StructProperty":
                return ReadStructProperty();
            default:
                // Skip unknown properties
                reader.ReadBytes(size);
                return null;
        }
    }

    private List<object> ReadArrayProperty()
    {
        string arrayType = ReadString();
        int count = reader.ReadInt32();
        var array = new List<object>(count);

        for (int i = 0; i < count; i++)
        {
            array.Add(ReadPropertyValue(arrayType, 0)); // Size not used for array elements
        }

        return array;
    }

    private Dictionary<object, object> ReadMapProperty()
    {
        string keyType = ReadString();
        string valueType = ReadString();
        int count = reader.ReadInt32();
        var map = new Dictionary<object, object>(count);

        for (int i = 0; i < count; i++)
        {
            object key = ReadPropertyValue(keyType, 0);
            object value = ReadPropertyValue(valueType, 0);
            map[key] = value;
        }

        return map;
    }

    private Dictionary<string, object> ReadStructProperty()
    {
        string structType = ReadString();
        byte[] id = reader.ReadBytes(16); // Struct GUID
        var properties = ReadProperties();

        return new Dictionary<string, object>
        {
            ["StructType"] = structType,
            ["Id"] = new Guid(id),
            ["Properties"] = properties
        };
    }

    private string ReadString()
    {
        int length = reader.ReadInt32();
        if (length == 0)
        {
            return string.Empty;
        }

        // Negative length indicates Unicode string
        if (length < 0)
        {
            length = -length;
            byte[] bytes = reader.ReadBytes(length * 2);
            return Encoding.Unicode.GetString(bytes, 0, bytes.Length - 2); // Remove null terminator
        }
        else
        {
            byte[] bytes = reader.ReadBytes(length);
            return Encoding.ASCII.GetString(bytes, 0, bytes.Length - 1); // Remove null terminator
        }
    }
}