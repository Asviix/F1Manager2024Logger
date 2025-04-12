using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace F1Manager2024Plugin
{
    public class mmfReader
    {
        // Define the DataReceived event
        public event Action<string> DataReceived;
        public event Action<string> ErrorOccurred;

        private bool _isReading;
        private Task _readingTask;
        private CancellationTokenSource _cts;
        private readonly string _filePath;

        public mmfReader(string filePath = @"D:\F1M\Logging Script\F1ManagerStandalone\src\F1Manager_Telemetry")
        {
            _filePath = filePath;
        }

        public void StartReading()
        {
            _cts = new CancellationTokenSource();
            _isReading = true;

            _readingTask = Task.Run(() =>
            {
                while (_isReading && !_cts.IsCancellationRequested)
                {
                    try
                    {
                        using (var fs = new FileStream(_filePath,
                               FileMode.Open,
                               FileAccess.Read,
                               FileShare.ReadWrite))
                        using (var reader = new BinaryReader(fs))
                        {
                            // Read length prefix (4 bytes)
                            int length = reader.ReadInt32();

                            if (length <= 0 || length > 65532)
                            {
                                Thread.Sleep(1);
                                continue;
                            }

                            // Read JSON data
                            byte[] buffer = reader.ReadBytes(length);
                            string json = Encoding.UTF8.GetString(buffer);

                            DataReceived?.Invoke(json);
                        }
                    }
                    catch (IOException ioEx)
                    {
                        // File being written to or not available yet
                        Thread.Sleep(1);
                    }
                    catch (Exception ex)
                    {
                        ErrorOccurred?.Invoke($"Read error: {ex.Message}");
                        Thread.Sleep(100);
                    }
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
            finally
            {
                _cts?.Dispose();
            }
        }
    }
}