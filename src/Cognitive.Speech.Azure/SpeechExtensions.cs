namespace Cognitive.Speech.Azure;

public static class SpeechExtensions
{
    public static SynthesisJob WithDisplayName(this SynthesisJob job, string displayName)
        => job is AzureSynthesisJob azureJob ?
            azureJob with { DisplayName = displayName } :
            throw new ArgumentException("Invalid job type.", nameof(job));

    public static SynthesisJob WithDescription(this SynthesisJob job, string description)
        => job is AzureSynthesisJob azureJob ?
            azureJob with { Description = description } :
            throw new ArgumentException("Invalid job type.", nameof(job));
}
