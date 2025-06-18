using MediatR;
using SpreadsBack.CommonServices.Core.Models;

namespace SpreadsBack.CommonServices.Application.Queries;

/// <summary>
/// Interface base para queries
/// </summary>
/// <typeparam name="TResponse">Tipo de resposta da query</typeparam>
public interface IQuery<TResponse> : IRequest<ApiResponse<TResponse>>
{
}

/// <summary>
/// Query base com paginação
/// </summary>
/// <typeparam name="TResponse">Tipo de resposta da query</typeparam>
public abstract class PaginatedQuery<TResponse> : IQuery<PaginatedResult<TResponse>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public PaginationParams GetPaginationParams()
    {
        return new PaginationParams
        {
            PageNumber = PageNumber,
            PageSize = PageSize
        };
    }
}
