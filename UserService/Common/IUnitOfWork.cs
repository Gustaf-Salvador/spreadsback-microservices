using UserService.Models;

namespace UserService.Common;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    
    void AddDomainEvent(DomainEvent domainEvent);
    IReadOnlyList<DomainEvent> GetDomainEvents();
    void ClearDomainEvents();
}

public interface IUserRepository
{
    Task<OperationResult<User>> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<OperationResult<User>> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<OperationResult<IEnumerable<User>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OperationResult<bool>> ExistsAsync(string id, CancellationToken cancellationToken = default);
    Task<OperationResult<bool>> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<OperationResult> AddAsync(User user, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
