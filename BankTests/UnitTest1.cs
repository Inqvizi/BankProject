using BankClient.Services;
using BankServer.Data;
using BankServer.Services;
using BankShared.Constants;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Bank.Tests
{
    // ================= Repository Tests =================
    public class RepositoryTests
    {
        [Fact]
        public void SeedData_ShouldCreateMyAccount_WhenRepositoryStarts()
        {
            var repository = new BankRepository();

            var account = repository.GetByNumber("1111");

            Assert.NotNull(account);
            Assert.Equal(1000.00m, account.Balance);
        }

        [Fact]
        public void GetByNumber_ShouldReturnNull_ForUnknownAccount()
        {
            var repository = new BankRepository();

            var account = repository.GetByNumber("99999999");

            Assert.Null(account);
        }
    }

    // ================= FileLogger JSON Append Tests =================
    public class FileLoggerAppendTests
    {
        private class LogEntry
        {
            public string Message { get; set; } = string.Empty;
        }

        [Fact]
        public void LogTransaction_ShouldCreateFile_IfNotExists()
        {
            var path = "test_logs/logs1.json";
            if (File.Exists(path)) File.Delete(path);

            var logger = new FileLogger(path);
            logger.LogTransaction("Test message");

            Assert.True(File.Exists(path));

            var lines = File.ReadAllLines(path);
            Assert.Single(lines);

            var entry = JsonSerializer.Deserialize<LogEntry>(lines[0]);
            Assert.Equal("Test message", entry.Message);
        }

        [Fact]
        public void LogTransaction_ShouldAppendJsonLines()
        {
            var path = "test_logs/logs2.json";
            if (File.Exists(path)) File.Delete(path);

            var logger = new FileLogger(path);
            logger.LogTransaction("First");
            logger.LogTransaction("Second");

            var lines = File.ReadAllLines(path);

            Assert.Equal(2, lines.Length);

            var first = JsonSerializer.Deserialize<LogEntry>(lines[0]);
            var second = JsonSerializer.Deserialize<LogEntry>(lines[1]);

            Assert.Equal("First", first.Message);
            Assert.Equal("Second", second.Message);
        }

        [Fact]
        public void LogTransaction_ShouldThrowIfMessageNull()
        {
            var logger = new FileLogger("test_logs/logs_null.json");
            Assert.Throws<ArgumentNullException>(() => logger.LogTransaction(null));
        }

        [Fact]
        public void Constructor_ShouldThrowIfPathNull()
        {
            Assert.Throws<ArgumentNullException>(() => new FileLogger(null));
        }

    }

    public class BankClientServiceTests
    {
        [Fact]
        public void SendRequest_ShouldWriteJsonToSharedMemory()
        {
            var service = new BankClientService();
            var testData = new { Command = "Test", Amount = 123.45 };

            using (var serverMmf = MemoryMappedFile.CreateNew(
                AppConstants.MemoryMappedFileName,
                AppConstants.MemoryBufferSize))
            {
                service.SendRequest(testData);

                using (var stream = serverMmf.CreateViewStream())
                {
                    byte[] buffer = new byte[AppConstants.MemoryBufferSize];
                    stream.Read(buffer, 0, buffer.Length);

                    string jsonResult = Encoding.UTF8.GetString(buffer).TrimEnd('\0');

                    var expectedJson = JsonSerializer.Serialize(testData);
                    Assert.Equal(expectedJson, jsonResult);
                }
            }
        }
    }
}
