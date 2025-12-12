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
        private readonly BankRepository _repository = new BankRepository();
        private readonly TransactionService _service;
        private readonly FileLogger _logger = new FileLogger();

        private const string AliceAccount = "1111";
        private const string BobAccount = "2222"; 
        private const string NonExistentAccount = "9999";

        public TransactionServiceTests()
        {
            _service = new TransactionService(_repository, _logger);
        }

        [Fact]
        public void Deposit_ShouldIncreaseBalance_AndReturnSuccess()
        {
            var initialBalance = 1000.00m;
            var depositAmount = 250.00m;
            var expectedBalance = initialBalance + depositAmount;

            var request = new TransactionRequest
            {
                AccountNumber = AliceAccount,
                Amount = depositAmount,
                Type = TransactionType.Deposit
            };

            var response = _service.ProcessRequest(request);

            Assert.Equal(TransactionResult.Success, response.ResultStatus);
            Assert.Equal(expectedBalance, response.NewBalance);
        }

        [Fact]
        public void Withdraw_ShouldReturnInsufficientFunds_WhenBalanceIsTooLow()
        {
            var initialBalance = 500.50m; 
            var withdrawAmount = 501.00m; 

            var request = new TransactionRequest
            {
                AccountNumber = BobAccount,
                Amount = withdrawAmount,
                Type = TransactionType.Withdraw
            };

            var response = _service.ProcessRequest(request);

            Assert.Equal(TransactionResult.InsufficientFunds, response.ResultStatus);

            Assert.Equal(initialBalance, response.NewBalance);
        }

        [Fact]
        public void ProcessRequest_ShouldReturnAccountNotFound_ForInvalidAccount()
        {
            var request = new TransactionRequest
            {
                AccountNumber = NonExistentAccount,
                Amount = 10.00m,
                Type = TransactionType.Deposit
            };

            var response = _service.ProcessRequest(request);

            Assert.Equal(TransactionResult.AccountNotFound, response.ResultStatus);
        }

        [Fact]
        public void ProcessRequest_ShouldReturnInvalidAmount_ForZeroAmount()
        {
            var request = new TransactionRequest
            {
                AccountNumber = AliceAccount,
                Amount = 0.00m,
                Type = TransactionType.Deposit
            };

            var response = _service.ProcessRequest(request);

            Assert.Equal(TransactionResult.InvalidAmount, response.ResultStatus);
        }
    }
}
