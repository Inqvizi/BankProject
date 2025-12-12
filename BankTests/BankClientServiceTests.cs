using Xunit;
using BankClient.Services;
using BankShared.Constants;
using BankShared.DTOs;  
using BankShared.Enums; 
using System;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Text;

namespace BankTests.ClientServiceTests
{
    public class BankClientServiceTests : IDisposable
    {
        private readonly MemoryMappedFile _serverMmf;
        private readonly EventWaitHandle _serverSignal;
        private const int BufferSize = AppConstants.MemoryBufferSize;

        public BankClientServiceTests()
        {
            _serverMmf = MemoryMappedFile.CreateOrOpen(
                AppConstants.MemoryMappedFileName,
                BufferSize
            );

            _serverSignal = new EventWaitHandle(false, EventResetMode.AutoReset, AppConstants.NewDataSignalName);
        }

        public void Dispose()
        {
            _serverMmf?.Dispose();
            _serverSignal?.Dispose();
        }

        [Fact]
        public void SendRequest_ValidData_WritesToSharedMemoryAndSignals()
        {
            var service = new BankClientService();
            var requestData = new TransactionRequest
            {
                Type = TransactionType.Deposit,
                AccountNumber = "1111",
                Amount = 1000.00m
            };

            try
            {
                service.SendRequest(requestData);
            }
            catch
            {
            }

            bool signalReceived = _serverSignal.WaitOne(TimeSpan.FromSeconds(1));
            Assert.True(signalReceived, "Сигнал не отримано від клієнта!");

            using (var stream = _serverMmf.CreateViewStream())
            {
                byte[] buffer = new byte[AppConstants.MemoryBufferSize];
                stream.Read(buffer, 0, buffer.Length);
                string jsonString = Encoding.UTF8.GetString(buffer).TrimEnd('\0');

                Assert.Contains("0", jsonString);
                Assert.Contains("1111", jsonString);
                Assert.Contains("1000", jsonString);
            }
        }

        [Fact]
        public void SendRequest_TooLargeData_ThrowsException()
        {
            var service = new BankClientService();

            string hugeString = new string('A', BufferSize + 50);
            var hugeRequest = new TransactionRequest
            {
                AccountNumber = hugeString,
                Type = TransactionType.Deposit,
                Amount = 10
            };

            var ex = Assert.Throws<Exception>(() => service.SendRequest(hugeRequest));
            Assert.Contains("Request is too large", ex.Message);
        }

        [Fact]
        public void SendRequest_ConcurrentAccess_RespectsMutex()
        {
            var service = new BankClientService();
            var requestData = new TransactionRequest
            {
                Type = TransactionType.Withdraw,
                Amount = 50,
                AccountNumber = "Test"
            };

            using (var mutex = new Mutex(false, AppConstants.MutexName))
            {
                bool mutexAcquired = mutex.WaitOne(TimeSpan.FromSeconds(1));
                Assert.True(mutexAcquired, "Тест не зміг захопити мютекс.");

                bool clientStartedWriting = false;
                object lockObj = new object();

                System.Threading.Tasks.Task task = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        service.SendRequest(requestData);
                    }
                    catch
                    {
                    }
                    finally
                    {
                        lock (lockObj)
                        {
                            clientStartedWriting = true;
                        }
                    }
                });

                Thread.Sleep(500);

                lock (lockObj)
                {
                    Assert.False(task.IsCompleted, "Клієнт проігнорував мютекс і записав дані!");
                }

                mutex.ReleaseMutex();

                bool finishedAfterRelease = task.Wait(TimeSpan.FromSeconds(6));
                Assert.True(finishedAfterRelease, "Клієнт не завершився після звільнення мютекса.");
            }
        }
    }
}