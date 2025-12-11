using Xunit;
using BankClient.Services;
using BankShared.Constants;
using System;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Text.Json;
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
            var requestData = new { Command = "Deposit", Amount = 100 };

            service.SendRequest(requestData);

            bool signalReceived = _serverSignal.WaitOne(timeout: TimeSpan.FromSeconds(2));
            Assert.True(signalReceived, "Сервер не отримав сигнал про нові дані (таймаут).");

            using (var stream = _serverMmf.CreateViewStream())
            {
                byte[] buffer = new byte[BufferSize];
                stream.Read(buffer, 0, BufferSize);

                int nullIndex = Array.IndexOf(buffer, (byte)0);
                int dataLength = nullIndex >= 0 ? nullIndex : buffer.Length;

                string jsonString = Encoding.UTF8.GetString(buffer, 0, dataLength);

                Assert.Contains("Deposit", jsonString);
                Assert.Contains("100", jsonString);
            }
        }

        [Fact]
        public void SendRequest_TooLargeData_ThrowsException()
        {
            var service = new BankClientService();

            string hugeString = new string('A', BufferSize + 50);
            var hugeRequest = new { Data = hugeString };

            var ex = Assert.Throws<Exception>(() => service.SendRequest(hugeRequest));
            Assert.Equal("Request is too large for shared memory.", ex.Message);
        }

        [Fact]
        public void SendRequest_ConcurrentAccess_RespectsMutex()
        {
            var service = new BankClientService();
            var requestData = new { Command = "Test" };

            using (var mutex = new Mutex(false, AppConstants.MutexName))
            {
                bool mutexAcquired = mutex.WaitOne(TimeSpan.FromSeconds(1));
                Assert.True(mutexAcquired, "Тест не зміг захопити мютекс.");

                System.Threading.Tasks.Task task = null;

                try
                {
                    task = System.Threading.Tasks.Task.Run(() =>
                    {
                        service.SendRequest(requestData);
                    });

                    bool completed = task.Wait(500);

                    Assert.False(completed, "Клієнт проігнорував мютекс і записав дані!");
                }
                finally
                {
                    mutex.ReleaseMutex();
                }

                if (task != null)
                {
                    try
                    {
                        task.Wait(TimeSpan.FromSeconds(2));
                    }
                    catch (AggregateException)
                    {
                    }

                    Assert.True(task.IsCompleted, "Клієнт мав би завершити роботу після звільнення мютекса.");
                }
            }
        }
    }
}