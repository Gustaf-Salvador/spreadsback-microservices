using CheckingAccountsService.Domain.Repositories;
using CheckingAccountsService.Domain.Services;
using CheckingAccountsService.Infrastructure.Persistence;
using CheckingAccountsService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpreadsBack.CommonServices.Infrastructure.Configuration;
using System.Reflection;

namespace CheckingAccountsService.API;

public static class DependencyInjection
{
    public static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();

        // Add all common services (MediatR, FluentValidation, AWS, etc.)
        services.AddCommonServices(Assembly.GetExecutingAssembly());

        // Database configuration
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? 
            "Host=localhost;Port=5432;Database=checking_accounts;Username=postgres;Password=postgres";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Register specific repositories
        services.AddScoped<ICheckingAccountRepository, EfCheckingAccountRepository>();
        services.AddScoped<ITransactionRepository, EfTransactionRepository>();
        services.AddScoped<IWithdrawalLimitRepository, EfWithdrawalLimitRepository>();

        // Register domain services specific to this microservice
        services.AddScoped<WithdrawalService>();

        return services;
    }
}