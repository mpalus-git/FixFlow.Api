namespace FixFlow.Api.Common.Email;

public sealed record EmailMessage(IReadOnlyList<string> Recipients, string Subject, string Body);
