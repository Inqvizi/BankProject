using System;
using System.Collections.Generic;
using BankShared.DTOs;
using BankShared.Enums;

namespace BankShared.Models
{
    [Serializable]
    public class BankAccount
    {
        public string AccountNumber { get; set; }
        public string OwnerName { get; set; }
        public decimal Balance { get; set; }
        public List<TransactionHistoryDTO> TransactionHistory { get; set; }

        public BankAccount()
        {
            TransactionHistory = new List<TransactionHistoryDTO>();
        }

        public BankAccount(string accountNumber, string ownerName, decimal initialBalance)
        {
            AccountNumber = accountNumber;
            OwnerName = ownerName;
            Balance = initialBalance;
            TransactionHistory = new List<TransactionHistoryDTO>();
        }

        public void Credit(decimal amount)
        {
            Balance += amount;
            AddTransactionToHistory(TransactionType.Deposit, amount, TransactionResult.Success);
        }

        public bool Debit(decimal amount)
        {
            if (Balance >= amount)
            {
                Balance -= amount;
                AddTransactionToHistory(TransactionType.Withdraw, amount, TransactionResult.Success);
                return true;
            }

            AddTransactionToHistory(TransactionType.Withdraw, amount, TransactionResult.InsufficientFunds);
            return false;
        }

        private void AddTransactionToHistory(TransactionType type, decimal amount, TransactionResult status)
        {
            var transaction = new TransactionHistoryDTO
            {
                Timestamp = DateTime.Now,
                Type = type,
                Amount = amount,
                BalanceAfter = Balance,
                Status = status,
                AccountNumber = AccountNumber
            };

            TransactionHistory.Insert(0, transaction);

            if (TransactionHistory.Count > 100)
            {
                TransactionHistory.RemoveAt(TransactionHistory.Count - 1);
            }
        }

        public List<TransactionHistoryDTO> GetTransactionHistory(int maxRecords = 50)
        {
            int recordsToReturn = Math.Min(maxRecords, TransactionHistory.Count);
            return TransactionHistory.GetRange(0, recordsToReturn);
        }
    }
}