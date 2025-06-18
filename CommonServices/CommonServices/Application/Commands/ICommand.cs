using MediatR;
using SpreadsBack.CommonServices.Core.Models;

namespace SpreadsBack.CommonServices.Application.Commands;

/// <summary>
/// Interface base para commands
/// </summary>
/// <typeparam name="TResponse">Tipo de resposta do command</typeparam>
public interface ICommand<TResponse> : IRequest<ApiResponse<TResponse>>
{
}

/// <summary>
/// Interface base para commands sem retorno específico
/// </summary>
public interface ICommand : IRequest<ApiResponse<bool>>
{
}
