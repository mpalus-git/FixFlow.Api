namespace FixFlow.Api.Domain.WorkOrders;

public sealed record ClientSignature(Guid PhotoId, Guid? UploadedByTechnicianId, bool IsReadableImage);
