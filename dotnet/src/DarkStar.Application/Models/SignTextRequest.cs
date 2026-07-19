namespace DarkStar.Application.Models;

public sealed record SignTextRequest(
    string Message,
    string Secret
);
