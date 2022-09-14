namespace Cognitive.Speech;

public interface ISpeechEngine
{
    Task<SynthesisJob> CreateJobAsync(string voice, Func<SynthesisJob, SynthesisJob>? configure = null, CancellationToken cancellation = default);

    Task<IReadOnlyCollection<Voice>> GetVoicesAsync(CancellationToken cancellation = default);
}
