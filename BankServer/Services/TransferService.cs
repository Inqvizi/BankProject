using BankServer.Data;
using BankShared.DTOs;
using BankShared.Enums;
using BankShared.Models;

namespace BankServer.Services
{
    public class TransferService
    {
        private readonly BankRepository _repository;
        private readonly FileLogger _fileLogger;

        public TransferService(BankRepository repository, FileLogger fileLogger)
        {
            _repository = repository;
            _fileLogger = fileLogger;
        }

        public TransferResponse ProcessTransfer(TransferRequest request)
        {
            var response = new TransferResponse
            {
                FromAccountNumber = request.FromAccountNumber,
                ToAccountNumber = request.ToAccountNumber,
                ResultStatus = TransactionResult.Success,
                Message = "Transfer initiated."
            };

            if (request.Amount <= 0)
            {
                response.ResultStatus = TransactionResult.InvalidAmount;
                response.Message = "Amount must be greater than zero.";
                _fileLogger.LogTransaction($"FAILED: Invalid transfer amount {request.Amount}");
                return response;
            }

            if (request.FromAccountNumber == request.ToAccountNumber)
            {
                response.ResultStatus = TransactionResult.ServerError;
                response.Message = "Cannot transfer to the same account.";
                _fileLogger.LogTransaction($"FAILED: Transfer to same account {request.FromAccountNumber}");
                return response;
            }

            BankAccount? fromAccount = _repository.GetByNumber(request.FromAccountNumber);
            if (fromAccount == null)
            {
                response.ResultStatus = TransactionResult.AccountNotFound;
                response.Message = "Source account not found.";
                _fileLogger.LogTransaction($"FAILED: Source account {request.FromAccountNumber} not found");
                return response;
            }

            BankAccount? toAccount = _repository.GetByNumber(request.ToAccountNumber);
            if (toAccount == null)
            {
                response.ResultStatus = TransactionResult.AccountNotFound;
                response.Message = "Destination account not found.";
                _fileLogger.LogTransaction($"FAILED: Destination account {request.ToAccountNumber} not found");
                return response;
            }

            if (fromAccount.Balance < request.Amount)
            {
                response.ResultStatus = TransactionResult.InsufficientFunds;
                response.Message = "Insufficient funds for transfer.";
                response.FromAccountNewBalance = fromAccount.Balance;
                response.ToAccountNewBalance = toAccount.Balance;
                _fileLogger.LogTransaction($"FAILED: Transfer of {request.Amount} from {request.FromAccountNumber} - Insufficient Funds");
                return response;
            }

            bool debitSuccess = fromAccount.Debit(request.Amount);
            if (debitSuccess)
            {
                toAccount.Credit(request.Amount);

                response.FromAccountNewBalance = fromAccount.Balance;
                response.ToAccountNewBalance = toAccount.Balance;
                response.Message = $"Transfer of ${request.Amount} successful.";

                _fileLogger.LogTransaction($"SUCCESS: Transfer {request.Amount} from {request.FromAccountNumber} to {request.ToAccountNumber}. From: {fromAccount.Balance}, To: {toAccount.Balance}");
            }
            else
            {
                response.ResultStatus = TransactionResult.InsufficientFunds;
                response.Message = "Transfer failed due to insufficient funds.";
                response.FromAccountNewBalance = fromAccount.Balance;
                response.ToAccountNewBalance = toAccount.Balance;
            }

            return response;
        }
    }
}