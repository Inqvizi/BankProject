using System;
using System.Collections.Generic;
using System.Text;

namespace BankShared.Models
{
    [Serializable]
    public class BankAccount
    {
        public string AccountNumber { get; set; }
        public string OwnerName { get; set; }
        public decimal Balance { get; set; }

        //Порожній конструктор потрібен для серіалізації (фу) 
        public BankAccount() { }

        public BankAccount(string accountNumber, string ownerName, decimal initialBalance)
        {
            AccountNumber = accountNumber;
            OwnerName = ownerName;
            Balance = initialBalance;
        }
        public void Credit(decimal amount)
        {
            Balance += amount;
        }

       
        public bool Debit(decimal amount)
        {
            if (Balance >= amount) 
            {
                Balance -= amount;
                return true;
            }
            return false; 
        }
    }
}
