using System;
using System.Text;
using System.Threading;
using System.IO.MemoryMappedFiles;
using System.Text.Json;
using BankShared.Constants;
using BankShared.DTOs;
using BankShared.Enums;

namespace BankClient.Services
{
    public class BankClientService
    {
        public TransactionResponse SendRequest(TransactionRequest request)
        {

            using (var mutex = new Mutex(false, AppConstants.MutexName))
            {
                bool hasMutex = false;
                try
                {

                    try
                    {
                        hasMutex = mutex.WaitOne(TimeSpan.FromSeconds(10));
                        if (!hasMutex)
                            return new TransactionResponse { ResultStatus = TransactionResult.ServerError, Message = "Client: Queue Timeout" };
                    }
                    catch (AbandonedMutexException) { hasMutex = true; }

                    using (var mmf = MemoryMappedFile.CreateOrOpen(AppConstants.MemoryMappedFileName, AppConstants.MemoryBufferSize))
                    using (var stream = mmf.CreateViewStream())
                    {
                        string json = JsonSerializer.Serialize(request);
                        byte[] data = Encoding.UTF8.GetBytes(json);

                        stream.Write(new byte[AppConstants.MemoryBufferSize], 0, AppConstants.MemoryBufferSize);

                        stream.Position = 0;
                        stream.Write(data, 0, data.Length);
                    }

                    using (var serverSignal = new EventWaitHandle(false, EventResetMode.AutoReset, AppConstants.NewDataSignalName))
                    {
                        serverSignal.Set();
                    }

                    using (var clientSignal = new EventWaitHandle(false, EventResetMode.AutoReset, AppConstants.ClientWaitSignalName))
                    {
                        if (!clientSignal.WaitOne(TimeSpan.FromSeconds(10)))
                        {
                            return new TransactionResponse { ResultStatus = TransactionResult.ServerError, Message = "Client: Server Timeout" };
                        }
                    }

                    
                    using (var mmf = MemoryMappedFile.OpenExisting(AppConstants.MemoryMappedFileName))
                    using (var stream = mmf.CreateViewStream())
                    {
                        byte[] buffer = new byte[AppConstants.MemoryBufferSize];
                        stream.Read(buffer, 0, buffer.Length);
                        string jsonResponse = Encoding.UTF8.GetString(buffer).TrimEnd('\0');

                        return JsonSerializer.Deserialize<TransactionResponse>(jsonResponse);
                    }
                }
                catch (Exception ex)
                {
                    return new TransactionResponse { ResultStatus = TransactionResult.ServerError, Message = $"IPC Error: {ex.Message}" };
                }
                finally
                {
                    if (hasMutex) mutex.ReleaseMutex();
                }
            }
        }
    }
}