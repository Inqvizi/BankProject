using BankClient.Services;
using BankServer.Data;
using BankServer.Services;
using BankShared.Constants;
using BankShared.DTOs;
using BankShared.Enums;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

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

    //public class BankClientServiceTests
    //{
    //    [Fact]
    //    public void SendRequest_ShouldWriteJsonToSharedMemory()
    //    {
    //        var service = new BankClientService();
    //        var testData = new { Command = "Test", Amount = 123.45 };

    //        using (var serverMmf = MemoryMappedFile.CreateNew(
    //            AppConstants.MemoryMappedFileName,
    //            AppConstants.MemoryBufferSize))
    //        {
    //            service.SendRequest(testData);

    //            using (var stream = serverMmf.CreateViewStream())
    //            {
    //                byte[] buffer = new byte[AppConstants.MemoryBufferSize];
    //                stream.Read(buffer, 0, buffer.Length);

    //                string jsonResult = Encoding.UTF8.GetString(buffer).TrimEnd('\0');

    //                var expectedJson = JsonSerializer.Serialize(testData);
    //                Assert.Equal(expectedJson, jsonResult);
    //            }
    //        }
    //    }
    //}

    public class StressTests
    {
        private readonly ITestOutputHelper _output;

        public StressTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void StressTest_10Clients_SimultaneousDeposit()
        {
            // === ARRANGE ===
            int numberOfClients = 10; // Кількість потоків
            decimal amountPerClient = 10.0m;
            string targetAccount = "1111"; // Початковий баланс 1000.00 (з BankRepository)

            // Очікуваний приріст: 10 * 10 = 100. 
            // Але через Race Condition у простій архітектурі IPC деякі запити можуть загубитися.
            // Цей тест покаже реальну стабільність.

            var tasks = new List<Task<TransactionResponse>>();

            _output.WriteLine($"🚀 STARTING STRESS TEST: {numberOfClients} clients targeting account {targetAccount}...");

            // === ACT ===
            for (int i = 0; i < numberOfClients; i++)
            {
                int clientId = i;
                tasks.Add(Task.Run(() =>
                {
                    var clientService = new BankClientService();
                    var request = new TransactionRequest
                    {
                        AccountNumber = targetAccount,
                        Amount = amountPerClient,
                        Type = TransactionType.Deposit
                    };

                    _output.WriteLine($"[Client {clientId}] Sending request...");

                    try
                    {
                        var response = clientService.SendRequest(request);
                        _output.WriteLine($"[Client {clientId}] Done. Result: {response.ResultStatus}, Balance: {response.NewBalance}");
                        return response;
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"[Client {clientId}] FAILED: {ex.Message}");
                        return new TransactionResponse { ResultStatus = TransactionResult.ServerError, Message = ex.Message };
                    }
                }));
            }

            // Чекаємо завершення всіх потоків
            Task.WaitAll(tasks.ToArray());

            // === ASSERT & ANALYZE ===
            int successCount = tasks.Count(t => t.Result.ResultStatus == TransactionResult.Success);
            int failCount = tasks.Count(t => t.Result.ResultStatus != TransactionResult.Success);

            _output.WriteLine("------------------------------------------------");
            _output.WriteLine($"📊 REPORT:");
            _output.WriteLine($"✅ Successful transactions: {successCount}");
            _output.WriteLine($"❌ Failed transactions: {failCount}");

            // Отримуємо фінальний баланс (робимо ще один запит, щоб дізнатися актуальний стан)
            var finalCheckService = new BankClientService();
            var finalResponse = finalCheckService.SendRequest(new TransactionRequest
            {
                AccountNumber = targetAccount,
                Amount = 0, // Фіктивний запит, щоб отримати баланс (або суму 1 грн)
                Type = TransactionType.Deposit
            });

            _output.WriteLine($"💰 FINAL BALANCE ON SERVER: {finalResponse.NewBalance}");

            // ПЕРЕВІРКА:
            // Якщо система ідеальна (з чергою), то successCount має бути 10.
            // Якщо у нас проста MemoryMappedFile (одна комірка), то буде багато ServerTimeout або перезаписів.

            // Цей Assert перевіряє, чи хоч щось пройшло успішно (м'яка перевірка)
            Assert.True(successCount > 0, "Хоча б одна транзакція мала пройти успішно!");
        }
    }
}
