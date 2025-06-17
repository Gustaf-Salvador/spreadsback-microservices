using CheckingAccountsService.Domain.Entities;
using CheckingAccountsService.Domain.Repositories;
using Dapper;
using Npgsql;
using System.Data;

namespace CheckingAccountsService.Infrastructure.Persistence.Repositories;

public class WithdrawalLimitRepository : IWithdrawalLimitRepository
{
    private readonly string _connectionString;

    public WithdrawalLimitRepository(DatabaseSettings settings)
    {
        _connectionString = settings.ConnectionString;
    }

    private IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task<WithdrawalLimit?> GetByUserIdAndCurrencyAsync(string userId, string currencyId)
    {
        using var connection = CreateConnection();
        var sql = @"
            SELECT id, user_id, currency_id, daily_limit, monthly_limit, created_at, updated_at
            FROM withdrawal_limits
            WHERE user_id = @UserId AND currency_id = @CurrencyId";

        var parameters = new { UserId = userId, CurrencyId = currencyId };
        var result = await connection.QueryFirstOrDefaultAsync<WithdrawalLimitDto>(sql, parameters);

        return result == null ? null : MapToEntity(result);
    }

    public async Task<bool> CreateAsync(WithdrawalLimit withdrawalLimit)
    {
        using var connection = CreateConnection();
        var sql = @"
            INSERT INTO withdrawal_limits (id, user_id, currency_id, daily_limit, monthly_limit, created_at, updated_at)
            VALUES (@Id, @UserId, @CurrencyId, @DailyLimit, @MonthlyLimit, @CreatedAt, @UpdatedAt)";

        var parameters = new
        {
            Id = withdrawalLimit.Id,
            UserId = withdrawalLimit.UserId,
            CurrencyId = withdrawalLimit.CurrencyId,
            DailyLimit = withdrawalLimit.DailyLimit,
            MonthlyLimit = withdrawalLimit.MonthlyLimit,
            CreatedAt = withdrawalLimit.CreatedAt,
            UpdatedAt = withdrawalLimit.UpdatedAt
        };

        var rowsAffected = await connection.ExecuteAsync(sql, parameters);
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateAsync(WithdrawalLimit withdrawalLimit)
    {
        using var connection = CreateConnection();
        var sql = @"
            UPDATE withdrawal_limits
            SET daily_limit = @DailyLimit, monthly_limit = @MonthlyLimit, updated_at = @UpdatedAt
            WHERE id = @Id AND user_id = @UserId AND currency_id = @CurrencyId";

        var parameters = new
        {
            Id = withdrawalLimit.Id,
            UserId = withdrawalLimit.UserId,
            CurrencyId = withdrawalLimit.CurrencyId,
            DailyLimit = withdrawalLimit.DailyLimit,
            MonthlyLimit = withdrawalLimit.MonthlyLimit,
            UpdatedAt = withdrawalLimit.UpdatedAt
        };

        var rowsAffected = await connection.ExecuteAsync(sql, parameters);
        return rowsAffected > 0;
    }

    private WithdrawalLimit MapToEntity(WithdrawalLimitDto dto)
    {
        // Using reflection to set private properties for Entity reconstruction
        var entity = (WithdrawalLimit)Activator.CreateInstance(typeof(WithdrawalLimit), true)!;
        
        typeof(WithdrawalLimit).GetProperty("Id")!.SetValue(entity, dto.Id);
        typeof(WithdrawalLimit).GetProperty("UserId")!.SetValue(entity, dto.UserId);
        typeof(WithdrawalLimit).GetProperty("CurrencyId")!.SetValue(entity, dto.CurrencyId);
        typeof(WithdrawalLimit).GetProperty("DailyLimit")!.SetValue(entity, dto.DailyLimit);
        typeof(WithdrawalLimit).GetProperty("MonthlyLimit")!.SetValue(entity, dto.MonthlyLimit);
        typeof(WithdrawalLimit).GetProperty("CreatedAt")!.SetValue(entity, dto.CreatedAt);
        typeof(WithdrawalLimit).GetProperty("UpdatedAt")!.SetValue(entity, dto.UpdatedAt);
        
        return entity;
    }

    // DTO for Dapper mapping
    private class WithdrawalLimitDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string CurrencyId { get; set; } = string.Empty;
        public decimal DailyLimit { get; set; }
        public decimal MonthlyLimit { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}