using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace BankServer.Services
{
    public class FileLogger
    {
        private readonly string _logPath;

        public FileLogger(string logPath = "logs.json")
        {
            _logPath = logPath;

            if (!File.Exists(_logPath))
            {
                File.WriteAllText(_logPath, "[]", Encoding.UTF8);
            }
        }

        public void LogTransaction(string message)
        {
            var logEntry = new
            {
                Timestamp = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"),
                Message = message
            };

            string jsonEntry = JsonSerializer.Serialize(logEntry);

            using (var stream = new FileStream(_logPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                if (stream.Length <= 2)
                {
                    // порожній масив []
                    stream.Seek(1, SeekOrigin.Begin); // після [
                    byte[] bytes = Encoding.UTF8.GetBytes(jsonEntry + "]");
                    stream.Write(bytes, 0, bytes.Length);
                }
                else
                {
                    // масив має записи, вставляємо кому перед новим елементом
                    stream.Seek(-1, SeekOrigin.End); // перед ]
                    byte[] bytes = Encoding.UTF8.GetBytes("," + jsonEntry + "]");
                    stream.Write(bytes, 0, bytes.Length);
                }
            }
        }
    }
}
