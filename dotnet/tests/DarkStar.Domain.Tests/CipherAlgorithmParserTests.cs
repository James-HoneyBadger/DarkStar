using DarkStar.Domain.Security;

namespace DarkStar.Domain.Tests;

public sealed class CipherAlgorithmParserTests
{
    [Theory]
    [InlineData("aes", CipherAlgorithm.Aes256Gcm)]
    [InlineData("AES-256-GCM", CipherAlgorithm.Aes256Gcm)]
    [InlineData("chacha", CipherAlgorithm.ChaCha20Poly1305)]
    [InlineData("chacha20poly1305", CipherAlgorithm.ChaCha20Poly1305)]
    public void Parse_KnownValues_ReturnsExpected(string input, CipherAlgorithm expected)
    {
        var actual = CipherAlgorithmParser.Parse(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Parse_Unknown_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CipherAlgorithmParser.Parse("unknown"));
    }
}
