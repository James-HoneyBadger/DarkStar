namespace DarkStar.Api.Contracts;

public sealed record FileCryptoApiResponse(
    string Algorithm,
    string InputPath,
    string OutputPath,
    long InputBytes,
    long OutputBytes
);
