using FluentValidation;

namespace FixFlow.Api.Features.Clients.CreateClient;

public sealed class CreateClientRequestValidator : AbstractValidator<CreateClientRequest>
{
    public CreateClientRequestValidator()
    {
        Include(new ClientDetailsValidator());
    }
}
