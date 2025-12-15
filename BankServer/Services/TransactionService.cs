using BankShared.DTOs;
using BankShared.Enums;
using BankServer.Data; 
using BankShared.Models; 

namespace BankServer.Services
{
    public class TransactionService
    {
        private readonly BankRepository _repository;
        private readonly FileLogger _fileLogger;

        public TransactionService(BankRepository repository, FileLogger fileLogger)
        {
            _repository = repository;
            _fileLogger = fileLogger;
        }

        public TransactionResponse ProcessRequest(TransactionRequest request)
        {
            var response = new TransactionResponse
            {
                AccountNumber = request.AccountNumber,
                Message = "Transaction initiated.",
                ResultStatus = TransactionResult.Success
            };

            BankAccount? account = _repository.GetByNumber(request.AccountNumber);
            if (account == null)
            {
                response.ResultStatus = TransactionResult.AccountNotFound;
                response.Message = "Account not found.";
                _fileLogger.LogTransaction($"FAILED: Account {request.AccountNumber} not found. Op: {request.Type}");
                return response;
            }

            if (request.Type == TransactionType.CheckBalance)
            {
                response.NewBalance = account.Balance;
                response.Message = "Sync";

                response.History = account.GetTransactionHistory(10);

                return response;
            }

            if (request.Amount <= 0)
            {
                response.ResultStatus = TransactionResult.InvalidAmount;
                response.Message = "Amount must be greater than zero.";
                _fileLogger.LogTransaction($"FAILED: Invalid amount {request.Amount} for {request.AccountNumber}");
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
                    _fileLogger.LogTransaction($"FAILED: Withdrawal of {request.Amount} from {request.AccountNumber} - Insufficient Funds");
                    return response;
                }
                response.Message = $"Withdrawal of {request.Amount} successful.";
            }

            response.NewBalance = account.Balance;
            _fileLogger.LogTransaction($"SUCCESS: {request.Type} {request.Amount} for {request.AccountNumber}. New Balance: {account.Balance}");

            return response;
        }
    }
}