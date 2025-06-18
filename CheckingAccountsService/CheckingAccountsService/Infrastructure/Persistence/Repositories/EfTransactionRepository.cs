using CheckingAccountsService.Domain.Entities;
using CheckingAccountsService.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CheckingAccountsService.Infrastructure.Persistence.Repositories;

public class EfTransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;

    public EfTransactionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Transaction>> GetByUserIdAsync(
        string userId, 
        string? currencyId = null, 
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        int? skip = null, 
        int? top = null)
    {
        IQueryable<Transaction> query = _context.Transactions
            .Where(t => t.UserId == userId);

        if (!string.IsNullOrEmpty(currencyId))
        {
            query = query.Where(t => t.CurrencyId == currencyId);
        }

        if (startDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt < endDate.Value);
        }

        // Order by creation date descending
        query = query.OrderByDescending(t => t.CreatedAt);

        if (skip.HasValue)
        {
            query = query.Skip(skip.Value);
        }

        if (top.HasValue)
        {
            query = query.Take(top.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<bool> CreateAsync(Transaction transaction)
    {
        await _context.Transactions.AddAsync(transaction);
        var rowsAffected = await _context.SaveChangesAsync();
        return rowsAffected > 0;
    }

    public async Task<decimal> GetTotalWithdrawalsForPeriodAsync(string userId, string currencyId, DateTime startDate, DateTime endDate)
    {
        return await _context.Transactions
            .Where(t => t.UserId == userId
                && t.CurrencyId == currencyId
                && t.Type == TransactionType.Withdrawal
                && t.CreatedAt >= startDate
                && t.CreatedAt < endDate)
            .SumAsync(t => t.Amount);
    }
}