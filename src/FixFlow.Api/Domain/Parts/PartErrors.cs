using ErrorOr;

namespace FixFlow.Api.Domain.Parts;

public static class PartErrors
{
    public static readonly Error NotFound = Error.NotFound("Part.NotFound", "Part was not found.");

    public static readonly Error Archived = Error.Conflict("Part.Archived", "Archived part cannot be modified or used in service entries.");

    public static readonly Error InsufficientStock = Error.Conflict("Part.InsufficientStock", "There are not enough units of the part in stock.");

    public static readonly Error DuplicateCatalogNumber = Error.Conflict("Part.DuplicateCatalogNumber", "A part with this catalog number already exists.");
}
