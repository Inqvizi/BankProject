using BankShared.DTOs;
using BankShared.Enums;
using BankServer.Data; 
using BankShared.Models; 

namespace BankServer.Services
{
    public class TransactionService
    {
        private readonly BankRepository _repository;

        private static Mutex _mutex = new Mutex(); 
        public TransactionService(BankRepository repository)
        {
            _repository = repository;
        }

        // Головний метод обробки транзакцій
        public TransactionResponse ProcessRequest(TransactionRequest request)
        {
            // Ініціалізація відповіді
            var response = new TransactionResponse
            {
                AccountNumber = request.AccountNumber,
                Message = "Transaction initiated.",
                ResultStatus = TransactionResult.Success
            };

            _mutex.WaitOne();

            try
            {




            
            // Пошук рахунку
            BankAccount? account = _repository.GetByNumber(request.AccountNumber);
            if (account == null)
            {
                response.ResultStatus = TransactionResult.AccountNotFound;
                response.Message = "Account not found.";
                return response;
            }


            response.NewBalance = account.Balance;

            // Валідація суми
            if (request.Amount <= 0)
            {
                response.ResultStatus = TransactionResult.InvalidAmount;
                response.Message = "Amount must be greater than zero.";
                return response;
            }


            if (request.Type == TransactionType.Deposit)
            {
                account.Credit(request.Amount);
                response.Message = $"Deposit of {request.Amount} successful.";
            }
            else if (request.Type == TransactionType.Withdraw)
            {

                bool success = account.Debit(request.Amount);

                if (!success)
                {
                    response.ResultStatus = TransactionResult.InsufficientFunds;
                    response.Message = "Insufficient funds.";
                    response.NewBalance = account.Balance;
                    return response;
                }
                response.Message = $"Withdrawal of {request.Amount} successful.";
            }


            response.NewBalance = account.Balance;
            return response;
        }
            finally { _mutex.ReleaseMutex(); }

        }
    }
}