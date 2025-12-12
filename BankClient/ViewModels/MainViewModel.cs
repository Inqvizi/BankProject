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
        private decimal _change;

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

        public decimal Change
        {
            get => _change;
            set
            {
                _change = value;
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
        private readonly System.Threading.Timer _currencyUpdateTimer;
        private decimal _balance;
        private string _log;
        private string _amountToEnter;
        private bool _isBusy;
        private AccountInfo _selectedAccount;
        private AccountInfo _transferToAccount;
        private decimal _transferAmount;
        private bool _isTransferMode;

        // Словник для зберігання історії кожного акаунта
        private Dictionary<string, ObservableCollection<TransactionHistoryItem>> _accountHistories;

        public ObservableCollection<AccountInfo> Accounts { get; set; }
        public ObservableCollection<TransactionHistoryItem> TransactionHistory { get; set; }
        public ObservableCollection<CurrencyRate> CurrencyRates { get; set; }
        public ObservableCollection<AccountInfo> TransferAccounts { get; set; }

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
                    UpdateTransferAccounts();
                }
            }
        }

        public AccountInfo TransferToAccount
        {
            get { return _transferToAccount; }
            set { _transferToAccount = value; OnPropertyChanged(); }
        }

        public decimal TransferAmount
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
                new AccountInfo { AccountNumber = "1111", DisplayName = "💼 Main Account (1111)", Balance = 1000.00m },
                new AccountInfo { AccountNumber = "2222", DisplayName = "💳 Savings Account (2222)", Balance = 500.50m },
                new AccountInfo { AccountNumber = "3333", DisplayName = "🏦 Investment Account (3333)", Balance = 999999.00m }
            };

            TransactionHistory = new ObservableCollection<TransactionHistoryItem>();
            TransferAccounts = new ObservableCollection<AccountInfo>();
            CurrencyRates = new ObservableCollection<CurrencyRate>();

            // Ініціалізація історій для кожного акаунта
            _accountHistories = new Dictionary<string, ObservableCollection<TransactionHistoryItem>>();
            foreach (var account in Accounts)
            {
                _accountHistories[account.AccountNumber] = new ObservableCollection<TransactionHistoryItem>();
            }

            // Додамо тестові дані для демонстрації
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
            RefreshCommand = new RelayCommand(_ => RefreshBalance());
            TransferCommand = new RelayCommand(async _ => await ExecuteTransfer());
            ToggleTransferModeCommand = new RelayCommand(_ => ToggleTransferMode());

            SelectedAccount = Accounts.First();

            // Завантаження курсів валют
            _ = LoadCurrencyRatesAsync();

            // Таймер для оновлення курсів кожні 60 секунд
            _currencyUpdateTimer = new System.Threading.Timer(
                async _ => await UpdateCurrencyRatesAsync(),
                null,
                TimeSpan.FromSeconds(60),
                TimeSpan.FromSeconds(60)
            );

            Log = "Ready";
        }

        private void LoadAccountHistory(string accountNumber)
        {
            TransactionHistory.Clear();

            // Завантажуємо збережену історію для цього акаунта
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

                    // EUR
                    if (rates.ContainsKey("EUR"))
                    {
                        CurrencyRates.Add(new CurrencyRate
                        {
                            CurrencyCode = "EUR",
                            CurrencyName = "Euro",
                            Rate = rates["EUR"],
                            Change = CalculateRandomChange(),
                            Flag = "🇪🇺"
                        });
                    }

                    // GBP
                    if (rates.ContainsKey("GBP"))
                    {
                        CurrencyRates.Add(new CurrencyRate
                        {
                            CurrencyCode = "GBP",
                            CurrencyName = "British Pound",
                            Rate = rates["GBP"],
                            Change = CalculateRandomChange(),
                            Flag = "🇬🇧"
                        });
                    }

                    // JPY
                    if (rates.ContainsKey("JPY"))
                    {
                        CurrencyRates.Add(new CurrencyRate
                        {
                            CurrencyCode = "JPY",
                            CurrencyName = "Japanese Yen",
                            Rate = rates["JPY"],
                            Change = CalculateRandomChange(),
                            Flag = "🇯🇵"
                        });
                    }

                    // CHF
                    if (rates.ContainsKey("CHF"))
                    {
                        CurrencyRates.Add(new CurrencyRate
                        {
                            CurrencyCode = "CHF",
                            CurrencyName = "Swiss Franc",
                            Rate = rates["CHF"],
                            Change = CalculateRandomChange(),
                            Flag = "🇨🇭"
                        });
                    }

                    if (rates.ContainsKey("CAD"))
                    {
                        CurrencyRates.Add(new CurrencyRate
                        {
                            CurrencyCode = "CAD",
                            CurrencyName = "Canadian Dollar",
                            Rate = rates["CAD"],
                            Change = CalculateRandomChange(),
                            Flag = "🇨🇦"
                        });
                    }

                    Log = $"{DateTime.Now:HH:mm:ss} - Currency rates updated";
                });
            }
            catch (Exception ex)
            {
                Log = $"Failed to load currency rates: {ex.Message}";
                LoadFallbackCurrencyRates();
            }
        }

        private async Task UpdateCurrencyRatesAsync()
        {
            try
            {
                var rates = await _currencyService.GetLatestRatesAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var currencyRate in CurrencyRates)
                    {
                        if (rates.ContainsKey(currencyRate.CurrencyCode))
                        {
                            var oldRate = currencyRate.Rate;
                            var newRate = rates[currencyRate.CurrencyCode];

                            currencyRate.Rate = newRate;
                            currencyRate.Change = newRate - oldRate;
                        }
                    }
                    OnPropertyChanged(nameof(CurrencyRates));
                });
            }
            catch
            {
                // Ігноруємо помилки при автоматичному оновленні
            }
        }

        private void LoadFallbackCurrencyRates()
        {
            CurrencyRates.Clear();
            CurrencyRates.Add(new CurrencyRate { CurrencyCode = "EUR", CurrencyName = "Euro", Rate = 0.92m, Change = 0.012m, Flag = "🇪🇺" });
            CurrencyRates.Add(new CurrencyRate { CurrencyCode = "GBP", CurrencyName = "British Pound", Rate = 0.79m, Change = -0.005m, Flag = "🇬🇧" });
            CurrencyRates.Add(new CurrencyRate { CurrencyCode = "JPY", CurrencyName = "Japanese Yen", Rate = 149.50m, Change = 0.85m, Flag = "🇯🇵" });
            CurrencyRates.Add(new CurrencyRate { CurrencyCode = "CHF", CurrencyName = "Swiss Franc", Rate = 0.88m, Change = 0.003m, Flag = "🇨🇭" });
            CurrencyRates.Add(new CurrencyRate { CurrencyCode = "CAD", CurrencyName = "Canadian Dollar", Rate = 1.36m, Change = -0.018m, Flag = "🇨🇦" });
        }

        private decimal CalculateRandomChange()
        {
            var random = new Random();
            return (decimal)(random.NextDouble() * 0.02 - 0.01); // Від -0.01 до +0.01
        }

        private void UpdateTransferAccounts()
        {
            TransferAccounts.Clear();
            foreach (var account in Accounts)
            {
                if (account.AccountNumber != SelectedAccount?.AccountNumber)
                {
                    TransferAccounts.Add(account);
                }
            }

            TransferToAccount = TransferAccounts.FirstOrDefault();
        }

        private void ToggleTransferMode()
        {
            IsTransferMode = !IsTransferMode;
            if (IsTransferMode)
            {
                UpdateTransferAccounts();
                Log = "Transfer mode activated";
            }
            else
            {
                Log = "Transfer mode deactivated";
            }
        }

        private async Task ExecuteTransfer()
        {
            if (IsBusy || SelectedAccount == null || TransferToAccount == null)
            {
                return;
            }

            if (TransferAmount <= 0)
            {
                Log = "Please enter a valid transfer amount";
                return;
            }

            if (TransferAmount > Balance)
            {
                Log = "Insufficient funds for transfer";
                return;
            }

            IsBusy = true;
            Log = "Processing transfer...";

            try
            {
                // Зняття з поточного рахунку
                var withdrawRequest = new TransactionRequest()
                {
                    Amount = TransferAmount,
                    Type = TransactionType.Withdraw,
                    AccountNumber = SelectedAccount.AccountNumber
                };

                TransactionResponse withdrawResponse = await Task.Run(() => _bankClient.SendRequest(withdrawRequest));

                if (withdrawResponse.ResultStatus == TransactionResult.Success)
                {
                    // Поповнення цільового рахунку
                    var depositRequest = new TransactionRequest()
                    {
                        Amount = TransferAmount,
                        Type = TransactionType.Deposit,
                        AccountNumber = TransferToAccount.AccountNumber
                    };

                    TransactionResponse depositResponse = await Task.Run(() => _bankClient.SendRequest(depositRequest));

                    if (depositResponse.ResultStatus == TransactionResult.Success)
                    {
                        // Оновлюємо баланси
                        Balance = withdrawResponse.NewBalance;
                        SelectedAccount.Balance = withdrawResponse.NewBalance;

                        var targetAccount = Accounts.FirstOrDefault(a => a.AccountNumber == TransferToAccount.AccountNumber);
                        if (targetAccount != null)
                        {
                            targetAccount.Balance = depositResponse.NewBalance;
                        }

                        // Додаємо до історії відправника
                        var senderHistoryItem = new TransactionHistoryItem
                        {
                            Timestamp = DateTime.Now,
                            Type = $"Transfer to {TransferToAccount.DisplayName}",
                            Amount = TransferAmount,
                            Balance = withdrawResponse.NewBalance,
                            Status = "Success"
                        };

                        TransactionHistory.Insert(0, senderHistoryItem);
                        _accountHistories[SelectedAccount.AccountNumber].Insert(0, senderHistoryItem);

                        // Додаємо до історії отримувача
                        var receiverHistoryItem = new TransactionHistoryItem
                        {
                            Timestamp = DateTime.Now,
                            Type = $"Transfer from {SelectedAccount.DisplayName}",
                            Amount = TransferAmount,
                            Balance = depositResponse.NewBalance,
                            Status = "Success"
                        };

                        _accountHistories[TransferToAccount.AccountNumber].Insert(0, receiverHistoryItem);

                        Log = $"{DateTime.Now:HH:mm:ss} - Transfer of ${TransferAmount:N2} successful";
                        TransferAmount = 0;
                        IsTransferMode = false;
                    }
                    else
                    {
                        Log = $"Transfer failed: {depositResponse.Message}";
                    }
                }
                else
                {
                    Log = $"Transfer failed: {withdrawResponse.Message}";
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

        private void RefreshBalance()
        {
            if (SelectedAccount != null)
            {
                // В реальному додатку тут був би запит на сервер для отримання поточного балансу
                Log = $"{DateTime.Now:HH:mm:ss} - Balance refreshed";
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

                    // Додаємо до історії поточного акаунта
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

                    // Додаємо невдалу транзакцію до історії
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