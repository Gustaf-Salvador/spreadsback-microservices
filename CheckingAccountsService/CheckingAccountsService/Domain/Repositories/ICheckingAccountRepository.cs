using CheckingAccountsService.Domain.Entities;
using SpreadsBack.CommonServices.Infrastructure.Repositories;

namespace CheckingAccountsService.Domain.Repositories;

public interface ICheckingAccountRepository : IBaseRepository<CheckingAccount>
{
    Task<CheckingAccount?> GetByUserIdAndCurrencyAsync(string userId, string currencyId);
    Task<IEnumerable<CheckingAccount>> GetAllByUserIdAsync(string userId);
}