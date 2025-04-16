using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace F1Manager2024Plugin
{
    public class MmfReader
    {
        public event Action<string> DataReceived;

        private bool _isReading;
        private Task _readingTask;
        private CancellationTokenSource _cts;
        private string _currentFilePath;

        public void StartReading(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                SimHub.Logging.Current.Info("No file path specified");
                DataReceived?.Invoke("ERROR: No file path specified");
                return;
            }

            if (_isReading && filePath == _currentFilePath)
            {
                return; // Already reading this file
            }

            StopReading(); // Stop any existing reading

            _currentFilePath = filePath;
            _cts = new CancellationTokenSource();
            _isReading = true;

            _readingTask = Task.Run(() =>
            {
                while (_isReading && !_cts.IsCancellationRequested)
                {
                    try
                    {
                        using (var fs = new FileStream(_currentFilePath,
                               FileMode.Open,
                               FileAccess.Read,
                               FileShare.ReadWrite))
                        using (var reader = new BinaryReader(fs))
                        {
                            int length = reader.ReadInt32();
                            if (length <= 0 || length > 1024*1024)
                            {
                                Thread.Sleep(1);
                                continue;
                            }

                            byte[] buffer = reader.ReadBytes(length);
                            string json = Encoding.UTF8.GetString(buffer);
                            DataReceived?.Invoke(json);
                        }
                    }

                    catch (DirectoryNotFoundException)
                    {
                        SimHub.Logging.Current.Info($"Directory not found: {_currentFilePath}");
                        DataReceived?.Invoke("ERROR: Directory not found");
                        StopReading();
                    }

                    catch (Exception ex) when (
                        ex is FileNotFoundException ||
                        ex is DirectoryNotFoundException)
                    {
                        SimHub.Logging.Current.Info($"File not found: {_currentFilePath}");
                        Thread.Sleep(1000);
                    }

                    catch (IOException)
                    {
                        Thread.Sleep(1);
                    }

                catch (Exception ex)
                {
                    SimHub.Logging.Current.Error($"Read error: {ex.Message}");
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
            catch (AggregateException) { } // Ignore task cancellation exceptions
            finally
            {
                _cts?.Dispose();
                _cts = null;
                _readingTask = null;
            }
        }
    }
}