using CheckingAccountsService.Domain.Entities;
using CheckingAccountsService.Domain.Repositories;
using Dapper;
using Npgsql;
using System.Data;

namespace CheckingAccountsService.Infrastructure.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly string _connectionString;

    public TransactionRepository(DatabaseSettings settings)
    {
        _connectionString = settings.ConnectionString;
    }

    private IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task<IEnumerable<Transaction>> GetByUserIdAsync(
        string userId, 
        string? currencyId = null, 
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        int? skip = null, 
        int? top = null)
    {
        using var connection = CreateConnection();
        
        var sqlBuilder = new System.Text.StringBuilder();
        sqlBuilder.Append(@"
            SELECT id, checking_account_id, user_id, currency_id, amount, type, description, created_at
            FROM transactions
            WHERE user_id = @UserId");

        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId);

        if (!string.IsNullOrEmpty(currencyId))
        {
            sqlBuilder.Append(" AND currency_id = @CurrencyId");
            parameters.Add("CurrencyId", currencyId);
        }

        if (startDate.HasValue)
        {
            sqlBuilder.Append(" AND created_at >= @StartDate");
            parameters.Add("StartDate", startDate.Value);
        }

        if (endDate.HasValue)
        {
            sqlBuilder.Append(" AND created_at < @EndDate");
            parameters.Add("EndDate", endDate.Value);
        }

        sqlBuilder.Append(" ORDER BY created_at DESC");

        if (skip.HasValue && top.HasValue)
        {
            sqlBuilder.Append(" LIMIT @Top OFFSET @Skip");
            parameters.Add("Skip", skip.Value);
            parameters.Add("Top", top.Value);
        }

        var results = await connection.QueryAsync<TransactionDto>(sqlBuilder.ToString(), parameters);
        return results.Select(MapToEntity).ToList();
    }

    public async Task<bool> CreateAsync(Transaction transaction)
    {
        using var connection = CreateConnection();
        var sql = @"
            INSERT INTO transactions (id, checking_account_id, user_id, currency_id, amount, type, description, created_at)
            VALUES (@Id, @CheckingAccountId, @UserId, @CurrencyId, @Amount, @Type, @Description, @CreatedAt)";

        var parameters = new
        {
            Id = transaction.Id,
            CheckingAccountId = transaction.CheckingAccountId,
            UserId = transaction.UserId,
            CurrencyId = transaction.CurrencyId,
            Amount = transaction.Amount,
            Type = transaction.Type.ToString(),
            Description = transaction.Description,
            CreatedAt = transaction.CreatedAt
        };

        var rowsAffected = await connection.ExecuteAsync(sql, parameters);
        return rowsAffected > 0;
    }

    public async Task<decimal> GetTotalWithdrawalsForPeriodAsync(string userId, string currencyId, DateTime startDate, DateTime endDate)
    {
        using var connection = CreateConnection();
        var sql = @"
            SELECT COALESCE(SUM(amount), 0)
            FROM transactions
            WHERE user_id = @UserId 
              AND currency_id = @CurrencyId 
              AND type = 'Withdrawal' 
              AND created_at >= @StartDate 
              AND created_at < @EndDate";

        var parameters = new
        {
            UserId = userId,
            CurrencyId = currencyId,
            StartDate = startDate,
            EndDate = endDate
        };

        return await connection.ExecuteScalarAsync<decimal>(sql, parameters);
    }

    private Transaction MapToEntity(TransactionDto dto)
    {
        // Using reflection to set private properties for Entity reconstruction
        var entity = (Transaction)Activator.CreateInstance(typeof(Transaction), true)!;
        
        typeof(Transaction).GetProperty("Id")!.SetValue(entity, dto.Id);
        typeof(Transaction).GetProperty("CheckingAccountId")!.SetValue(entity, dto.CheckingAccountId);
        typeof(Transaction).GetProperty("UserId")!.SetValue(entity, dto.UserId);
        typeof(Transaction).GetProperty("CurrencyId")!.SetValue(entity, dto.CurrencyId);
        typeof(Transaction).GetProperty("Amount")!.SetValue(entity, dto.Amount);
        typeof(Transaction).GetProperty("Type")!.SetValue(entity, Enum.Parse<TransactionType>(dto.Type));
        typeof(Transaction).GetProperty("Description")!.SetValue(entity, dto.Description);
        typeof(Transaction).GetProperty("CreatedAt")!.SetValue(entity, dto.CreatedAt);
        
        return entity;
    }

    // DTO for Dapper mapping
    private class TransactionDto
    {
        public Guid Id { get; set; }
        public Guid CheckingAccountId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string CurrencyId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}