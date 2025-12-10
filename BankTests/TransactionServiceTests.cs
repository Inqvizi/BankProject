using BankServer.Data;
using BankServer.Services;
using BankShared.DTOs;
using BankShared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankTests
{
    public class TransactionServiceTests
    {
        // Примітка: Репозиторій і Сервіс створюються для кожного тесту
        private readonly BankRepository _repository = new BankRepository();
        private readonly TransactionService _service;

        // Використовуємо номери рахунків, які були "засіяні" у BankRepository
        private const string AliceAccount = "1111"; // Початковий баланс 1000.00m
        private const string BobAccount = "2222";   // Початковий баланс 500.50m
        private const string NonExistentAccount = "9999";

        public TransactionServiceTests()
        {
            // Ініціалізуємо сервіс, який буде використовувати тестові дані
            _service = new TransactionService(_repository);
        }

        // 1. Тест успішного депозиту
        [Fact]
        public void Deposit_ShouldIncreaseBalance_AndReturnSuccess()
        {
            // Arrange
            var initialBalance = 1000.00m;
            var depositAmount = 250.00m;
            var expectedBalance = initialBalance + depositAmount;

            var request = new TransactionRequest
            {
                AccountNumber = AliceAccount,
                Amount = depositAmount,
                Type = TransactionType.Deposit
            };

            // Act
            var response = _service.ProcessRequest(request);

            // Assert
            Assert.Equal(TransactionResult.Success, response.ResultStatus);
            Assert.Equal(expectedBalance, response.NewBalance);
        }

        // 2. Тест на недостатність коштів при знятті
        [Fact]
        public void Withdraw_ShouldReturnInsufficientFunds_WhenBalanceIsTooLow()
        {
            // Arrange
            var initialBalance = 500.50m; // Баланс Bob's
            var withdrawAmount = 501.00m; // Більше, ніж є

            var request = new TransactionRequest
            {
                AccountNumber = BobAccount,
                Amount = withdrawAmount,
                Type = TransactionType.Withdraw
            };

            // Act
            var response = _service.ProcessRequest(request);

            // Assert
            Assert.Equal(TransactionResult.InsufficientFunds, response.ResultStatus);
            // Баланс не повинен змінитися
            Assert.Equal(initialBalance, response.NewBalance);
        }

        // 3. Тест на неіснуючий рахунок
        [Fact]
        public void ProcessRequest_ShouldReturnAccountNotFound_ForInvalidAccount()
        {
            // Arrange
            var request = new TransactionRequest
            {
                AccountNumber = NonExistentAccount,
                Amount = 10.00m,
                Type = TransactionType.Deposit
            };

            // Act
            var response = _service.ProcessRequest(request);

            // Assert
            Assert.Equal(TransactionResult.AccountNotFound, response.ResultStatus);
        }

        // 4. Тест на недійсну суму (від'ємну або нульову)
        [Fact]
        public void ProcessRequest_ShouldReturnInvalidAmount_ForZeroAmount()
        {
            // Arrange
            var request = new TransactionRequest
            {
                AccountNumber = AliceAccount,
                Amount = 0.00m,
                Type = TransactionType.Deposit
            };

            // Act
            var response = _service.ProcessRequest(request);

            // Assert
            Assert.Equal(TransactionResult.InvalidAmount, response.ResultStatus);
        }
    }
}
