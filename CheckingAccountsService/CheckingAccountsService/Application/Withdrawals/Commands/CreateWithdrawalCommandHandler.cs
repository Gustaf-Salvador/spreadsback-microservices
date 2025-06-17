using CheckingAccountsService.Application.Common.DTOs;
using CheckingAccountsService.Application.Common.Models;
using CheckingAccountsService.Domain.Services;
using FluentValidation;
using MediatR;

namespace CheckingAccountsService.Application.Withdrawals.Commands;

public class CreateWithdrawalCommandValidator : AbstractValidator<CreateWithdrawalCommand>
{
    public CreateWithdrawalCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required");
        RuleFor(x => x.CurrencyId).NotEmpty().WithMessage("Currency ID is required");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero");
        RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required");
    }
}

public class CreateWithdrawalCommandHandler : IRequestHandler<CreateWithdrawalCommand, ApiResponse<WithdrawalDto>>
{
    private readonly WithdrawalService _withdrawalService;
    private readonly IValidator<CreateWithdrawalCommand> _validator;

    public CreateWithdrawalCommandHandler(
        WithdrawalService withdrawalService,
        IValidator<CreateWithdrawalCommand> validator)
    {
        _withdrawalService = withdrawalService;
        _validator = validator;
    }

    public async Task<ApiResponse<WithdrawalDto>> Handle(CreateWithdrawalCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate request
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ApiResponse<WithdrawalDto>.ErrorResponse("Validation failed", errors);
            }

            // Process the withdrawal
            var transaction = await _withdrawalService.ProcessWithdrawalAsync(
                request.UserId,
                request.CurrencyId,
                request.Amount,
                request.Description);

            // Map to DTO
            var withdrawalDto = new WithdrawalDto
            {
                Id = transaction.Id,
                UserId = transaction.UserId,
                CurrencyId = transaction.CurrencyId,
                Amount = transaction.Amount,
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt
            };

            return ApiResponse<WithdrawalDto>.SuccessResponse(withdrawalDto, "Withdrawal processed successfully");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<WithdrawalDto>.ErrorResponse(ex.Message);
        }
        catch (Exception ex)
        {
            return ApiResponse<WithdrawalDto>.ErrorResponse($"Error processing withdrawal: {ex.Message}");
        }
    }
}