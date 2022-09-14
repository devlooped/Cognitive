using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Cloud.TextToSpeech.V1;
using Microsoft.Extensions.Configuration;

namespace Cognitive.Speech.Google;

public class SpeechEngine : ISpeechEngine
{
    readonly string apiKey;
    readonly Lazy<HttpClient> http;

    public SpeechEngine(string apiKey, IHttpClientFactory httpFactory)
    {
        this.apiKey = apiKey;

        http = new Lazy<HttpClient>(() =>
        {
            var http = httpFactory.CreateClient();
            http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Cognitive.Speech.Google");
            http.DefaultRequestHeaders.TryAddWithoutValidation("X-goog-api-key", this.apiKey);
            http.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");
            return http;
        });
    }

    public static ISpeechEngine Create(IConfiguration configuration, IHttpClientFactory httpFactory)
    {
        var apiKey = configuration["Google:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentException("Required 'Google:ApiKey' configuration missing.", nameof(configuration));

        return Create(apiKey, httpFactory);
    }

    public static ISpeechEngine Create(string apiKey, IHttpClientFactory httpFactory)
        => new SpeechEngine(apiKey, httpFactory);

    public Task<SynthesisJob> CreateJobAsync(string voice, Func<SynthesisJob, SynthesisJob>? configure = null, CancellationToken cancellation = default)
        => throw new NotImplementedException();

    public async Task<IReadOnlyCollection<Voice>> GetVoicesAsync(CancellationToken cancellation = default)
    {
        var response = await http.Value.GetAsync("https://texttospeech.googleapis.com/v1/voices", cancellation);

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

        return json["voices"]
            .Where(x => x.Name.Contains("neural", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    class VoiceConverter : JsonConverter<Voice>
    {
        public override Voice? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            /*
            "languageCodes": [
            "ro-RO"
            ],
            "name": "ro-RO-Wavenet-A",
            "ssmlGender": "FEMALE",
            "naturalSampleRateHertz": 24000
            */
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            string? name = default;
            Gender? gender = default;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (name != null && gender != null)
                        return new Voice(name, name, string.Join('-', name.Split('-')[0..2]), gender.Value);
                    else
                        return null;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();

                var prop = reader.GetString();
                reader.Read();

                if (prop == "name")
                    name = reader.GetString();
                else if (prop == "ssmlGender" &&
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