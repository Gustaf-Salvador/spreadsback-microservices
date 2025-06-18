using CheckingAccountsService.Domain.Entities;
using CheckingAccountsService.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using SpreadsBack.CommonServices.Infrastructure.Repositories;

namespace CheckingAccountsService.Infrastructure.Persistence.Repositories;

public class EfCheckingAccountRepository : BaseRepository<CheckingAccount>, ICheckingAccountRepository
{
    public EfCheckingAccountRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<CheckingAccount?> GetByUserIdAndCurrencyAsync(string userId, string currencyId)
    {
        return await DbSet
            .FirstOrDefaultAsync(a => a.UserId == userId && a.CurrencyId == currencyId);
    }

    public async Task<IEnumerable<CheckingAccount>> GetAllByUserIdAsync(string userId)
    {
        return await DbSet
            .Where(a => a.UserId == userId)
            .ToListAsync();
    }
}