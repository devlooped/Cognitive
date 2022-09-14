using Cognitive.Speech.Google;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Cognitive.Tests;

public class GoogleSpeechTests
{
    static readonly IServiceProvider Services = new ServiceCollection()
        .AddHttpClient()
        .BuildServiceProvider();

    static readonly IConfiguration Config = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .AddUserSecrets(ThisAssembly.Project.UserSecretsId).Build();

    [Fact]
    public void CreateEngineThrowsIfNoSpeechKeyConfig()
        => Assert.Contains("Google:ApiKey",
            Assert.Throws<ArgumentException>("configuration", () =>
                SpeechEngine.Create(
                    new ConfigurationBuilder().AddInMemoryCollection().Build(),
                    Mock.Of<IHttpClientFactory>()))
            .Message);

    [Fact]
    public async Task GetVoices()
    {
        var engine = SpeechEngine.Create(Config, Services.GetRequiredService<IHttpClientFactory>());
        var voices = await engine.GetVoicesAsync();

        Assert.NotEmpty(voices);
        Assert.Contains(voices, voice => voice.Locale == "en-US");
    }
}