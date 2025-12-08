using System;
using BankShared.Enums;

namespace BankShared.DTOs
{
    public class TransactionRequest
    {
        public TransactionType Type { get; set; }
        public string AccountNumber { get; set; }
        public decimal Amount { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}