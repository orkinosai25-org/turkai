using TurkAI.Web.Models;

namespace TurkAI.Web.Services;

public interface IUserService
{
    Task<AppUser?> FindByEmailAsync(string email);
    Task<AppUser?> FindByExternalProviderAsync(string provider, string providerId);
    Task<AppUser> CreateAsync(AppUser user);
    Task<AppUser> UpdateAsync(AppUser user);
    Task<AppUser?> ValidatePasswordAsync(string email, string password);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    string GenerateApiKey();
}
