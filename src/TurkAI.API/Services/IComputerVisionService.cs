using TurkAI.Shared.Models;

namespace TurkAI.API.Services;

public interface IComputerVisionService
{
    Task<ImageAnalysisResult> AnalyseImageAsync(ImageAnalysisRequest request, CancellationToken cancellationToken = default);
}
