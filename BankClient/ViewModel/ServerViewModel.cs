using System.Collections.ObjectModel;
using System.Windows.Input;
using BankClient.Commands; // RelayCommand
using BankServer.Data;
using BankClient.ViewModels; // BaseViewModel
using BankShared.Models;

namespace BankServer.ViewModels
{
    public class ServerViewModel : BaseViewModel
    {
        private readonly BankRepository _repository;

        private ObservableCollection<BankAccount> _accounts;
        public ObservableCollection<BankAccount> Accounts
        {
            get => _accounts;
            set
            {
                _accounts = value;
                OnPropertyChanged();
            }
        }

        private string _serverStatus;
        public string ServerStatus
        {
            get => _serverStatus;
            set
            {
                _serverStatus = value;
                OnPropertyChanged();
            }
        }

        public ICommand StartServerCommand { get; }
        public ICommand StopServerCommand { get; }

        public ServerViewModel()
        {
            _repository = new BankRepository();
            Accounts = new ObservableCollection<BankAccount>(_repository.GetAll());
            ServerStatus = "Server stopped";

            StartServerCommand = new RelayCommand(StartServer);
            StopServerCommand = new RelayCommand(StopServer);
        }

        private void StartServer(object? parameter)
        {
            ServerStatus = "Server running";

            // Якщо треба — оновлення акаунтів
            Accounts = new ObservableCollection<BankAccount>(_repository.GetAll());
        }

        private void StopServer(object? parameter)
        {
            ServerStatus = "Server stopped";
        }
    }
}
