namespace DarkStar.Application.Models;

public sealed record EncryptFileResult(
    string Algorithm,
    string InputPath,
    string OutputPath,
    long InputBytes,
    long OutputBytes
);
