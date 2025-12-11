using System;
using System.Text;
using System.Threading;
using System.IO.MemoryMappedFiles;
using System.Text.Json;
using BankShared.Constants;

namespace BankClient.Services
{
    public class BankClientService
    {
        public void SendRequest(object request)
        {
            string json = JsonSerializer.Serialize(request);
            byte[] data = Encoding.UTF8.GetBytes(json);

            if (data.Length > AppConstants.MemoryBufferSize)
            {
                throw new Exception("Request is too large for shared memory.");
            }

            using (var mutex = new Mutex(false, AppConstants.MutexName))
            {
               
                try
                {
                    mutex.WaitOne();
                }
                catch (AbandonedMutexException)
                {
                    
                }

                try
                {
                    using (var mmf = MemoryMappedFile.CreateOrOpen(AppConstants.MemoryMappedFileName, AppConstants.MemoryBufferSize))
                    {
                        using (var stream = mmf.CreateViewStream())
                        {
                            stream.Write(new byte[AppConstants.MemoryBufferSize], 0, AppConstants.MemoryBufferSize);
                            stream.Position = 0;

                            stream.Write(data, 0, data.Length);
                        }
                    }

                    try
                    {
                        using (var signal = EventWaitHandle.OpenExisting(AppConstants.NewDataSignalName))
                        {
                            signal.Set();
                        }
                    }
                    catch (WaitHandleCannotBeOpenedException)
                    {
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error writing to shared memory: {ex.Message}");
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }
    }
}