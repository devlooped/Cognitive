using Cognitive.Speech.Amazon;
using Microsoft.Extensions.Configuration;

namespace Cognitive.Tests;

public class AmazonSpeechTests
{
    static readonly IConfiguration Config = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .AddUserSecrets(ThisAssembly.Project.UserSecretsId).Build();

    [Fact]
    public void CreateEngineThrowsIfNoSpeechKeyConfig()
        => Assert.Contains("Amazon:AccessKey",
            Assert.Throws<ArgumentException>("configuration", () =>
                SpeechEngine.Create(
                    new ConfigurationBuilder().AddInMemoryCollection().Build()))
            .Message);

    [Fact]
    public void CreateEngineThrowsIfNoSecretKeyConfig()
        => Assert.Contains("Amazon:SecretKey",
            Assert.Throws<ArgumentException>("configuration", () =>
                SpeechEngine.Create(
                    new ConfigurationBuilder()
                        .AddInMemoryCollection(new[] { KeyValuePair.Create("Amazon:AccessKey", "foo") })
                    .Build()))
            .Message);

    [Fact]
    public void CreateEngineThrowsIfNoRegionConfig()
        => Assert.Contains("Amazon:Region",
            Assert.Throws<ArgumentException>("configuration", () =>
                SpeechEngine.Create(
                    new ConfigurationBuilder()
                        .AddInMemoryCollection(new[]
                        {
                            KeyValuePair.Create("Amazon:AccessKey", "foo"),
                            KeyValuePair.Create("Amazon:SecretKey", "bar")
                        })
                    .Build()))
            .Message);


    [Fact]
    public async Task GetVoices()
    {
        var engine = SpeechEngine.Create(Config);
        var voices = await engine.GetVoicesAsync();

        Assert.NotEmpty(voices);
        Assert.Contains(voices, voice => voice.Name == "Kendra");
    }
}