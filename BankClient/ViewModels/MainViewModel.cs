using BankClient.Commands;
using System;
using BankClient.Services;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using System.Xml.Serialization;
using System.Transactions;
using BankShared.Enums;
using BankShared.DTOs;
using System.Threading.Tasks;

namespace BankClient.ViewModels
{
    internal class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private readonly BankClientService _bankClient;
        private decimal _balance;
        private string _log;
        private decimal _amountToEnter;
        private bool _isBusy;
        private string _accountNumber;
        public decimal AmountToEnter
        {
            get { return _amountToEnter; }
            set {  _amountToEnter = value; OnPropertyChanged(); }
        }
        public string Log
        {
            get { return _log; }
            set { _log = value; OnPropertyChanged(); }
        }
        public decimal Balance
        {
            get { return _balance; }
            set { _balance = value; OnPropertyChanged(); }
        }
        public bool IsBusy
        {
            get
            {
                return _isBusy;
            }
            set { _isBusy = value; OnPropertyChanged(); }  
        }
        public string AccountNumber
        {
            get { 
                return _accountNumber;
            }
            set { _accountNumber = value; OnPropertyChanged(); }
        }

        public ICommand DepositCommand { get; set; }
        public ICommand WithdrawCommand { get; set; }


        public MainViewModel() { 
        var _bankService = new  BankClientService();
        DepositCommand = new RelayCommand(async _ => await ExecuteTransaction(TransactionType.Deposit));
        WithdrawCommand = new RelayCommand(async _ => await ExecuteTransaction(TransactionType.Withdraw));
        
        }

        private async Task ExecuteTransaction(TransactionType transactionType) {
            if (IsBusy)
            {
                return;
            }
            IsBusy = true;
            Log = "Transaction ongoing";

            try
            {
                var request = new TransactionRequest()
                {
                    Amount = _amountToEnter,
                    Type = transactionType,
                    AccountNumber = _accountNumber
                };
                TransactionResponse transactionResponse = await Task.Run(async () => _bankClient.SendRequest(request)); 
                
                if (transactionResponse.ResultStatus == TransactionResult.Success)
                {
                    Balance = _balance;
                    Log = $"{DateTime.Now} Operation succes";
                }
                else
                {
                    Log = $"{DateTime.Now} {transactionResponse.ResultStatus}";
                }
            
            
            }
            catch (Exception ex)
            {
                Log = $"Critical error {ex}";
            }
            finally { IsBusy = false; }
        
        
        }
    }
}
