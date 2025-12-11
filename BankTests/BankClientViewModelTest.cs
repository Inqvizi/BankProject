using Xunit;
using BankServer.ViewModels;
using BankServer.Data;
using BankShared.Models;
using System.Collections.ObjectModel;

namespace BankTests
{
    public class ServerViewModelTests
    {
        [Fact]
        public void Constructor_ShouldInitializeAccountsAndStatus()
        {
            // Arrange & Act
            var vm = new ServerViewModel();

            // Assert
            Assert.NotNull(vm.Accounts);
            Assert.Equal(3, vm.Accounts.Count); // бо BankRepository SeedData() додає 3 акаунти
            Assert.Equal("Server stopped", vm.ServerStatus);
        }

        [Fact]
        public void StartServerCommand_ShouldSetStatusToRunningAndUpdateAccounts()
        {
            // Arrange
            var vm = new ServerViewModel();

            // Act
            vm.StartServerCommand.Execute(null);

            // Assert
            Assert.Equal("Server running", vm.ServerStatus);
            Assert.NotNull(vm.Accounts);
            Assert.Equal(3, vm.Accounts.Count);
        }

        [Fact]
        public void StopServerCommand_ShouldSetStatusToStopped()
        {
            // Arrange
            var vm = new ServerViewModel();

            // Act
            vm.StopServerCommand.Execute(null);

            // Assert
            Assert.Equal("Server stopped", vm.ServerStatus);
        }
    }
}
