namespace DarkStar.Application.Models;

public sealed record VerifyTextRequest(
    string Message,
    string Secret,
    string SignatureBase64
);
