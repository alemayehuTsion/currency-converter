using MediatR;

namespace Currency.Application.Features.Convert;

public sealed class ConvertCurrencyHandler
    : IRequestHandler<ConvertCurrencyCommand, ConvertCurrencyResult>
{
    public Task<ConvertCurrencyResult> Handle(ConvertCurrencyCommand request, CancellationToken ct)
    {
        throw new NotImplementedException("Convert handler not implemented yet.");
    }
}
