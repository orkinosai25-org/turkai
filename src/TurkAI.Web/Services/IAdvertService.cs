using TurkAI.Web.Models;

namespace TurkAI.Web.Services;

public interface IAdvertService
{
    Task<IReadOnlyList<Advertisement>> GetAllAsync();
    Task<IReadOnlyList<Advertisement>> GetActiveByPositionAsync(AdvertPosition position);
    Task<Advertisement?> GetByIdAsync(string id);
    Task<Advertisement> CreateAsync(Advertisement ad);
    Task<Advertisement> UpdateAsync(Advertisement ad);
    Task DeleteAsync(string id);
}
