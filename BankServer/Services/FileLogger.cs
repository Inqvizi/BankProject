using System;
using System.IO;
using System.Text;

namespace BankServer.Services
{
    public class FileLogger
    {
        private readonly string _logPath;

        public FileLogger(string logPath = "logs.txt")
        {
            _logPath = logPath;
        }

        public void LogTransaction(string message)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}";
            File.AppendAllText(_logPath, logEntry + Environment.NewLine, Encoding.UTF8);
        }
    }
}
