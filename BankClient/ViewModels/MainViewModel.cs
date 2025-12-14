using BankClient.Commands;
using System;
using BankClient.Services;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BankShared.Enums;
using BankShared.DTOs;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;

namespace BankClient.ViewModels
{
    public class TransactionHistoryItem
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; }
    }

    public class AccountInfo
    {
        public string AccountNumber { get; set; }
        public string DisplayName { get; set; }
        public decimal Balance { get; set; }

        public override string ToString() => DisplayName;
    }

    public class CurrencyRate : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private decimal _rate;

        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }

        public decimal Rate
        {
            get => _rate;
            set
            {
                _rate = value;
                OnPropertyChanged();
            }
        }

        public string Flag { get; set; }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private readonly BankClientService _bankClient;
        private readonly CurrencyService _currencyService;
        private decimal _balance;
        private string _log;
        private string _amountToEnter;
        private bool _isBusy;
        private AccountInfo _selectedAccount;
        private string _transferToAccountNumber;
        private string _transferAmount;
        private bool _isTransferMode;

        private Dictionary<string, ObservableCollection<TransactionHistoryItem>> _accountHistories;

        public ObservableCollection<AccountInfo> Accounts { get; set; }
        public ObservableCollection<TransactionHistoryItem> TransactionHistory { get; set; }
        public ObservableCollection<CurrencyRate> CurrencyRates { get; set; }

        public AccountInfo SelectedAccount
        {
            get { return _selectedAccount; }
            set
            {
                _selectedAccount = value;
                OnPropertyChanged();
                if (value != null)
                {
                    Balance = value.Balance;
                    LoadAccountHistory(value.AccountNumber);
                }
            }
        }

        public string TransferToAccountNumber
        {
            get { return _transferToAccountNumber; }
            set { _transferToAccountNumber = value; OnPropertyChanged(); }
        }

        public string TransferAmount
        {
            get { return _transferAmount; }
            set { _transferAmount = value; OnPropertyChanged(); }
        }

        public bool IsTransferMode
        {
            get { return _isTransferMode; }
            set { _isTransferMode = value; OnPropertyChanged(); }
        }

        public string AmountToEnter
        {
            get { return _amountToEnter; }
            set { _amountToEnter = value; OnPropertyChanged(); }
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
            get { return _isBusy; }
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public ICommand DepositCommand { get; set; }
        public ICommand WithdrawCommand { get; set; }
        public ICommand RefreshCommand { get; set; }
        public ICommand TransferCommand { get; set; }
        public ICommand ToggleTransferModeCommand { get; set; }

        public MainViewModel()
        {
            _bankClient = new BankClientService();
            _currencyService = new CurrencyService();

            Accounts = new ObservableCollection<AccountInfo>
            {
                new AccountInfo { AccountNumber = "1111", DisplayName = "Main Account (1111)", Balance = 1000.00m },
                new AccountInfo { AccountNumber = "2222", DisplayName = "Savings Account (2222)", Balance = 500.50m },
                new AccountInfo { AccountNumber = "3333", DisplayName = "Investment Account (3333)", Balance = 999999.00m }
            };

            TransactionHistory = new ObservableCollection<TransactionHistoryItem>();
            CurrencyRates = new ObservableCollection<CurrencyRate>();

            _accountHistories = new Dictionary<string, ObservableCollection<TransactionHistoryItem>>();
            foreach (var account in Accounts)
            {
                _accountHistories[account.AccountNumber] = new ObservableCollection<TransactionHistoryItem>();
            }

            _accountHistories["1111"].Add(new TransactionHistoryItem
            {
                Timestamp = DateTime.Now.AddDays(-2),
                Type = "Deposit",
                Amount = 500.00m,
                Balance = 1500.00m,
                Status = "Success"
            });
            _accountHistories["1111"].Add(new TransactionHistoryItem
            {
                Timestamp = DateTime.Now.AddDays(-1),
                Type = "Withdraw",
                Amount = 500.00m,
                Balance = 1000.00m,
                Status = "Success"
            });

            DepositCommand = new RelayCommand(async _ => await ExecuteTransaction(TransactionType.Deposit));
            WithdrawCommand = new RelayCommand(async _ => await ExecuteTransaction(TransactionType.Withdraw));
            RefreshCommand = new RelayCommand(async _ => await RefreshCurrencyRates());
            TransferCommand = new RelayCommand(async _ => await ExecuteTransfer());
            ToggleTransferModeCommand = new RelayCommand(_ => ToggleTransferMode());

            SelectedAccount = Accounts.First();

            _ = LoadCurrencyRatesAsync();

            Log = "Ready";
        }

        private void LoadAccountHistory(string accountNumber)
        {
            TransactionHistory.Clear();

            if (_accountHistories.ContainsKey(accountNumber))
            {
                foreach (var transaction in _accountHistories[accountNumber])
                {
                    TransactionHistory.Add(transaction);
                }
            }
        }

        private async Task LoadCurrencyRatesAsync()
        {
            Log = "Loading currency rates...";
            try
            {
                var rates = await _currencyService.GetLatestRatesAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrencyRates.Clear();

                    var currenciesToDisplay = new[]
                    {
                        ("EUR", "Euro", "🇪🇺"),
                        ("GBP", "British Pound", "🇬🇧"),
                        ("JPY", "Japanese Yen", "🇯🇵"),
                        ("CHF", "Swiss Franc", "🇨🇭"),
                        ("CAD", "Canadian Dollar", "🇨🇦"),
                        ("UAH", "Ukrainian Hryvnia", "🇺🇦") 
                     };

                    foreach (var (code, name, flag) in currenciesToDisplay)
                    {
                        if (rates.ContainsKey(code))
                        {
                            CurrencyRates.Add(new CurrencyRate
                            {
                                CurrencyCode = code,
                                CurrencyName = name,
                                Rate = rates[code],
                                Flag = flag
                            });
                        }
                    }

                    Log = $"{DateTime.Now:HH:mm:ss} - Currency rates loaded";
                });
            }
            catch (Exception ex)
            {
                Log = $"Failed to load currency rates: {ex.Message}";
                LoadFallbackCurrencyRates();
            }
        }

        private async Task RefreshCurrencyRates()
        {
            Log = "Refreshing currency rates...";
            await LoadCurrencyRatesAsync();
        }

        private void LoadFallbackCurrencyRates()
        {
            CurrencyRates.Clear();
            CurrencyRates.Add(new CurrencyRate { CurrencyCode = "EUR", CurrencyName = "Euro", Rate = 0.92m, Flag = "🇪🇺" });
            CurrencyRates.Add(new CurrencyRate { CurrencyCode = "GBP", CurrencyName = "British Pound", Rate = 0.79m, Flag = "🇬🇧" });
            CurrencyRates.Add(new CurrencyRate { CurrencyCode = "JPY", CurrencyName = "Japanese Yen", Rate = 149.50m, Flag = "🇯🇵" });
            CurrencyRates.Add(new CurrencyRate { CurrencyCode = "CHF", CurrencyName = "Swiss Franc", Rate = 0.88m, Flag = "🇨🇭" });
            CurrencyRates.Add(new CurrencyRate { CurrencyCode = "CAD", CurrencyName = "Canadian Dollar", Rate = 1.36m, Flag = "🇨🇦" });
        }

        private void ToggleTransferMode()
        {
            IsTransferMode = !IsTransferMode;
            if (IsTransferMode)
            {
                TransferToAccountNumber = "";
                Log = "Transfer mode activated. Enter recipient account number.";
            }
            else
            {
                Log = "Transfer mode deactivated";
            }
        }

        private async Task ExecuteTransfer()
        {
            if (IsBusy || SelectedAccount == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(TransferToAccountNumber))
            {
                Log = "Please enter recipient account number";
                return;
            }

            if (TransferToAccountNumber == SelectedAccount.AccountNumber)
            {
                Log = "Cannot transfer to the same account";
                return;
            }

            IsBusy = true;
            Log = "Processing transfer...";

            try
            {
                if (!decimal.TryParse(TransferAmount, out decimal validAmount))
                {
                    Log = "Error: Please enter a valid number";
                    MessageBox.Show(Log);
                    return;
                }

                if (validAmount <= 0)
                {
                    Log = "Please enter a valid transfer amount";
                    return;
                }

                if (validAmount > Balance)
                {
                    Log = "Insufficient funds for transfer";
                    return;
                }

                var transferRequest = new TransferRequest
                {
                    FromAccountNumber = SelectedAccount.AccountNumber,
                    ToAccountNumber = TransferToAccountNumber.Trim(),
                    Amount = validAmount
                };

                TransferResponse response = await Task.Run(() => _bankClient.SendTransferRequest(transferRequest));

                if (response.ResultStatus == TransactionResult.Success)
                {
                    Balance = response.FromAccountNewBalance;
                    SelectedAccount.Balance = response.FromAccountNewBalance;

                    var targetAccount = Accounts.FirstOrDefault(a => a.AccountNumber == TransferToAccountNumber.Trim());
                    if (targetAccount != null)
                    {
                        targetAccount.Balance = response.ToAccountNewBalance;
                    }

                    var senderHistoryItem = new TransactionHistoryItem
                    {
                        Timestamp = DateTime.Now,
                        Type = $"Transfer to {TransferToAccountNumber}",
                        Amount = validAmount,
                        Balance = response.FromAccountNewBalance,
                        Status = "Success"
                    };

                    TransactionHistory.Insert(0, senderHistoryItem);
                    _accountHistories[SelectedAccount.AccountNumber].Insert(0, senderHistoryItem);

                    if (_accountHistories.ContainsKey(TransferToAccountNumber.Trim()))
                    {
                        var receiverHistoryItem = new TransactionHistoryItem
                        {
                            Timestamp = DateTime.Now,
                            Type = $"Transfer from {SelectedAccount.AccountNumber}",
                            Amount = validAmount,
                            Balance = response.ToAccountNewBalance,
                            Status = "Success"
                        };

                        _accountHistories[TransferToAccountNumber.Trim()].Insert(0, receiverHistoryItem);
                    }

                    Log = $"{DateTime.Now:HH:mm:ss} - Transfer of ${TransferAmount:N2} to {TransferToAccountNumber} successful";
                    TransferToAccountNumber = "";
                    IsTransferMode = false;
                }
                else
                {
                    Log = $"Transfer failed: {response.Message}";
                }
            }
            catch (Exception ex)
            {
                Log = $"Transfer error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteTransaction(TransactionType transactionType)
        {
            if (IsBusy || SelectedAccount == null)
            {
                return;
            }

            IsBusy = true;
            Log = "Processing transaction...";

            try
            {
                if (!decimal.TryParse(AmountToEnter, out decimal validAmount))
                {
                    Log = "Error: Please enter a valid number";
                    MessageBox.Show(Log);
                    return;
                }

                var request = new TransactionRequest()
                {
                    Amount = validAmount,
                    Type = transactionType,
                    AccountNumber = SelectedAccount.AccountNumber
                };

                TransactionResponse transactionResponse = await Task.Run(() => _bankClient.SendRequest(request));

                if (transactionResponse.ResultStatus == TransactionResult.Success)
                {
                    Balance = transactionResponse.NewBalance;
                    SelectedAccount.Balance = transactionResponse.NewBalance;

                    var historyItem = new TransactionHistoryItem
                    {
                        Timestamp = DateTime.Now,
                        Type = transactionType.ToString(),
                        Amount = validAmount,
                        Balance = transactionResponse.NewBalance,
                        Status = "Success"
                    };

                    TransactionHistory.Insert(0, historyItem);
                    _accountHistories[SelectedAccount.AccountNumber].Insert(0, historyItem);

                    Log = $"{DateTime.Now:HH:mm:ss} - {transactionType} successful. New balance: ${transactionResponse.NewBalance:N2}";
                }
                else
                {
                    Log = $"{DateTime.Now:HH:mm:ss} - Failed: {transactionResponse.Message}";

                    var historyItem = new TransactionHistoryItem
                    {
                        Timestamp = DateTime.Now,
                        Type = transactionType.ToString(),
                        Amount = validAmount,
                        Balance = Balance,
                        Status = transactionResponse.ResultStatus.ToString()
                    };

                    TransactionHistory.Insert(0, historyItem);
                    _accountHistories[SelectedAccount.AccountNumber].Insert(0, historyItem);
                }
            }
            catch (Exception ex)
            {
                Log = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}