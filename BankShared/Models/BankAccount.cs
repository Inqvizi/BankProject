using System;
using System.Collections.Generic;
using System.Text;

namespace BankShared.Models
{
    [Serializable]
    public class BankAccount
    {
        public string CardNumber { get; set; }
        public string OwnerName { get; set; }
        public decimal Balance { get; set; }

        //Порожній конструктор потрібен для серіалізації
        public BankAccount() { }

        public BankAccount(string cardNumber, string ownerName, decimal initialBalance)
        {
            CardNumber = cardNumber;
            OwnerName = ownerName;
            Balance = initialBalance;
        }
    }
}
