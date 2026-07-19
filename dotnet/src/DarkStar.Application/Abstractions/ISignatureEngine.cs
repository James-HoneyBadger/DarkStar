namespace DarkStar.Application.Abstractions;

public interface ISignatureEngine
{
    string SignToBase64(string message, string secret);
    bool Verify(string message, string secret, string signatureBase64);
}
