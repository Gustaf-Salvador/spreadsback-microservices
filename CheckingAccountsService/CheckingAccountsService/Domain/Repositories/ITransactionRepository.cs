using CheckingAccountsService.Domain.Entities;

namespace CheckingAccountsService.Domain.Repositories;

public interface ITransactionRepository
{
    Task<IEnumerable<Transaction>> GetByUserIdAsync(
        string userId, 
        string? currencyId = null, 
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        int? skip = null, 
        int? top = null);
        
    Task<bool> CreateAsync(Transaction transaction);
    Task<decimal> GetTotalWithdrawalsForPeriodAsync(string userId, string currencyId, DateTime startDate, DateTime endDate);
}