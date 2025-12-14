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
            var baseRequest = new BaseRequest
            {
                RequestType = RequestType.Transaction,
                JsonPayload = JsonSerializer.Serialize(request)
            };

            return SendBaseRequest<TransactionResponse>(baseRequest);
        }

        public TransferResponse SendTransferRequest(TransferRequest request)
        {
            var baseRequest = new BaseRequest
            {
                RequestType = RequestType.Transfer,
                JsonPayload = JsonSerializer.Serialize(request)
            };

            return SendBaseRequest<TransferResponse>(baseRequest);
        }

        private T SendBaseRequest<T>(BaseRequest request)
        {
            using (var accessMutex = new Mutex(false, AppConstants.MutexName))
            {
                bool hasAccessMutex = false;
                try
                {
                    try
                    {
                        hasAccessMutex = accessMutex.WaitOne(TimeSpan.FromSeconds(10));
                        if (!hasAccessMutex)
                        {
                            if (typeof(T) == typeof(TransactionResponse))
                                return (T)(object)new TransactionResponse { ResultStatus = TransactionResult.ServerError, Message = "Client: Queue Timeout (Request Write)" };
                            else
                                return (T)(object)new TransferResponse { ResultStatus = TransactionResult.ServerError, Message = "Client: Queue Timeout (Request Write)" };
                        }
                    }
                    catch (AbandonedMutexException) { hasAccessMutex = true; }

                    using (var mmf = MemoryMappedFile.OpenExisting(AppConstants.MemoryMappedFileName))
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

                    accessMutex.ReleaseMutex();
                    hasAccessMutex = false;

                    using (var clientSignal = new EventWaitHandle(false, EventResetMode.AutoReset, AppConstants.ClientWaitSignalName))
                    {
                        if (!clientSignal.WaitOne(TimeSpan.FromSeconds(10)))
                        {
                            if (typeof(T) == typeof(TransactionResponse))
                                return (T)(object)new TransactionResponse { ResultStatus = TransactionResult.ServerError, Message = "Client: Server Timeout" };
                            else
                                return (T)(object)new TransferResponse { ResultStatus = TransactionResult.ServerError, Message = "Client: Server Timeout" };
                        }
                    }

                    try
                    {
                        hasAccessMutex = accessMutex.WaitOne(TimeSpan.FromSeconds(5));
                        if (!hasAccessMutex)
                        {
                            if (typeof(T) == typeof(TransactionResponse))
                                return (T)(object)new TransactionResponse { ResultStatus = TransactionResult.ServerError, Message = "Client: Queue Timeout (Response Read)" };
                            else
                                return (T)(object)new TransferResponse { ResultStatus = TransactionResult.ServerError, Message = "Client: Queue Timeout (Response Read)" };
                        }
                    }
                    catch (AbandonedMutexException) { hasAccessMutex = true; }

                    using (var mmf = MemoryMappedFile.OpenExisting(AppConstants.MemoryMappedFileName))
                    using (var stream = mmf.CreateViewStream())
                    {
                        stream.Position = 0;
                        byte[] buffer = new byte[AppConstants.MemoryBufferSize];

                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0) throw new InvalidOperationException("IPC: Received 0 bytes from server.");

                        string jsonResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead).TrimEnd('\0');

                        return JsonSerializer.Deserialize<T>(jsonResponse)!;
                    }
                }
                catch (Exception ex)
                {
                    if (typeof(T) == typeof(TransactionResponse))
                        return (T)(object)new TransactionResponse { ResultStatus = TransactionResult.ServerError, Message = $"IPC Error: {ex.Message}" };
                    else
                        return (T)(object)new TransferResponse { ResultStatus = TransactionResult.ServerError, Message = $"IPC Error: {ex.Message}" };
                }
                finally
                {
                    if (hasAccessMutex) accessMutex.ReleaseMutex();
                }
            }
        }
    }
}