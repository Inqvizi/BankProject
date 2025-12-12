using BankShared.Enums; // Необхідно додати, якщо його немає
using System;

namespace BankShared.DTOs
{
    [Serializable] // Обов'язково для Міжпроцесної Взаємодії (IPC)
    public class TransactionResponse
    {
        // Поле для коду стану (Успіх, Недостатньо коштів, Рахунок не знайдено)
        public TransactionResult ResultStatus { get; set; }

        // Поле для детального повідомлення користувачеві
        public string Message { get; set; }

        // Поле для оновленого балансу
        public decimal NewBalance { get; set; }

        // Поле для ідентифікації рахунку
        public string AccountNumber { get; set; }

        // Порожній конструктор
        public TransactionResponse() { }
    }
}