using BankShared.Enums;
using System;
using System.Collections.Generic;

namespace BankShared.DTOs
{
    public class TransactionResponse
    {
        public TransactionResult ResultStatus { get; set; }
        public string Message { get; set; }
        public decimal NewBalance { get; set; }
        public string AccountNumber { get; set; }

        public List<TransactionHistoryDTO> History { get; set; }

        public TransactionResponse()
        {
            History = new List<TransactionHistoryDTO>();
        }
    }
}