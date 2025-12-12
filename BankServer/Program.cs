using System;
using System.Threading;
using System.IO.MemoryMappedFiles;
using BankShared.Constants;
using BankServer.Data;
using BankServer.Services;
namespace BankServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Bank Server Started...");

            Console.WriteLine("Server is running. Press Enter to exit...");
            Console.ReadLine();
            StartIpcListener();
        }
        public static void StartIpcListener() 
        {
            Console.WriteLine("Server: Starting Event Loop Listener [B4]...");
            using (var newDataSignal = new EventWaitHandle(false, EventResetMode.AutoReset, AppConstants.NewDataSignalName))
            using (var mutex = new Mutex(false, AppConstants.MutexName))
            using (var mmf = MemoryMappedFile.CreateOrOpen(AppConstants.MemoryMappedFileName, AppConstants.MemoryBufferSize))
            {
                Console.WriteLine($"Server listening for signal: {AppConstants.NewDataSignalName}");
                while (true)
                {
                    // Блокуюча операція, очікує на сигнал від Клієнта
                    newDataSignal.WaitOne(); 

                    // Записати повідомлення "Запит отримано" в консоль після пробудження.
                    Console.WriteLine("Server: Request received! Starting processing... [B4]");

                }
            }
        }
    }
}