using System.Security.Cryptography;
using System.Text;
using DarkStar.Application.Abstractions;

namespace DarkStar.Infrastructure.Crypto;

public sealed class HmacSignatureEngine : ISignatureEngine
{
    public string SignToBase64(string message, string secret)
    {
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        using var hmac = new HMACSHA256(key);
        var sig = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToBase64String(sig);
    }

    public bool Verify(string message, string secret, string signatureBase64)
    {
        byte[] provided;
        try
        {
            provided = Convert.FromBase64String(signatureBase64);
        }
        catch (FormatException)
        {
            return false;
        }

        var computed = Convert.FromBase64String(SignToBase64(message, secret));
        return CryptographicOperations.FixedTimeEquals(computed, provided);
    }
}
