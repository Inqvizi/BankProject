using System;
using System.IO;
using System.Text.Json;

namespace BankServer.Services
{
    public class FileLogger
    {
        private readonly string _filePath;

        public FileLogger(string filePath = "logs.json")
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            EnsureFileExists();
        }

        public void LogTransaction(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var logEntry = new
            {
                Timestamp = DateTime.UtcNow,
                Message = message
            };

            string json = JsonSerializer.Serialize(logEntry);

            try
            {
                using (var writer = new StreamWriter(_filePath, append: true))
                {
                    writer.WriteLine(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Logger Error] Could not write to log file: {ex.Message}");
            }
        }
        private void EnsureFileExists()
        {
            try
            {
                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (!File.Exists(_filePath))
                {
                    using (File.Create(_filePath)) { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Logger Error] Could not create log file: {ex.Message}");
            }
        }
    }
}