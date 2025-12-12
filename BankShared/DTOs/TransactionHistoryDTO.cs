using System;
using BankShared.Enums;

namespace BankShared.DTOs
{
    [Serializable]
    public class TransactionHistoryDTO
    {
        public DateTime Timestamp { get; set; }
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public TransactionResult Status { get; set; }
        public string AccountNumber { get; set; }

        public TransactionHistoryDTO() { }
    }

    [Serializable]
    public class TransactionHistoryRequest
    {
        public string AccountNumber { get; set; }
        public int MaxRecords { get; set; } = 50;
    }

    [Serializable]
    public class TransactionHistoryResponse
    {
        public List<TransactionHistoryDTO> Transactions { get; set; }
        public string AccountNumber { get; set; }

        public TransactionHistoryResponse()
        {
            Transactions = new List<TransactionHistoryDTO>();
        }
    }
}