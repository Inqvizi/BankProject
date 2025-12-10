// Файл: BankServer/Services/TransactionService.cs

using BankShared.DTOs;
using BankShared.Enums;
using BankServer.Data; // Використовуємо ваш існуючий простір імен для репозиторію
using BankShared.Models; // Необхідно для BankAccount

namespace BankServer.Services
{
    public class TransactionService
    {
        private readonly BankRepository _repository;

        // Dependency Injection: приймаємо BankRepository
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

            // 1. Пошук рахунку
            BankAccount? account = _repository.GetByNumber(request.AccountNumber);
            if (account == null)
            {
                response.ResultStatus = TransactionResult.AccountNotFound;
                response.Message = "Account not found.";
                return response;
            }

            // Встановлюємо поточний баланс (якщо транзакція не пройде)
            response.NewBalance = account.Balance;

            // 2. Валідація суми
            if (request.Amount <= 0)
            {
                response.ResultStatus = TransactionResult.InvalidAmount;
                response.Message = "Amount must be greater than zero.";
                return response;
            }

            // --- 3. ВИКОНАННЯ ОСНОВНОЇ ЛОГІКИ (A2) ---

            if (request.Type == TransactionType.Deposit)
            {
                account.Credit(request.Amount);
                response.Message = $"Deposit of {request.Amount} successful.";
            }
            else if (request.Type == TransactionType.Withdraw)
            {
                // Перевірка (InsufficientFunds)
                bool success = account.Debit(request.Amount);

                if (!success)
                {
                    response.ResultStatus = TransactionResult.InsufficientFunds;
                    response.Message = "Insufficient funds.";
                    response.NewBalance = account.Balance; // Баланс не змінювався
                    return response; // НЕМЕДІЙНО ВИХОДИМО З ПОМИЛКОЮ
                }
                response.Message = $"Withdrawal of {request.Amount} successful.";
            }

            // 4. Фінальне оновлення та повернення
            response.NewBalance = account.Balance;
            return response;
        }
    }
}