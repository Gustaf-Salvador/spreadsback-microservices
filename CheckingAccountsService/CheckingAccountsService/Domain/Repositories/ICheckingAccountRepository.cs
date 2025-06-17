using CheckingAccountsService.Domain.Entities;

namespace CheckingAccountsService.Domain.Repositories;

public interface ICheckingAccountRepository
{
    Task<CheckingAccount?> GetByUserIdAndCurrencyAsync(string userId, string currencyId);
    Task<IEnumerable<CheckingAccount>> GetAllByUserIdAsync(string userId);
    Task<bool> UpdateAsync(CheckingAccount checkingAccount);
    Task<bool> CreateAsync(CheckingAccount checkingAccount);
}