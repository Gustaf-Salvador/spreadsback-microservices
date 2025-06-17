using CheckingAccountsService.Domain.Entities;
using CheckingAccountsService.Domain.Repositories;
using Dapper;
using Npgsql;
using System.Data;

namespace CheckingAccountsService.Infrastructure.Persistence.Repositories;

public class CheckingAccountRepository : ICheckingAccountRepository
{
    private readonly string _connectionString;

    public CheckingAccountRepository(DatabaseSettings settings)
    {
        _connectionString = settings.ConnectionString;
    }

    private IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task<CheckingAccount?> GetByUserIdAndCurrencyAsync(string userId, string currencyId)
    {
        using var connection = CreateConnection();
        var sql = @"
            SELECT id, user_id, currency_id, balance, created_at, updated_at
            FROM checking_accounts
            WHERE user_id = @UserId AND currency_id = @CurrencyId";

        var parameters = new { UserId = userId, CurrencyId = currencyId };
        var result = await connection.QueryFirstOrDefaultAsync<CheckingAccountDto>(sql, parameters);

        return result == null ? null : MapToEntity(result);
    }

    public async Task<IEnumerable<CheckingAccount>> GetAllByUserIdAsync(string userId)
    {
        using var connection = CreateConnection();
        var sql = @"
            SELECT id, user_id, currency_id, balance, created_at, updated_at
            FROM checking_accounts
            WHERE user_id = @UserId";

        var parameters = new { UserId = userId };
        var results = await connection.QueryAsync<CheckingAccountDto>(sql, parameters);

        return results.Select(MapToEntity).ToList();
    }

    public async Task<bool> UpdateAsync(CheckingAccount checkingAccount)
    {
        using var connection = CreateConnection();
        var sql = @"
            UPDATE checking_accounts
            SET balance = @Balance, updated_at = @UpdatedAt
            WHERE id = @Id AND user_id = @UserId AND currency_id = @CurrencyId";

        var parameters = new
        {
            Id = checkingAccount.Id,
            UserId = checkingAccount.UserId,
            CurrencyId = checkingAccount.CurrencyId,
            Balance = checkingAccount.Balance,
            UpdatedAt = checkingAccount.UpdatedAt
        };

        var rowsAffected = await connection.ExecuteAsync(sql, parameters);
        return rowsAffected > 0;
    }

    public async Task<bool> CreateAsync(CheckingAccount checkingAccount)
    {
        using var connection = CreateConnection();
        var sql = @"
            INSERT INTO checking_accounts (id, user_id, currency_id, balance, created_at, updated_at)
            VALUES (@Id, @UserId, @CurrencyId, @Balance, @CreatedAt, @UpdatedAt)";

        var parameters = new
        {
            Id = checkingAccount.Id,
            UserId = checkingAccount.UserId,
            CurrencyId = checkingAccount.CurrencyId,
            Balance = checkingAccount.Balance,
            CreatedAt = checkingAccount.CreatedAt,
            UpdatedAt = checkingAccount.UpdatedAt
        };

        var rowsAffected = await connection.ExecuteAsync(sql, parameters);
        return rowsAffected > 0;
    }

    private CheckingAccount MapToEntity(CheckingAccountDto dto)
    {
        // Using reflection to set private properties for Entity reconstruction
        var entity = (CheckingAccount)Activator.CreateInstance(typeof(CheckingAccount), true)!;
        
        typeof(CheckingAccount).GetProperty("Id")!.SetValue(entity, dto.Id);
        typeof(CheckingAccount).GetProperty("UserId")!.SetValue(entity, dto.UserId);
        typeof(CheckingAccount).GetProperty("CurrencyId")!.SetValue(entity, dto.CurrencyId);
        typeof(CheckingAccount).GetProperty("Balance")!.SetValue(entity, dto.Balance);
        typeof(CheckingAccount).GetProperty("CreatedAt")!.SetValue(entity, dto.CreatedAt);
        typeof(CheckingAccount).GetProperty("UpdatedAt")!.SetValue(entity, dto.UpdatedAt);
        
        return entity;
    }

    // DTO for Dapper mapping
    private class CheckingAccountDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string CurrencyId { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}