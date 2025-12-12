using BankShared.Enums; 
using System;

namespace BankShared.DTOs
{
    public class TransactionResponse
    {
        public TransactionResult ResultStatus { get; set; }

        public string Message { get; set; }

        public decimal NewBalance { get; set; }

        public string AccountNumber { get; set; }

        public TransactionResponse() { }
    }
}