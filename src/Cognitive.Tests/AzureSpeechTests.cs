using Cognitive.Speech.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Moq;

namespace Cognitive.Tests;

public class AzureSpeechTests
{
    static readonly IServiceProvider Services = new ServiceCollection()
        .AddHttpClient()
        .BuildServiceProvider();

    static readonly IConfiguration Config = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .AddUserSecrets(ThisAssembly.Project.UserSecretsId).Build();

    [Fact]
    public void CreateEngineThrowsIfNoSpeechKeyConfig()
        => Assert.Contains("Azure:SpeechKey",
            Assert.Throws<ArgumentException>("configuration", () =>
                SpeechEngine.Create(
                    new ConfigurationBuilder().AddInMemoryCollection().Build(),
                    Mock.Of<IHttpClientFactory>()))
            .Message);

    [Fact]
    public void CreateEngineThrowsIfNoSpeechRegionConfig()
        => Assert.Contains("Azure:SpeechRegion",
            Assert.Throws<ArgumentException>("configuration", () =>
                SpeechEngine.Create(
                    new ConfigurationBuilder()
                        .AddInMemoryCollection(new[] { KeyValuePair.Create("Azure:SpeechKey", "foo") })
                        .Build(),
                    Mock.Of<IHttpClientFactory>()))
            .Message);

    [Fact]
    public async Task GetVoices()
    {
        var engine = SpeechEngine.Create(Config, Services.GetRequiredService<IHttpClientFactory>());
        var voices = await engine.GetVoicesAsync();

        Assert.NotEmpty(voices);
        Assert.Contains(voices, voice => voice.Name == "en-US-JennyNeural");
    }
}