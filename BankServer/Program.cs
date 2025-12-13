using System;
using System.Text;
using System.Threading;
using System.IO.MemoryMappedFiles;
using System.Text.Json;
using BankServer.Data;
using BankServer.Services;
using BankShared.Constants;
using BankShared.DTOs;
using BankShared.Enums;

namespace BankServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Midnight Finance Server";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║     MIDNIGHT FINANCE SERVER v1.0       ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();

            var repository = new BankRepository();
            var logger = new FileLogger("logs.json");
            var transactionService = new TransactionService(repository, logger);
            var transferService = new TransferService(repository, logger);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.ResetColor();
            Console.WriteLine();

            using (var mmf = MemoryMappedFile.CreateOrOpen(AppConstants.MemoryMappedFileName, AppConstants.MemoryBufferSize))
            using (var serverSignal = new EventWaitHandle(false, EventResetMode.AutoReset, AppConstants.NewDataSignalName))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Shared Memory created");
                Console.WriteLine("Listening for client requests...");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine();

                int requestCount = 0;

                while (true)
                {
                    serverSignal.WaitOne();
                    requestCount++;
                    ProcessClientRequest(transactionService, transferService, requestCount);
                }
            }
        }

        static void ProcessClientRequest(TransactionService transactionService, TransferService transferService, int requestNumber)
        {
            DateTime startTime = DateTime.Now;

            try
            {
                using (var mmf = MemoryMappedFile.OpenExisting(AppConstants.MemoryMappedFileName))
                using (var stream = mmf.CreateViewStream())
                {
                    byte[] buffer = new byte[AppConstants.MemoryBufferSize];
                    stream.Read(buffer, 0, buffer.Length);
                    string jsonRequest = Encoding.UTF8.GetString(buffer).TrimEnd('\0');

                    if (string.IsNullOrWhiteSpace(jsonRequest)) return;

                    var baseRequest = JsonSerializer.Deserialize<BaseRequest>(jsonRequest);

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"┌─ Request #{requestNumber} [{DateTime.Now:HH:mm:ss}]");
                    Console.ResetColor();

                    if (baseRequest.RequestType == RequestType.Transfer)
                    {
                        var transferRequest = JsonSerializer.Deserialize<TransferRequest>(baseRequest.JsonPayload);
                        ProcessTransferRequest(transferService, transferRequest, startTime);
                    }
                    else
                    {
                        var transactionRequest = JsonSerializer.Deserialize<TransactionRequest>(baseRequest.JsonPayload);
                        ProcessTransactionRequest(transactionService, transactionRequest, startTime);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"┌─ ERROR in Request #{requestNumber}");
                Console.WriteLine($"│  {ex.Message}");
                Console.WriteLine($"└─────────────────────────────────────────");
                Console.ResetColor();
                Console.WriteLine();
            }

            try
            {
                using (var clientSignal = EventWaitHandle.OpenExisting(AppConstants.ClientWaitSignalName))
                {
                    clientSignal.Set();
                }
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Client signal not available");
                Console.ResetColor();
            }
        }

        static void ProcessTransactionRequest(TransactionService service, TransactionRequest request, DateTime startTime)
        {
            Console.ForegroundColor = request.Type == TransactionType.Deposit
                ? ConsoleColor.Green
                : ConsoleColor.Red;
            Console.WriteLine($"│  Type: {request.Type}");
            Console.ResetColor();
            Console.WriteLine($"│  Account: {request.AccountNumber}");
            Console.WriteLine($"│  Amount: ${request.Amount:N2}");

            var response = service.ProcessRequest(request);
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;

            if (response.ResultStatus == TransactionResult.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"│  Status: SUCCESS");
                Console.WriteLine($"│  New Balance: ${response.NewBalance:N2}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"│  Status: {response.ResultStatus}");
                Console.WriteLine($"│  Message: {response.Message}");
            }

            Console.ResetColor();
            Console.WriteLine($"│  Processed in: {elapsed:F2}ms");
            Console.WriteLine($"└─────────────────────────────────────────");
            Console.WriteLine();

            WriteResponseToMemory(response);
        }

        static void ProcessTransferRequest(TransferService service, TransferRequest request, DateTime startTime)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"│  Type: TRANSFER");
            Console.ResetColor();
            Console.WriteLine($"│  From: {request.FromAccountNumber}");
            Console.WriteLine($"│  To: {request.ToAccountNumber}");
            Console.WriteLine($"│  Amount: ${request.Amount:N2}");

            var response = service.ProcessTransfer(request);
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;

            if (response.ResultStatus == TransactionResult.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"│  ✓ Status: SUCCESS");
                Console.WriteLine($"│  From Balance: ${response.FromAccountNewBalance:N2}");
                Console.WriteLine($"│  To Balance: ${response.ToAccountNewBalance:N2}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"│  Status: {response.ResultStatus}");
                Console.WriteLine($"│  Message: {response.Message}");
            }

            Console.ResetColor();
            Console.WriteLine($"│  Processed in: {elapsed:F2}ms");
            Console.WriteLine($"└─────────────────────────────────────────");
            Console.WriteLine();

            WriteResponseToMemory(response);
        }

        static void WriteResponseToMemory(object response)
        {
            string jsonResponse = JsonSerializer.Serialize(response);
            byte[] responseData = Encoding.UTF8.GetBytes(jsonResponse);

            using (var mmf = MemoryMappedFile.OpenExisting(AppConstants.MemoryMappedFileName))
            using (var stream = mmf.CreateViewStream())
            {
                stream.Write(new byte[AppConstants.MemoryBufferSize], 0, AppConstants.MemoryBufferSize);
                stream.Position = 0;
                stream.Write(responseData, 0, responseData.Length);
            }
        }
    }
}