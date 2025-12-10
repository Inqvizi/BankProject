using System;
using System.IO;
using System.Text.Json;
using Xunit;
using BankServer.Data;
using BankServer.Services;

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
}
