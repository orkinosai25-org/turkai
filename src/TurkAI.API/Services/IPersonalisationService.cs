using TurkAI.Shared.Models;

namespace TurkAI.API.Services;

public interface IPersonalisationService
{
    Task<PersonalisedRecommendation> GetRecommendationsAsync(PersonalisationContext context, CancellationToken cancellationToken = default);
    Task RecordFeedbackAsync(string userId, string destination, double reward, CancellationToken cancellationToken = default);
}
