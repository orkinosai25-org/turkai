using System.Security.Cryptography;
using System.Text;
using TurkAI.Web.Models;

namespace TurkAI.Web.Services;

/// <summary>
/// In-memory user store for development / MVP. Replace with a persistent
/// store (e.g. EF Core + SQL) for production deployments.
/// </summary>
public class InMemoryUserService : IUserService
{
    private readonly List<AppUser> _users = [];
    private readonly Lock _lock = new();

    public Task<AppUser?> FindByEmailAsync(string email)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(u =>
                string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(user);
        }
    }

    public Task<AppUser?> FindByExternalProviderAsync(string provider, string providerId)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(u =>
                u.ExternalProvider == provider && u.ExternalProviderId == providerId);
            return Task.FromResult(user);
        }
    }

    public Task<AppUser> CreateAsync(AppUser user)
    {
        lock (_lock)
        {
            _users.Add(user);
        }
        return Task.FromResult(user);
    }

    public Task<AppUser> UpdateAsync(AppUser user)
    {
        // In-memory store: object reference is already updated; no-op needed
        return Task.FromResult(user);
    }

    public async Task<AppUser?> ValidatePasswordAsync(string email, string password)
    {
        var user = await FindByEmailAsync(email);
        if (user is null || user.PasswordHash is null) return null;
        return VerifyPassword(password, user.PasswordHash) ? user : null;
    }

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
    }

    public bool VerifyPassword(string password, string hash)
    {
        var parts = hash.Split(':');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var storedHash = Convert.FromBase64String(parts[1]);
        var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
    }

    public string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return "tk_" + Convert.ToBase64String(bytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")[..40];
    }
}
