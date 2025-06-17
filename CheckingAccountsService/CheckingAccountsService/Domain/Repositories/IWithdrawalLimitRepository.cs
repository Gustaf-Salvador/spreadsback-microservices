using CheckingAccountsService.Domain.Entities;

namespace CheckingAccountsService.Domain.Repositories;

public interface IWithdrawalLimitRepository
{
    Task<WithdrawalLimit?> GetByUserIdAndCurrencyAsync(string userId, string currencyId);
    Task<bool> CreateAsync(WithdrawalLimit withdrawalLimit);
    Task<bool> UpdateAsync(WithdrawalLimit withdrawalLimit);
}