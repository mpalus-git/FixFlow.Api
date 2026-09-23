using System.ComponentModel;
using FixFlow.Api.Domain.Clients;
using FluentValidation;

namespace FixFlow.Api.Features.Clients;

[Description("Postal address of a client.")]
public sealed record ClientAddress(
    [property: Description("Street name.")] string Street,
    [property: Description("Building number, optionally with a letter or apartment, for example 12A or 12/4.")] string BuildingNumber,
    [property: Description("Polish postal code in the NN-NNN format.")] string PostalCode,
    [property: Description("City.")] string City)
{
    public Address ToDomain() => new(Street, BuildingNumber, PostalCode, City);

    public static ClientAddress FromDomain(Address address) =>
        new(address.Street, address.BuildingNumber, address.PostalCode, address.City);
}

public interface IClientDetails
{
    string Name { get; }

    ClientAddress Address { get; }

    string ContactPerson { get; }

    string Phone { get; }

    string? Email { get; }
}

public sealed class ClientDetailsValidator : AbstractValidator<IClientDetails>
{
    public ClientDetailsValidator()
    {
        RuleFor(details => details.Name).NotEmpty().MaximumLength(200);
        RuleFor(details => details.Address).NotNull().SetValidator(new ClientAddressValidator());
        RuleFor(details => details.ContactPerson).NotEmpty().MaximumLength(200);
        RuleFor(details => details.Phone).NotEmpty().Matches(@"^\+?[0-9 \-]{9,20}$")
            .WithMessage("Phone must have 9 to 20 digits, spaces or dashes and may start with +.");
        RuleFor(details => details.Email).EmailAddress().MaximumLength(256).When(details => !string.IsNullOrEmpty(details.Email));
    }
}

public sealed class ClientAddressValidator : AbstractValidator<ClientAddress>
{
    public ClientAddressValidator()
    {
        RuleFor(address => address.Street).NotEmpty().MaximumLength(200);
        RuleFor(address => address.BuildingNumber).NotEmpty().MaximumLength(20);
        RuleFor(address => address.PostalCode).NotEmpty().Matches(@"^\d{2}-\d{3}$")
            .WithMessage("Postal code must have the NN-NNN format.");
        RuleFor(address => address.City).NotEmpty().MaximumLength(100);
    }
}
