namespace Cognitive.Speech;

public enum Gender { Female, Male }

public record Voice(string Name, string DisplayName, string Locale, Gender Gender);

public record SynthesisJob(string Id, string Voice, DateTime CreatedOn, string? Locale = default);
