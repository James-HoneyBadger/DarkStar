namespace DarkStar.Application.Models;

public sealed record DecryptFileResult(
    string Algorithm,
    string InputPath,
    string OutputPath,
    long InputBytes,
    long OutputBytes
);
