namespace DarkStar.Api.Contracts;

public sealed record VerifyTextApiRequest(
    string Message,
    string Secret,
    string SignatureBase64
);
