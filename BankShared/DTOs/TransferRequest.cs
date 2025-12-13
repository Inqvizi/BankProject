using System;
using BankShared.Enums;

namespace BankShared.DTOs
{
    [Serializable]
    public class TransferRequest
    {
        public string FromAccountNumber { get; set; }
        public string ToAccountNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    [Serializable]
    public class TransferResponse
    {
        public TransactionResult ResultStatus { get; set; }
        public string Message { get; set; }
        public decimal FromAccountNewBalance { get; set; }
        public decimal ToAccountNewBalance { get; set; }
        public string FromAccountNumber { get; set; }
        public string ToAccountNumber { get; set; }
    }
}