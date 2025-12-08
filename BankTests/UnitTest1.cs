using Xunit;
using BankServer.Data;

namespace Bank.Tests
{
    public class RepositoryTests
    {
        [Fact]
        public void SeedData_ShouldCreateMyAccount_WhenRepositoryStarts()
        {
            var repository = new BankRepository();

            var account = repository.GetByNumber("1111");

            Assert.NotNull(account);

            Assert.Equal(1000.00m, account.Balance);
        }

        [Fact]
        public void GetByNumber_ShouldReturnNull_ForUnknownAccount()
        {
            var repository = new BankRepository();

            var account = repository.GetByNumber("99999999");

            Assert.Null(account);
        }
    }
}