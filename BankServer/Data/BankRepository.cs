using BankShared.Models;
using System.Collections.Generic;
using System.Linq;

namespace BankServer.Data
{
    public class BankRepository
    {
        private List<BankAccount> _accounts;

        public BankRepository()
        {
            _accounts = new List<BankAccount>();
            SeedData();
        }

        private void SeedData()
        {
            _accounts.Add(new BankAccount { AccountNumber = "1111", Balance = 1000.00m });
            _accounts.Add(new BankAccount { AccountNumber = "2222", Balance = 500.50m });
            _accounts.Add(new BankAccount { AccountNumber = "3333", Balance = 999999.00m });
        }

        public BankAccount? GetByNumber(string accountNumber)
        {
            return _accounts.FirstOrDefault(a => a.AccountNumber == accountNumber);
        }

        public List<BankAccount> GetAll()
        {
            return _accounts;
        }
    }
}
