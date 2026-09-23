using FluentValidation;

namespace FixFlow.Api.Features.Clients.UpdateClient;

public sealed class UpdateClientRequestValidator : AbstractValidator<UpdateClientRequest>
{
    public UpdateClientRequestValidator()
    {
        Include(new ClientDetailsValidator());
    }
}
