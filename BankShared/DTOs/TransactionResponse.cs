using System;
using System.Collections.Generic;
using System.Text;

namespace BankShared.DTOs
{
    public class TransactionResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public decimal NewBalance { get; set; }
    }
}
