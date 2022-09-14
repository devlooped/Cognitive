namespace Cognitive.Speech.Azure;

record AzureSynthesisJob(string Id, string Voice, DateTime CreatedOn, string? Locale = null)
        : SynthesisJob(Id, Voice, CreatedOn, Locale)
{
    // TODO: see if these are required by Cognitive
    public string Description { get; init; } = $"{Voice} on {CreatedOn:s}";
    public string DisplayName { get; init; } = $"{Voice} on {CreatedOn:s}";
}