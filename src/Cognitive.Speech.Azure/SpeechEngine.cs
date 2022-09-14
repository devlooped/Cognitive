using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Configuration;

namespace Cognitive.Speech.Azure;

public partial class SpeechEngine : ISpeechEngine
{
    readonly string key;
    readonly string region;
    readonly Lazy<HttpClient> http;

    public SpeechEngine(string key, string region, IHttpClientFactory httpFactory)
    {
        this.key = key;
        this.region = region;
        http = new Lazy<HttpClient>(() =>
        {
            var http = httpFactory.CreateClient();
            http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Cognitive.Speech.Azure");
            http.DefaultRequestHeaders.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", this.key);
            return http;
        });
    }

    public static ISpeechEngine Create(IConfiguration configuration, IHttpClientFactory httpFactory)
    {
        var key = configuration["Azure:SpeechKey"];
        var region = configuration["Azure:SpeechRegion"];

        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Required 'Azure:SpeechKey' configuration missing.", nameof(configuration));

        if (string.IsNullOrEmpty(region))
            throw new ArgumentException("Required 'Azure:SpeechRegion' configuration missing.", nameof(configuration));

        return Create(key, region, httpFactory);
    }

    public static ISpeechEngine Create(string key, string region, IHttpClientFactory httpFactory)
        => new SpeechEngine(key, region, httpFactory);

    public Task<SynthesisJob> CreateJobAsync(string voice, Func<SynthesisJob, SynthesisJob>? configure = default, CancellationToken cancellation = default)
    {
        var job = new SynthesisJob(Guid.NewGuid().ToString().ToLowerInvariant(), voice, DateTime.UtcNow);
        if (configure != null)
            job = configure(job);

        // post
        return Task.FromResult(job);
    }

    public async Task<IReadOnlyCollection<Voice>> GetVoicesAsync(CancellationToken cancellation = default)
    {
        var response = await http.Value.GetAsync($"https://{region}.customvoice.api.speech.microsoft.com/api/texttospeech/v3.0/longaudiosynthesis/voices");

        response.EnsureSuccessStatusCode();

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters =
            {
                new JsonStringEnumConverter(),
                new VoiceConverter(),
            }
        };

        var json = await response.Content.ReadFromJsonAsync<Dictionary<string, Voice[]>>(
            options, cancellation);

        if (json == null)
            throw new InvalidOperationException("Failed to retrieve voices.");

        return json["values"].Where(x => x.Name.Contains("Neural")).ToArray();
    }

    class VoiceConverter : JsonConverter<Voice>
    {
        public override Voice? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            /*
            "locale": "nb-NO",
            "voiceName": "nb-NO-IselinNeural",
            "description": "{\"Version\":\"20211130133950\"}",
            "gender": "Female",
            "createdDateTime": "2021-11-30T15:18:37.883Z",
            "properties": {
                "publicAvailable": true
            }
            */
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            string? name = default;
            string? locale = default;
            Gender? gender = default;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (name != null && locale != null && gender != null)
                        return new Voice(name, name.Split('-')[^1].Replace("Neural", ""), locale, gender.Value);
                    else
                        return null;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();

                var prop = reader.GetString();
                reader.Read();

                if (prop == "locale")
                    locale = reader.GetString();
                else if (prop == "voiceName")
                    name = reader.GetString();
                else if (prop == "gender" &&
                    Enum.TryParse<Gender>(reader.GetString(), true, out var result))
                    gender = result;
                else
                    reader.TrySkip();
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, Voice value, JsonSerializerOptions options) => throw new NotImplementedException();
    }
}
