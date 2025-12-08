using System;
using System.Collections.Generic;
using System.Text;

namespace BankShared.Constants
{
    public static class AppConstants
    {
        //Назва спільної пам'яті
        public const string MemoryMappedFileName = "Local\\BankAppSharedMemory";

        //Назва М'ютекса для синхронізації
        public const string MutexName = "Local\\BankAppMutex";

        //Назва події, яка сигналить серверу, що прийшли нові дані
        public const string NewDataSignalName = "Local\\BankAppNewDataSignal";

        //Розмір буфера в байтах
        public const int MemoryBufferSize = 4096;
    }
}
