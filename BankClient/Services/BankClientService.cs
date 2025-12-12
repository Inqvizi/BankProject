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
            using (var clientSignal = new EventWaitHandle(false, EventResetMode.AutoReset, AppConstants.ClientWaitSignalName))
            {
                using (var mutex = new Mutex(false, AppConstants.MutexName))
                {
                    try
                    {
                        mutex.WaitOne();

                        string json = JsonSerializer.Serialize(request);
                        byte[] data = Encoding.UTF8.GetBytes(json);

                        if (data.Length > AppConstants.MemoryBufferSize)
                            throw new Exception("Request is too large for shared memory.");

                        using (var mmf = MemoryMappedFile.CreateOrOpen(AppConstants.MemoryMappedFileName, AppConstants.MemoryBufferSize))
                        using (var stream = mmf.CreateViewStream())
                        {
                            stream.Write(new byte[AppConstants.MemoryBufferSize], 0, AppConstants.MemoryBufferSize);
                            stream.Position = 0;
                            stream.Write(data, 0, data.Length);
                        }
                    }
                    catch (AbandonedMutexException) { }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error writing to shared memory: {ex.Message}");
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }

                try
                {
                    using (var serverSignal = EventWaitHandle.OpenExisting(AppConstants.NewDataSignalName))
                    {
                        serverSignal.Set();
                    }
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    return new TransactionResponse { ResultStatus = BankShared.Enums.TransactionResult.ServerError, Message = "Server is offline" };
                }

                if (!clientSignal.WaitOne(5000))
                {
                    return new TransactionResponse { ResultStatus = BankShared.Enums.TransactionResult.ServerError, Message = "Server timeout" };
                }

                try
                {
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
                    return new TransactionResponse { ResultStatus = BankShared.Enums.TransactionResult.ServerError, Message = $"Read Error: {ex.Message}" };
                }
            }
        }
    }
}