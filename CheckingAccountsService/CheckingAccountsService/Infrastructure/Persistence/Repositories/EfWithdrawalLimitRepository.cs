using CheckingAccountsService.Domain.Entities;
using CheckingAccountsService.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CheckingAccountsService.Infrastructure.Persistence.Repositories;

public class EfWithdrawalLimitRepository : IWithdrawalLimitRepository
{
    private readonly ApplicationDbContext _context;

    public EfWithdrawalLimitRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WithdrawalLimit?> GetByUserIdAndCurrencyAsync(string userId, string currencyId)
    {
        return await _context.WithdrawalLimits
            .FirstOrDefaultAsync(w => w.UserId == userId && w.CurrencyId == currencyId);
    }

    public async Task<bool> CreateAsync(WithdrawalLimit withdrawalLimit)
    {
        await _context.WithdrawalLimits.AddAsync(withdrawalLimit);
        var rowsAffected = await _context.SaveChangesAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateAsync(WithdrawalLimit withdrawalLimit)
    {
        _context.WithdrawalLimits.Update(withdrawalLimit);
        var rowsAffected = await _context.SaveChangesAsync();
        return rowsAffected > 0;
    }
}