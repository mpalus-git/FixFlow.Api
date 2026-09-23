using System.ComponentModel;

namespace FixFlow.Api.Features.Auth.Refresh;

[Description("Refresh token received from the last login or refresh.")]
public sealed record RefreshRequest(
    [property: Description("Refresh token to exchange. Each refresh token can be used only once.")] string RefreshToken);
