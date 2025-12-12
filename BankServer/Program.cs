using System;
using System.Text;
using System.Threading;
using System.IO.MemoryMappedFiles;
using System.Text.Json;
using BankServer.Data;
using BankServer.Services;
using BankShared.Constants;
using BankShared.DTOs;

namespace BankServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Bank Server";
            Console.WriteLine("Bank Server Started..");


            var repository = new BankRepository();


            var logger = new FileLogger("logs.json");

            var transactionService = new TransactionService(repository, logger);

            using (var mmf = MemoryMappedFile.CreateOrOpen(AppConstants.MemoryMappedFileName, AppConstants.MemoryBufferSize))
            using (var serverSignal = new EventWaitHandle(false, EventResetMode.AutoReset, AppConstants.NewDataSignalName))
            {
                Console.WriteLine($"Shared Memory created.");
                Console.WriteLine($"Waiting for signals...");

                while (true)
                {

                    serverSignal.WaitOne();

                    Console.WriteLine("\n[!] Signal received. Processing request...");


                    ProcessClientRequest(transactionService);

                    Console.WriteLine("Waiting for next request...");
                }
            }
        }



        static void ProcessClientRequest(TransactionService service)
        {
  

            TransactionResponse response = null;

            try
            {
                using (var mmf = MemoryMappedFile.OpenExisting(AppConstants.MemoryMappedFileName))
                using (var stream = mmf.CreateViewStream())
                {
                    byte[] buffer = new byte[AppConstants.MemoryBufferSize];
                    stream.Read(buffer, 0, buffer.Length);
                    string jsonRequest = Encoding.UTF8.GetString(buffer).TrimEnd('\0');

                    if (string.IsNullOrWhiteSpace(jsonRequest)) return;

                    var request = JsonSerializer.Deserialize<TransactionRequest>(jsonRequest);
                    Console.WriteLine($" -> Processing: {request.AccountNumber}, {request.Amount}");

                    response = service.ProcessRequest(request);
                }

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
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            try
            {
                using (var clientSignal = EventWaitHandle.OpenExisting(AppConstants.ClientWaitSignalName))
                {
                    clientSignal.Set();
                }
            }
            catch { Console.WriteLine("Client signal missing"); }
        }
    }
}