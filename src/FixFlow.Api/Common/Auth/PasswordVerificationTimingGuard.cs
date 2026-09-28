using System.Security.Cryptography;
using FixFlow.Api.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FixFlow.Api.Common.Auth;

public sealed class PasswordVerificationTimingGuard
{
    private static readonly ApplicationUser UnknownUser = new();

    private readonly PasswordHasher<ApplicationUser> _passwordHasher;
    private readonly string _unknownUserPasswordHash;

    public PasswordVerificationTimingGuard(IOptions<PasswordHasherOptions> passwordHasherOptions)
    {
        _passwordHasher = new PasswordHasher<ApplicationUser>(passwordHasherOptions);
        _unknownUserPasswordHash = _passwordHasher.HashPassword(UnknownUser, RandomNumberGenerator.GetHexString(32));
    }

    public void SimulatePasswordVerification(string password) =>
        _passwordHasher.VerifyHashedPassword(UnknownUser, _unknownUserPasswordHash, password);
}
