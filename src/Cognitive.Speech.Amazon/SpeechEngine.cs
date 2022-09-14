using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;

namespace Cognitive.Speech.Amazon;

public class SpeechEngine : ISpeechEngine
{
    readonly AmazonPollyClient polly;

    public SpeechEngine(string accessKey, string secretKey, string region)
        => polly = new AmazonPollyClient(new BasicAWSCredentials(accessKey, secretKey), RegionEndpoint.GetBySystemName(region));

    public static ISpeechEngine Create(IConfiguration configuration)
    {
        var accessKey = configuration["Amazon:AccessKey"];
        var secretKey = configuration["Amazon:SecretKey"];
        var region = configuration["Amazon:Region"];

        if (string.IsNullOrEmpty(accessKey))
            throw new ArgumentException("Required 'Amazon:AccessKey' configuration missing.", nameof(configuration));

        if (string.IsNullOrEmpty(secretKey))
            throw new ArgumentException("Required 'Amazon:SecretKey' configuration missing.", nameof(configuration));

        if (string.IsNullOrEmpty(region))
            throw new ArgumentException("Required 'Amazon:Region' configuration missing.", nameof(configuration));

        return Create(accessKey, secretKey, region);
    }

    public static ISpeechEngine Create(string accessKey, string secretKey, string region)
        => new SpeechEngine(accessKey, secretKey, region);
    public Task<SynthesisJob> CreateJobAsync(string voice, Func<SynthesisJob, SynthesisJob>? configure = null, CancellationToken cancellation = default)
        => throw new NotImplementedException();

    public async Task<IReadOnlyCollection<Voice>> GetVoicesAsync(CancellationToken cancellation = default)
    {
        var voices = await polly.DescribeVoicesAsync(new DescribeVoicesRequest(), cancellation);

        return voices.Voices
            .Where(x => x.SupportedEngines.Contains("neural"))
            .Select(x => new Voice(x.Id, x.Name, x.LanguageCode,
                (Gender)Enum.Parse(typeof(Gender), x.Gender.Value)))
            .ToArray();
    }
}
