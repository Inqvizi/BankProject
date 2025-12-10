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

            var json = JsonSerializer.Serialize(logEntry);

            // Дозапис рядка у файл
            using (var writer = new StreamWriter(_filePath, append: true))
            {
                writer.WriteLine(json);
            }
        }

        private void EnsureFileExists()
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(_filePath))
                File.Create(_filePath).Dispose();
        }
    }
}
